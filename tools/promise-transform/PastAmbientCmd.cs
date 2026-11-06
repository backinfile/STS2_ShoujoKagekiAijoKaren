using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

// Isolated development fixture only; hide/show and stop/start exercise VFX, not card rules.
public sealed class PastAmbientCmd : AbstractConsoleCmd
{
    private static Task _capture = Task.CompletedTask;
    public override string CmdName => "past_ambient";
    public override string Args => "status|hide|show|stop|start|capture";
    public override string Description => "Inspect the owner, layering, fade and motion of passive past/future traces.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || args.Length != 1) return new(false, Args);
        var creature = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        if (args[0] == "capture")
        {
            if (!_capture.IsCompleted) return new(false, "Capture already running");
            var target = Descend(NRun.Instance).OfType<NKarenPastAndFutureRingVfx>().Single(n => n.BelongsTo(creature!));
            _capture = Capture(target);
            return new(_capture, true, "Frozen same-phase on/off screenshots in user://past-ambient-audit");
        }
        if (args[0] == "stop") KarenPastAndFutureRingVfxManager.Stop(player);
        else if (args[0] == "start") KarenPastAndFutureRingVfxManager.Start(player);
        else if (args[0] is not ("status" or "hide" or "show")) return new(false, Args);
        var nodes = Descend(NRun.Instance).OfType<NKarenPastAndFutureRingVfx>().ToArray();
        if (args[0] is "hide" or "show")
            foreach (var node in nodes.Where(n => n.BelongsTo(creature!))) node.Visible = args[0] == "show";
        return new(true, JsonSerializer.Serialize(new {
            capturing = !_capture.IsCompleted,
            nodes = nodes.Length,
            fronts = Descend(NRun.Instance).Count(n => n.Name == "PastAndFutureRingFront"),
            traces = Descend(NRun.Instance).Count(n => n is ColorRect && n.Name.ToString().StartsWith("PastAndFutureAmbientTrace")),
            ambientWeight = KarenVfxComposition.AmbientWeight(player.Creature),
            owners = nodes.Select(n => new {
                own = creature != null && n.BelongsTo(creature),
                behind = creature != null && n.GetParent() == creature.GetParent() && n.GetIndex() < creature.GetIndex(),
                frontAfter = Field(n, "_front") is Node2D front && creature != null && front.GetIndex() > creature.GetIndex(),
                originError = creature == null ? -1 : n.GlobalPosition.DistanceTo(creature.VfxSpawnPosition),
                scaleError = creature == null ? -1 : new Vector2(n.GlobalTransform.X.Length(), n.GlobalTransform.Y.Length())
                    .DistanceTo(new Vector2(creature.Visuals.GlobalTransform.X.Length(), creature.Visuals.GlobalTransform.Y.Length())),
                scaleX = n.GlobalTransform.X.X, scaleY = n.GlobalTransform.Y.Y,
                time = Field(n, "_ambientTime"), visibility = Field(n, "_ambientVisibility"),
                alpha = n.Modulate.A, visible = n.Visible
            })
        }));
    }
    private static async Task Capture(NKarenPastAndFutureRingVfx node)
    {
        var tree = node.GetTree();
        bool paused = tree.Paused;
        bool visible = node.Visible;
        double timeScale = Engine.TimeScale;
        var phaseField = node.GetType().GetField("_ambientTime", BindingFlags.NonPublic | BindingFlags.Instance)!;
        float phase = (float)phaseField.GetValue(node)!;
        string directory = ProjectSettings.GlobalizePath("user://past-ambient-audit");
        System.IO.Directory.CreateDirectory(directory);
        try
        {
            tree.Paused = true;
            Engine.TimeScale = 0;
            node.SetProcess(false);
            phaseField.SetValue(node, .5f);
            node.Show();
            UpdateAmbient(node);
            node.QueueRedraw();
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using (var image = node.GetViewport().GetTexture().GetImage())
                image.SavePng(System.IO.Path.Combine(directory, "visible.png"));
            node.Hide();
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using (var image = node.GetViewport().GetTexture().GetImage())
                image.SavePng(System.IO.Path.Combine(directory, "hidden.png"));
        }
        finally
        {
            phaseField.SetValue(node, phase);
            UpdateAmbient(node);
            node.Visible = visible;
            node.SetProcess(true);
            Engine.TimeScale = timeScale;
            tree.Paused = paused;
        }
    }

    private static void UpdateAmbient(NKarenPastAndFutureRingVfx node) =>
        node.GetType().GetMethod("UpdateAmbient", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(node, null);

    private static object? Field(object obj, string name) => obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj);
    private static System.Collections.Generic.IEnumerable<Node> Descend(Node? node)
    {
        if (node == null || !GodotObject.IsInstanceValid(node)) yield break;
        yield return node;
        foreach (var child in node.GetChildren()) foreach (var descendant in Descend(child)) yield return descendant;
    }
}