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
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

// Isolated test observer; never included in the shipped mod.
[ModInitializer(nameof(Initialize))]
public static class BaseballProbe
{
    private sealed class Seen(NKarenBaseballVfx node)
    {
        public readonly NKarenBaseballVfx Node = node;
        public readonly bool Impact = (bool)Field(node, "_impact")!;
        public int Frames, Windup, Flight, Confirmed;
        public float MaxAnchorError, ConfirmedAt = -1, FinalAge, Unit;
        public bool Exited;
    }
    private static readonly List<Seen> Casts = new();
    private static bool _watching;
    private static object? Field(object node, string name) => node.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(node);
    public static void Initialize() => ((SceneTree)Engine.GetMainLoop()).ProcessFrame += () => Callable.From(Sample).CallDeferred();
    private static bool Alive(NKarenBaseballVfx node) => GodotObject.IsInstanceValid(node) && node.IsInsideTree() && !node.IsQueuedForDeletion();
    private static void Sample()
    {
        if (!_watching || NCombatRoom.Instance is not { } room) return;
        foreach (var node in room.CombatVfxContainer.GetChildren().OfType<NKarenBaseballVfx>())
        {
            var seen = Casts.FirstOrDefault(s => s.Node == node);
            if (seen == null)
            {
                seen = new Seen(node); Casts.Add(seen);
                var tracked = seen;
                node.TreeExiting += () => { tracked.Exited = true; tracked.FinalAge = (float)Field(node, "_age")!; };
            }
            float age = (float)Field(node, "_age")!;
            seen.Frames++; seen.Unit = (float)Field(node, "_unit")!;
            if (!seen.Impact && age < .15f) seen.Windup++;
            if (!seen.Impact && age >= .15f && (float)Field(node, "_confirmedAt")! < 0) seen.Flight++;
            if ((float)Field(node, "_confirmedAt")! >= 0) { seen.Confirmed++; seen.ConfirmedAt = (float)Field(node, "_confirmedAt")!; }
            var target = (NCreature)Field(node, "_targetView")!;
            if (!GodotObject.IsInstanceValid(target) || !target.IsInsideTree() || target.IsQueuedForDeletion()) continue;
            var bounds = target.Visuals.GetNodeOrNull<Control>("%Bounds") ?? target.Hitbox;
            var expected = bounds.GetGlobalTransformWithCanvas() * (bounds.Size * .5f);
            var actual = node.GetGlobalTransformWithCanvas() * (Vector2)Field(node, "_target")!;
            seen.MaxAnchorError = Math.Max(seen.MaxAnchorError, actual.DistanceTo(expected));
        }
    }
    internal static string Status() => JsonSerializer.Serialize(new {
        live = Casts.Count(s => Alive(s.Node)),
        casts = Casts.Select(s => new { impact = s.Impact, frames = s.Frames, windup = s.Windup, flight = s.Flight,
            confirmed = s.Confirmed, confirmedAt = s.ConfirmedAt, unit = s.Unit, maxAnchorError = s.MaxAnchorError,
            exited = s.Exited, finalAge = s.FinalAge }) });
    internal static void Watch() { Casts.Clear(); _watching = true; }
}

public sealed class BaseballProbeCmd : AbstractConsoleCmd
{
    public override string CmdName => "baseball_probe";
    public override string Args => "watch|status|pose|lethal";
    public override string Description => "Observe real baseball attacks in the isolated test game.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || args.Length != 1 || NCombatRoom.Instance is not { } room) return new(false, Args);
        switch (args[0])
        {
            case "watch": BaseballProbe.Watch(); return new(true, "watching");
            case "status": return new(true, BaseballProbe.Status());
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
