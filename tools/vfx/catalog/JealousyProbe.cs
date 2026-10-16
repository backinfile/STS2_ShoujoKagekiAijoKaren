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
using ShoujoKagekiAijoKaren.src.Core.ExtraReplaySystem;

// Observe genuine native attacks and rewards in disposable test games only.
[ModInitializer(nameof(Initialize))]
public static class JealousyProbe
{
    private sealed class Seen(NKarenJealousyVfx node)
    {
        public readonly NKarenJealousyVfx Node = node;
        public readonly string Kind = (bool)Field(node, "_impact")! ? "impact" : "sweep";
        public int Frames, PreHitFrames, Confirmed, Ghosts, Emitters, ParticleCapacity, EmittingFrames;
        public readonly HashSet<ulong> Materials = new();
        public float MaxAnchorError, ConfirmedAt = -1, FinalAge, Unit;
        public bool Exited;
    }
    private static readonly List<Seen> Casts = new();
    private static bool _watching;
    private static Vector2 _baselineOffset, _baselinePosition, _baselineVisualPosition;
    private static NCreature? _ownerView;
    private static float _maxOffsetError, _maxPositionError, _maxVisualPositionError;
    private static int _maxZError;
    private static int _baselineZ;
    private static int _maxLiveNodes, _maxLiveParticleCapacity;
    private static object? Field(object node, string name) => node.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(node);
    public static void Initialize() => ((SceneTree)Engine.GetMainLoop()).ProcessFrame += () => Callable.From(Sample).CallDeferred();
    private static bool Alive(NKarenJealousyVfx node) => GodotObject.IsInstanceValid(node) && node.IsInsideTree() && !node.IsQueuedForDeletion();
    private static void Sample()
    {
        if (!_watching || NCombatRoom.Instance is not { } room) return;
        var currentNodes = room.CombatVfxContainer.GetChildren().OfType<NKarenJealousyVfx>().ToArray();
        _maxLiveNodes = Math.Max(_maxLiveNodes, currentNodes.Length);
        _maxLiveParticleCapacity = Math.Max(_maxLiveParticleCapacity,
            currentNodes.Sum(n => n.GetChildren().OfType<CpuParticles2D>().Sum(p => p.Amount)));
        if (GodotObject.IsInstanceValid(_ownerView) && _ownerView!.IsInsideTree()) {
            _maxPositionError = Math.Max(_maxPositionError, _ownerView.Position.DistanceTo(_baselinePosition));
            _maxVisualPositionError = Math.Max(_maxVisualPositionError, _ownerView.Visuals.Position.DistanceTo(_baselineVisualPosition));
            if (_ownerView.Body is Sprite2D actor) {
                _maxOffsetError = Math.Max(_maxOffsetError, actor.Offset.DistanceTo(_baselineOffset));
                _maxZError = Math.Max(_maxZError, Math.Abs(actor.ZIndex - _baselineZ));
            }
        }
        foreach (var node in currentNodes) {
            var seen = Casts.FirstOrDefault(s => s.Node == node);
            if (seen == null) {
                seen = new Seen(node); Casts.Add(seen);
                var tracked = seen;
                node.TreeExiting += () => { tracked.Exited = true; tracked.FinalAge = (float)Field(node, "_age")!; };
            }
            seen.Frames++; seen.Unit = (float)Field(node, "_unit")!;
            seen.Ghosts = Math.Max(seen.Ghosts, node.GetChildren().OfType<Sprite2D>().Count(g => g.Visible));
            var particles = node.GetChildren().OfType<CpuParticles2D>().ToArray();
            seen.Emitters = Math.Max(seen.Emitters, particles.Length);
            seen.ParticleCapacity = Math.Max(seen.ParticleCapacity, particles.Sum(p => p.Amount));
            if (particles.Any(p => p.Emitting)) seen.EmittingFrames++;
            foreach (var p in particles) if (p.Material != null) seen.Materials.Add(p.Material.GetInstanceId());
            if (seen.Kind == "sweep" && (float)Field(node, "_confirmedAt")! < 0) seen.PreHitFrames++;
            if ((float)Field(node, "_confirmedAt")! >= 0) { seen.Confirmed++; seen.ConfirmedAt = (float)Field(node, "_confirmedAt")!; }
            var view = (NCreature)Field(node, "_targetView")!;
            if (!GodotObject.IsInstanceValid(view) || !view.IsInsideTree() || view.IsQueuedForDeletion()) continue;
            var bounds = view.Visuals.GetNodeOrNull<Control>("%Bounds") ?? view.Hitbox;
            var matrix = bounds.GetGlobalTransformWithCanvas();
            var rect = new Rect2(matrix * Vector2.Zero, Vector2.Zero)
                .Expand(matrix * new Vector2(bounds.Size.X, 0)).Expand(matrix * new Vector2(0, bounds.Size.Y)).Expand(matrix * bounds.Size);
            var expected = rect.GetCenter();
            var actual = node.GetGlobalTransformWithCanvas() * (Vector2)Field(node, "_target")!;
            seen.MaxAnchorError = Math.Max(seen.MaxAnchorError, actual.DistanceTo(expected));
        }
    }
    internal static string Status(Player player) => JsonSerializer.Serialize(new {
        live = Casts.Count(s => Alive(s.Node)),
        actorOffsetError = ((NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.Body as Sprite2D)?.Offset ?? Vector2.Zero).DistanceTo(_baselineOffset),
        modelPositionError = (NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.Position ?? Vector2.Zero).DistanceTo(_baselinePosition),
        actorZError = ((NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.Body as Sprite2D)?.ZIndex ?? 0) - _baselineZ,
        maxActorOffsetError = _maxOffsetError, maxModelPositionError = _maxPositionError,
        maxVisualPositionError = _maxVisualPositionError, maxActorZError = _maxZError,
        maxLiveNodes = _maxLiveNodes, maxLiveParticleCapacity = _maxLiveParticleCapacity,
        distinctParticleMaterials = Casts.SelectMany(s => s.Materials).Distinct().Count(),
        relicRewards = player.RunState.CurrentRoom is CombatRoom room && room.ExtraRewards.TryGetValue(player, out var rewards)
            ? rewards.OfType<RelicReward>().Count() : 0,
        hand = player.PlayerCombatState!.Hand.Cards.Select(c => new { id = c.Id.Entry, shine = c.GetShineValue() }),
        casts = Casts.Select(s => new { kind = s.Kind, frames = s.Frames, preHitFrames = s.PreHitFrames,
            confirmed = s.Confirmed, confirmedAt = s.ConfirmedAt, unit = s.Unit, maxAnchorError = s.MaxAnchorError,
            ghosts = s.Ghosts, emitters = s.Emitters, particleCapacity = s.ParticleCapacity,
            emittingFrames = s.EmittingFrames, exited = s.Exited, finalAge = s.FinalAge }) });
    internal static void Watch(Player player) { Casts.Clear(); _watching = true;
        var view = NCombatRoom.Instance!.GetCreatureNode(player.Creature)!;
        _ownerView = view; _baselineVisualPosition = view.Visuals.Position;
        _maxOffsetError = _maxPositionError = _maxVisualPositionError = 0; _maxZError = 0;
        _maxLiveNodes = _maxLiveParticleCapacity = 0;
        _baselineOffset = (view.Body as Sprite2D)?.Offset ?? Vector2.Zero;
        _baselineZ = (view.Body as Sprite2D)?.ZIndex ?? 0;
        _baselinePosition = view.Position;
    }
}

