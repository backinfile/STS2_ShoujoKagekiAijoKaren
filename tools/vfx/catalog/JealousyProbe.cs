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
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

// Observe genuine native attacks and rewards in disposable test games only.
[ModInitializer(nameof(Initialize))]
public static class JealousyProbe
{
    private sealed class Seen(NKarenJealousyVfx node)
    {
        public readonly NKarenJealousyVfx Node = node;
        public readonly string Kind = (bool)Field(node, "_reward")! ? "reward" : (bool)Field(node, "_impact")! ? "impact" : "sweep";
        public int Frames, Travel, Confirmed, RewardCount;
        public float MaxAnchorError, ConfirmedAt = -1, FinalAge, Unit;
        public bool Exited;
    }
    private static readonly List<Seen> Casts = new();
    private static bool _watching;
    private static object? Field(object node, string name) => node.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(node);
    public static void Initialize() => ((SceneTree)Engine.GetMainLoop()).ProcessFrame += () => Callable.From(Sample).CallDeferred();
    private static bool Alive(NKarenJealousyVfx node) => GodotObject.IsInstanceValid(node) && node.IsInsideTree() && !node.IsQueuedForDeletion();
    private static void Sample()
    {
        if (!_watching || NCombatRoom.Instance is not { } room) return;
        foreach (var node in room.CombatVfxContainer.GetChildren().OfType<NKarenJealousyVfx>()) {
            var seen = Casts.FirstOrDefault(s => s.Node == node);
            if (seen == null) {
                seen = new Seen(node); Casts.Add(seen);
                var tracked = seen;
                node.TreeExiting += () => { tracked.Exited = true; tracked.FinalAge = (float)Field(node, "_age")!; };
            }
            seen.Frames++; seen.Unit = (float)Field(node, "_unit")!;
            seen.RewardCount = (int)Field(node, "_rewardCount")!;
            if (seen.Kind == "sweep" && (float)Field(node, "_confirmedAt")! < 0) seen.Travel++;
            if ((float)Field(node, "_confirmedAt")! >= 0) { seen.Confirmed++; seen.ConfirmedAt = (float)Field(node, "_confirmedAt")!; }
            var view = (NCreature)Field(node, "_targetView")!;
            if (!GodotObject.IsInstanceValid(view) || !view.IsInsideTree() || view.IsQueuedForDeletion()) continue;
            var bounds = view.Visuals.GetNodeOrNull<Control>("%Bounds") ?? view.Hitbox;
            var matrix = bounds.GetGlobalTransformWithCanvas();
            var rect = new Rect2(matrix * Vector2.Zero, Vector2.Zero)
                .Expand(matrix * new Vector2(bounds.Size.X, 0)).Expand(matrix * new Vector2(0, bounds.Size.Y)).Expand(matrix * bounds.Size);
            var expected = rect.GetCenter();
            if (seen.Kind == "reward") expected += new Vector2(rect.Size.X * .35f, -rect.Size.Y * .68f);
            var actual = node.GetGlobalTransformWithCanvas() * (Vector2)Field(node, "_target")!;
            seen.MaxAnchorError = Math.Max(seen.MaxAnchorError, actual.DistanceTo(expected));
        }
    }
    internal static string Status(Player player) => JsonSerializer.Serialize(new {
        live = Casts.Count(s => Alive(s.Node)),
        relicRewards = player.RunState.CurrentRoom is CombatRoom room && room.ExtraRewards.TryGetValue(player, out var rewards)
            ? rewards.OfType<RelicReward>().Count() : 0,
        hand = player.PlayerCombatState!.Hand.Cards.Select(c => new { id = c.Id.Entry, shine = c.GetShineValue() }),
        casts = Casts.Select(s => new { kind = s.Kind, frames = s.Frames, travel = s.Travel,
            confirmed = s.Confirmed, confirmedAt = s.ConfirmedAt, unit = s.Unit, maxAnchorError = s.MaxAnchorError,
            rewardCount = s.RewardCount, exited = s.Exited, finalAge = s.FinalAge }) });
    internal static void Watch() { Casts.Clear(); _watching = true; }
}

public sealed class JealousyProbeCmd : AbstractConsoleCmd
{
    public override string CmdName => "jealousy_probe";
    public override string Args => "watch|status|deplete-last|upgrade-last|pose|lethal";
    public override string Description => "Observe real jealousy casts and pending relic rewards.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || args.Length != 1 || NCombatRoom.Instance is not { } room) return new(false, Args);
        switch (args[0]) {
            case "watch": JealousyProbe.Watch(); return new(true, "watching");
            case "status": return new(true, JealousyProbe.Status(player));
            case "deplete-last":
                player.PlayerCombatState!.Hand.Cards.Last(c => c.Id.Entry == "KAREN_JEALOUSY").SetShineCurrent(1);
                return new(true, "one Shine, exhausted by the next native play");
            case "upgrade-last":
                CardCmd.Upgrade(player.PlayerCombatState!.Hand.Cards.Last(c => c.Id.Entry == "KAREN_JEALOUSY"));
                return new(true, "upgraded");
            case "lethal": room.CreatureNodes.First(v => v.Entity.IsMonster).Entity.SetCurrentHpInternal(12); return new(true, "hp=12");
            case "pose":
                var owner = room.GetCreatureNode(player.Creature)!;
                owner.ScaleTo(.65f, 0); owner.Body.Scale *= new Vector2(-1, 1);
                var target = room.CreatureNodes.First(v => v.Entity.IsMonster);
                target.Position += new Vector2(-170, 45); target.Visuals.Rotation = .14f;
                return new(true, "owner scaled/flipped; target moved/rotated");
            default: return new(false, Args);
        }
    }
}
