using System;
using System.Linq;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;

// Test plugin only. Check after the transform/cycle has settled for several frames.
public sealed class BurnVfxCmd : AbstractConsoleCmd
{
    public override string CmdName => "burn_vfx";
    public override string Args => "check|cycle";
    public override string Description => "Check upright fire placement or stop/restart its manager.";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? player, string[] args)
    {
        var creature = player == null ? null : NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        if (creature == null || args.Length != 1) return new(false, "Combat and check/cycle required");
        if (args[0] == "cycle")
        {
            KarenBurnVfxManager.Stop(player!);
            KarenBurnVfxManager.Start(player!);
            return new(true, "Stop/start queued; check after at least 0.3 seconds");
        }
        if (args[0] != "check") return new(false, "Use check or cycle");
        var nodes = creature.GetChildren().OfType<NKarenBurnVfx>().ToArray();
        if (nodes.Length != 1) return new(false, $"Expected one fire node, found {nodes.Length}");
        var fire = nodes[0];
        var actual = fire.GlobalTransform;
        var visual = creature.Visuals.GlobalTransform;
        var originError = actual.Origin.DistanceTo(creature.VfxSpawnPosition);
        var scaleError = new Vector2(actual.X.Length(), actual.Y.Length())
            .DistanceTo(new Vector2(visual.X.Length(), visual.Y.Length()));
        var upright = actual.X.X > 0 && actual.Y.Y > 0
            && Math.Abs(actual.X.Y) < 0.001f && Math.Abs(actual.Y.X) < 0.001f;
        var alive = !fire.IsQueuedForDeletion() && fire.Visible && fire.Modulate.A > 0.99f;
        var particles = fire.GetNodeOrNull<Node2D>("TowerFlames")?.GetChildCount() ?? 0;
        bool behindActor = fire.ShowBehindParent && fire.ZIndex == 0;
        var icon = NCombatRoom.Instance!.Ui.DrawPile.GetNode<TextureRect>("Icon");
        var drawFires = icon.GetChildren().OfType<Node2D>().Where(n => n.Name == "KarenBurnDrawPile" && !n.IsQueuedForDeletion()).ToArray();
        bool isVoid = player!.Creature.GetPower<KarenPromisePilePower>()?.IsVoidMode == true;
        bool drawFireValid = !isVoid ? drawFires.Length == 0 : drawFires.Length == 1
            && drawFires[0].ShowBehindParent && drawFires[0].Position.DistanceTo(icon.Size * .5f) < .1f
            && drawFires[0].GetChildCount() > 0;
        return new(originError < 0.1f && scaleError < 0.001f && upright && alive && particles > 0 && behindActor && drawFireValid,
            JsonSerializer.Serialize(new { originError, scaleError, upright, alive, particles,
                behindActor, isVoid, drawFireValid, drawFireNodes = drawFires.Length,
                expectedX = visual.X.Length(), actualX = actual.X.Length() }));
    }
}