public sealed class JealousyProbeCmd : AbstractConsoleCmd
{
    public override string CmdName => "jealousy_probe";
    public override string Args => "watch|status|deplete-last|upgrade-last|replay-last|overlap|pose|lethal|cancel|cancel-all";
    public override string Description => "Observe real jealousy casts and pending relic rewards.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || args.Length != 1 || NCombatRoom.Instance is not { } room) return new(false, Args);
        switch (args[0]) {
            case "watch": JealousyProbe.Watch(player); return new(true, "watching");
            case "status": return new(true, JealousyProbe.Status(player));
            case "deplete-last":
                player.PlayerCombatState!.Hand.Cards.Last(c => c.Id.Entry == "KAREN_JEALOUSY").SetShineCurrent(1);
                return new(true, "one Shine, exhausted by the next native play");
            case "upgrade-last":
                CardCmd.Upgrade(player.PlayerCombatState!.Hand.Cards.Last(c => c.Id.Entry == "KAREN_JEALOUSY"));
                return new(true, "upgraded");
            case "replay-last":
                player.PlayerCombatState!.Hand.Cards.Last(c => c.Id.Entry == "KAREN_JEALOUSY").AddExtraReplayCountForNextPlay(1);
                return new(true, "one genuine extra replay");
            case "overlap":
                // Visual concurrency fixture only: no card play, no damage and no reward.
                var enemy = room.CreatureNodes.First(v => v.Entity.IsMonster).Entity;
                for (int i = 0; i < 2; i++)
                    if (NKarenJealousyVfx.CreateImpact(enemy, null) is { } impact) room.CombatVfxContainer.AddChild(impact);
                return new(true, "two overlapping visual-only contact bursts");
            case "cancel-all":
                foreach (var node in room.CombatVfxContainer.GetChildren().OfType<NKarenJealousyVfx>()) node.QueueFree();
                return new(true, "cancelled flight and contact particle parents");
            case "cancel":
                foreach (var node in room.CombatVfxContainer.GetChildren().OfType<NKarenJealousyVfx>())
                    if (node.Name == "KarenJealousySwing") node.QueueFree();
                return new(true, "cancelled owned visual; native attack continues");
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
