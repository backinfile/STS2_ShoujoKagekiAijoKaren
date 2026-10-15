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
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

// Only loaded in the disposable recording game; never distributed with the Mod.
[ModInitializer(nameof(Initialize))]
public static class PassionProbe
{
    internal sealed class Seen(NKarenPassionVfx node)
    {
        public readonly NKarenPassionVfx Node = node;
        public int Frames;
        public bool Stamped, Arrived, Bound, Freed, Finite = true;
        public float Age, ArrivalError = -1;
        public readonly double Started = Time.GetTicksMsec() / 1000.0;
        public double Duration;
    }
    internal static readonly List<Seen> Effects = new();
    internal static bool Watching;
    static object? Field(object node, string name) => node.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(node);
    public static void Initialize() => ((SceneTree)Engine.GetMainLoop()).ProcessFrame += () => Callable.From(Sample).CallDeferred();
    static void Sample()
    {
        if (!Watching || NCombatRoom.Instance is not { } room) return;
        foreach (var node in room.CombatVfxContainer.GetChildren().OfType<NKarenPassionVfx>())
        {
            var seen = Effects.FirstOrDefault(s => s.Node == node);
            if (seen == null)
            {
                seen = new Seen(node); Effects.Add(seen);
                var captured = seen;
                node.TreeExiting += () => { captured.Freed = true; captured.Duration = Time.GetTicksMsec() / 1000.0 - captured.Started; };
            }
            seen.Frames++;
            seen.Age = (float)Field(node, "_age")!;
            seen.Stamped |= (bool)Field(node, "_stamped")!;
            seen.Arrived |= (bool)Field(node, "_arrived")!;
            seen.Bound |= Field(node, "_badge") != null;
            foreach (var name in new[] { "_body", "_center", "_destination" }) seen.Finite &= ((Vector2)Field(node, name)!).IsFinite();
            if (seen.Arrived && Field(node, "_badge") != null)
            {
                var p = (Vector2)node.GetType().GetMethod("EnvelopePoint", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(node, new object[] { seen.Age })!;
                seen.ArrivalError = p.DistanceTo((Vector2)Field(node, "_destination")!);
            }
        }
    }
    internal static object Status(Player player) => new {
        live = NCombatRoom.Instance?.CombatVfxContainer.GetChildren().OfType<NKarenPassionVfx>().Count() ?? 0,
        reserved = player.RunState.CurrentRoom is CombatRoom room && room.ExtraRewards.TryGetValue(player, out var rewards) ? rewards.OfType<RelicReward>().Count() : 0,
        power = player.Creature.GetPowerAmount<KarenPassionPower>(),
        relics = player.Relics.Select(r => r.Id.Entry).ToArray(),
        effects = Effects.Select(s => new { s.Frames, s.Stamped, s.Arrived, s.Bound, s.Freed, s.Finite, s.Age, s.Duration, s.ArrivalError })
    };
}
public sealed class PassionProbeCmd : AbstractConsoleCmd
{
    public override string CmdName => "passion_probe";
    public override string Args => "watch|status|speed normal/fast/instant|mirror";
    public override string Description => "Observe actual Passion rewards and VFX in isolated tests.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || args.Length == 0) return new(false, Args);
        switch (args[0])
        {
            case "watch": PassionProbe.Effects.Clear(); PassionProbe.Watching = true; return new(true, "Watching");
            case "status": return new(true, JsonSerializer.Serialize(PassionProbe.Status(player)));
            case "speed":
                SaveManager.Instance.PrefsSave.FastMode = args[1] switch { "fast" => FastModeType.Fast, "instant" => FastModeType.Instant, _ => FastModeType.Normal };
                return new(true, args[1]);
            case "mirror":
                var visuals = NCombatRoom.Instance!.GetCreatureNode(player.Creature)!.Visuals;
                visuals.Scale = new Vector2(-visuals.Scale.X, visuals.Scale.Y);
                return new(true, "Mirrored");
            default: return new(false, Args);
        }
    }
}
