using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

// Isolated observation only. No effect is spawned and no game RNG is consumed here.
[ModInitializer(nameof(Initialize))]
public static class SuitabilityPolishProbe
{
    private sealed class Seen(Node node)
    {
        public Node Node = node;
        public string Type = node.GetType().Name;
        public int Frames;
        public bool Exited;
    }
    private static readonly List<Seen> SeenNodes = new();
    private static readonly List<object> CurtainSamples = new();
    private static readonly Dictionary<string, int> Peaks = new();
    private static bool _watch;
    private static double _start, _next;
    private static object? Field(object o, string name) => o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(o);
    public static void Initialize() => ((SceneTree)Engine.GetMainLoop()).ProcessFrame += () => Callable.From(Sample).CallDeferred();
    private static bool Relevant(Node n) => n is NKarenArrogantVfx or NKarenPassionVfx or NKarenFightRelayVfx
        or NKarenJealousyVfx or NKarenWakeUpSelectionVfx or NKarenWakeUpCardVfx;
    private static void Sample()
    {
        if (!_watch || NCombatRoom.Instance is not { } room) return;
        var nodes = room.CombatVfxContainer.GetChildren().Concat(room.Ui.GetChildren()).Where(Relevant).ToArray();
        foreach (var group in nodes.GroupBy(n => n.GetType().Name))
            Peaks[group.Key] = Math.Max(Peaks.GetValueOrDefault(group.Key), group.Count());
        foreach (var node in nodes)
        {
            var seen = SeenNodes.FirstOrDefault(s => s.Node == node);
            if (seen == null) { seen = new Seen(node); SeenNodes.Add(seen); var captured = seen; node.TreeExiting += () => captured.Exited = true; }
            seen.Frames++;
        }
        double now = Time.GetTicksMsec() / 1000.0;
        if (now < _next) return;
        _next = now + .2;
        foreach (var node in nodes.OfType<NKarenWakeUpSelectionVfx>())
            if (Field(node, "_material") is ShaderMaterial material)
                CurtainSamples.Add(new { time = now - _start, showing = node.Showing, selected = node.SelectedCount,
                    strength = material.GetShaderParameter("strength").AsSingle(), closing = material.GetShaderParameter("closing").AsSingle(),
                    flowAge = Field(node, "_flowAge") as float? });
    }
    internal static void Watch() { SeenNodes.Clear(); CurtainSamples.Clear(); Peaks.Clear(); _start = Time.GetTicksMsec() / 1000.0; _next = 0; _watch = true; }
    internal static void Cancel()
    {
        foreach (var seen in SeenNodes)
            if (GodotObject.IsInstanceValid(seen.Node) && seen.Node.IsInsideTree()) seen.Node.QueueFree();
    }
    internal static object Status(Player player) => new {
        live = SeenNodes.Count(s => GodotObject.IsInstanceValid(s.Node) && s.Node.IsInsideTree() && !s.Node.IsQueuedForDeletion()),
        peaks = Peaks, nodes = SeenNodes.Select(s => new { type = s.Type, frames = s.Frames, exited = s.Exited }),
        curtains = CurtainSamples,
        passion = player.Creature.GetPowerAmount<KarenPassionPower>(), vulnerable = player.Creature.GetPowerAmount<VulnerablePower>(),
        relicRewards = player.RunState.CurrentRoom is CombatRoom room && room.ExtraRewards.TryGetValue(player, out var rewards)
            ? rewards.OfType<RelicReward>().Count() : 0,
        back = NCombatRoom.Instance?.BackCombatVfxContainer.GetChildren().Count(n => n.Name.ToString().StartsWith("KarenArrogant") || n.Name == "KarenRelayFootlights") ?? 0
    };
}
public sealed class SuitabilityPolishCmd : AbstractConsoleCmd
{
    public override string CmdName => "vfx_polish";
    public override string Args => "watch|status|cancel|deplete ID";
    public override string Description => "Observe VFX suitability comparisons in an isolated game.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player?.PlayerCombatState == null || args.Length == 0) return new(false, Args);
        if (args[0] == "watch") { SuitabilityPolishProbe.Watch(); return new(true, "watching"); }
        if (args[0] == "status") return new(true, JsonSerializer.Serialize(SuitabilityPolishProbe.Status(player)));
        if (args[0] == "cancel") { SuitabilityPolishProbe.Cancel(); return new(true, "cancelled test-owned effects"); }
        if (args[0] == "deplete" && args.Length == 2)
        { player.PlayerCombatState.Hand.Cards.First(c => c.Id.Entry == args[1]).SetShineCurrent(1); return new(true, "next native play exhausts Shine"); }
        return new(false, Args);
    }
}
