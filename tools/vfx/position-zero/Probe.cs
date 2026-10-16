using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Patches;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

// Isolated observation fixture. It never calls the visual trigger or changes attack counts.
[ModInitializer(nameof(Initialize))]
public static class PositionZeroProbe
{
    private sealed class Seen(NKarenPositionZeroVfx node)
    {
        public readonly NKarenPositionZeroVfx Node = node;
        public int Frames, Claims, Lights, Ready, Hover, Confirmations, ClaimStarts;
    }
    private static readonly List<Seen> Nodes = new();
    private static bool Watching;
    private static int MaxLive, MaxLights;
    public static void Initialize() => ((SceneTree)Engine.GetMainLoop()).ProcessFrame += Sample;
    private static void Sample()
    {
        if (!Watching || NCombatRoom.Instance is not {} room) return;
        var live = room.CombatVfxContainer.GetChildren().OfType<NKarenPositionZeroVfx>().Where(n => !n.IsQueuedForDeletion()).ToArray();
        MaxLive = Math.Max(MaxLive, live.Length);
        foreach (var node in live)
        {
            var seen = Nodes.FirstOrDefault(s => s.Node == node);
            if (seen == null) { seen = new(node); Nodes.Add(seen); }
            seen.Frames++;
            if (node.ClaimAge < 1.65f) seen.Claims++;
            if (node.ActiveBlockLights > 0) seen.Lights++;
            if (node.ReadyActive) seen.Ready++;
            if (node.HoverActive) seen.Hover++;
            seen.ClaimStarts = Math.Max(seen.ClaimStarts, node.ClaimStarts);
            seen.Confirmations = Math.Max(seen.Confirmations, node.Confirmations);
            MaxLights = Math.Max(MaxLights, node.ActiveBlockLights);
        }
    }
    internal static void Watch() { Nodes.Clear(); MaxLive = MaxLights = 0; Watching = true; }
    internal static object Status(Player p)
    {
        var room = NCombatRoom.Instance;
        var live = room?.CombatVfxContainer.GetChildren().OfType<NKarenPositionZeroVfx>().Where(n => !n.IsQueuedForDeletion()).ToArray() ?? [];
        return new {
            live = live.Length, maxLive = MaxLive, maxLights = MaxLights,
            back = room?.FindChildren("KarenPositionZeroBack", "", true, false).Count(n => !n.IsQueuedForDeletion()) ?? 0,
            amount = p.Creature.GetPowerAmount<KarenPosition0Power>(), block = p.Creature.Block,
            attacks = AttackCounter.GetAttackCount(p), weight = KarenVfxComposition.EventWeight(p.Creature),
            ownership = live.All(n => n.StageOwner == p.Creature),
            remaining = live.FirstOrDefault()?.RemainingTriggers ?? 0,
            ready = live.Any(n => n.ReadyActive), readyAlpha = live.FirstOrDefault()?.ReadyAlpha ?? 0,
            hover = live.Any(n => n.HoverActive), hoverCard = live.FirstOrDefault()?.HoverModel?.Id.Entry,
            cardLights = room?.FindChildren("*", "", true, false).OfType<NKarenPositionZeroCardHoverVfx>().Count(n => !n.IsQueuedForDeletion()) ?? 0,
            geometry = live.Select(n => new { foot = n.FootPoint.ToString(), transform = n.GlobalTransform.ToString(), canvas = n.GetCanvasTransform().ToString(),
                unit = typeof(NKarenPositionZeroVfx).GetField("_unit", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(n) }),
            confirmations = Nodes.Sum(n => n.Confirmations),
            nodes = Nodes.Select(n => new { frames = n.Frames, claimFrames = n.Claims, blockLightFrames = n.Lights, readyFrames = n.Ready,
                hoverFrames = n.Hover, confirmations = n.Confirmations, claimStarts = n.ClaimStarts })
        };
    }
}
public sealed class PositionZeroCmd : AbstractConsoleCmd
{
    public override string CmdName => "pzero";
    public override string Args => "watch|status|remove|speed normal/fast/instant";
    public override string Description => "Observe real Position 0 plays in the isolated recording game.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? p, string[] args)
    {
        if (p == null || args.Length < 1) return new(false, Args);
        if (args[0] == "watch") { PositionZeroProbe.Watch(); return new(true, "Watching native plays"); }
        if (args[0] == "status") return new(true, JsonSerializer.Serialize(PositionZeroProbe.Status(p)));
        if (args[0] == "remove" && p.Creature.GetPower<KarenPosition0Power>() is {} power)
            return new(PowerCmd.Remove(power), true, "Removing real power");
        if (args[0] == "weak-attack")
            return new(PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>(new MegaCrit.Sts2.Core.GameActions.Multiplayer.BlockingPlayerChoiceContext(),
                p.Creature, -99, null, null), true, "Zero-damage attack fixture");
        if (args[0] == "speed" && args.Length == 2)
        {
            SaveManager.Instance.PrefsSave.FastMode = args[1] switch { "fast" => FastModeType.Fast, "instant" => FastModeType.Instant, _ => FastModeType.Normal };
            return new(true, "Set isolated game speed");
        }
        return new(false, Args);
    }
}
