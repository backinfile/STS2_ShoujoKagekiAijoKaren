using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>
/// One star-shaped cut per real hit. The bright layer fades; the same silhouette
/// stays at the captured battle position until combat ends, independently of either creature.
/// </summary>
public partial class NKarenUnforgottenShineVfx : Node2D
{
    // Shared visual resources are prepared during the run loading screen as well as on demand.
    internal static void PrepareResources()
    {
        _texture ??= KarenResourceLoader.LoadTexture(
            "res://images/vfx/unforgettable/lingering-star.png", nameof(NKarenUnforgottenShineVfx));
    }
    internal static Godot.Resource?[] PreparedResources => [_texture];

    public const float HitTime = .20f;
    public const float FastHitTime = HitTime * .7f;
    private const float FadeTime = .45f;
    private const float RetainedOpacity = .14f;
    private const string GroupName = "KarenUnforgottenScars";
    private static readonly Vector2 TexturePivot = new(.5f, .5f);
    private static Texture2D? _texture;

    // A slot owns both copies for its whole combat lifetime. New hits reuse the
    // oldest slot once a target or the battle reaches its budget.
    private sealed class CutSlot(Sprite2D retained, Sprite2D flash)
    {
        public readonly Sprite2D Retained = retained, Flash = flash;
        public ulong Target;
        public long Order;
        public float Age, Speed;
    }
    private static int TargetBudget => KarenVfxQuality.Level == KarenVfxLevel.Low ? 2 : 4;
    private static int BattleBudget => KarenVfxQuality.Level == KarenVfxLevel.Low ? 6 : 12;

    // SelfModulate applies after CanvasGroup compositing: overlapping old cuts
    // remain inside one opacity budget instead of accumulating toward opaque white.
    private readonly CanvasGroup _retained = new()
    {
        Name = "RetainedCuts",
        SelfModulate = new Color(1, 1, 1, RetainedOpacity),
        FitMargin = 2,
        ClearMargin = 2
    };
    private readonly Node2D _flashes = new() { Name = "FreshHits", ZIndex = 1 };
    private readonly List<CutSlot> _cuts = new(12);
    private NCombatRoom _room = null!;
    private long _nextScar;

    public static NKarenUnforgottenShineVfx? Create(Creature owner, Creature target)
    {
        var room = NCombatRoom.Instance;
        if (!KarenVfxQuality.Enabled || NonInteractiveMode.IsActive || room == null || !CombatManager.Instance.IsInProgress
            || room.GetCreatureNode(owner) is not { } attacker
            || room.GetCreatureNode(target) is not { } victim)
            return null;

        PrepareResources();
        if (_texture == null) return null;

        var bounds = ScreenBounds(victim);
        var attackerBounds = ScreenBounds(attacker);
        float side = bounds.GetCenter().X >= attackerBounds.GetCenter().X ? 1 : -1;
        // The sprite's transparent padding leaves the visible long axis about
        // one third of the target height. Distances between creatures never stretch it.
        float size = Mathf.Clamp(bounds.Size.Y * .42f, 48, 140);
        var group = room.CombatVfxContainer.GetNodeOrNull<NKarenUnforgottenShineVfx>(GroupName);
        bool existing = Present(group);
        if (!existing)
            group = new NKarenUnforgottenShineVfx { Name = GroupName, _room = room, ZIndex = 22 };

        var spread = new Vector2(Mathf.Min(bounds.Size.X * .10f, size * .23f),
            Mathf.Min(bounds.Size.Y * .08f, size * .23f));
        var point = group!.PickHitPoint(bounds.GetCenter(), spread, size);
        // Presentation-only randomness; never advance a run/combat RNG stream.
        float angle = Mathf.DegToRad((float)GD.RandRange(-10.0, 10.0));
        var screenPlacement = new Transform2D(angle,
            new Vector2(side * size / _texture.GetWidth(), size / _texture.GetHeight()), 0, point);
        var placement = room.CombatVfxContainer.GetGlobalTransformWithCanvas().AffineInverse()
            * screenPlacement;

        group.AddCut(placement, victim.GetInstanceId());
        // AttackCommand attaches a newly returned node. An existing battle group
        // receives the next cut in place and must not be attached a second time.
        return existing ? null : group;
    }

    private Vector2 PickHitPoint(Vector2 center, Vector2 spread, float size)
    {
        // Best of eight bounded samples keeps successive scars distinct without
        // pushing a hit away from the target or retrying forever in a crowded area.
        Vector2 best = center;
        float bestDistance = -1;
        for (int sample = 0; sample < 8; sample++)
        {
            float direction = GD.Randf() * Mathf.Tau;
            var candidate = center + Vector2.FromAngle(direction) * spread * Mathf.Sqrt(GD.Randf());
            float nearest = float.MaxValue;
            foreach (var cut in _cuts)
            {
                var anchor = cut.Retained.GetGlobalTransformWithCanvas().Origin;
                if (anchor.DistanceSquaredTo(center) < size * size * 2.25f)
                    nearest = Mathf.Min(nearest, candidate.DistanceSquaredTo(anchor));
            }
            if (nearest == float.MaxValue) return candidate;
            if (nearest > bestDistance) { bestDistance = nearest; best = candidate; }
        }
        return best;
    }

    private static bool Present(Node? node)
        => GodotObject.IsInstanceValid(node) && node!.IsInsideTree() && !node.IsQueuedForDeletion();

    private static Rect2 ScreenBounds(NCreature view)
    {
        var bounds = view.Visuals.GetNodeOrNull<Control>("%Bounds") ?? view.Hitbox;
        var transform = bounds.GetGlobalTransformWithCanvas();
        var rect = new Rect2(transform * Vector2.Zero, Vector2.Zero);
        rect = rect.Expand(transform * new Vector2(bounds.Size.X, 0));
        rect = rect.Expand(transform * new Vector2(0, bounds.Size.Y));
        return rect.Expand(transform * bounds.Size);
    }

    public override void _Ready()
    {
        AddChild(_retained);
        AddChild(_flashes);
    }

    private void AddCut(Transform2D placement, ulong target)
    {
        TrimBudget();
        CutSlot? slot = null;
        int sameTarget = 0;
        foreach (var cut in _cuts) if (cut.Target == target) sameTarget++;
        if (sameTarget >= TargetBudget) slot = Oldest(target);
        else if (_cuts.Count >= BattleBudget) slot = Oldest();
        if (slot == null)
        {
            string id = (_cuts.Count + 1).ToString();
            slot = new CutSlot(MakeSprite(placement, "Scar" + id), MakeSprite(placement, "Hit" + id));
            _retained.AddChild(slot.Retained); _flashes.AddChild(slot.Flash);
            _cuts.Add(slot);
        }
        slot.Target = target; slot.Order = ++_nextScar; slot.Age = 0;
        var mode = SaveManager.Instance.PrefsSave.FastMode;
        slot.Speed = mode == FastModeType.Fast ? 1 / .7f : 1;
        slot.Retained.Transform = slot.Flash.Transform = placement;
        slot.Flash.SelfModulate = Colors.White;
        slot.Flash.Visible = mode != FastModeType.Instant;
    }

    private CutSlot? Oldest(ulong? target = null)
    {
        CutSlot? oldest = null;
        foreach (var cut in _cuts)
            if ((!target.HasValue || cut.Target == target) && (oldest == null || cut.Order < oldest.Order)) oldest = cut;
        return oldest;
    }
    private void TrimBudget()
    {
        for (int i = _cuts.Count - 1; i >= 0; i--)
        {
            var cut = _cuts[i]; int sameTarget = 0;
            foreach (var other in _cuts) if (other.Target == cut.Target) sameTarget++;
            if (_cuts.Count <= BattleBudget && sameTarget <= TargetBudget) continue;
            var retired = sameTarget > TargetBudget ? Oldest(cut.Target)! : Oldest()!;
            _cuts.Remove(retired);
            _retained.RemoveChild(retired.Retained); _flashes.RemoveChild(retired.Flash);
            retired.Retained.QueueFree(); retired.Flash.QueueFree();
        }
    }

    private static Sprite2D MakeSprite(Transform2D placement, string name) => new()
    {
        Name = name,
        Texture = _texture,
        Transform = placement,
        Offset = _texture!.GetSize() * (new Vector2(.5f, .5f) - TexturePivot),
        TextureFilter = TextureFilterEnum.Linear
    };

    public override void _Process(double delta)
    {
        if (!KarenVfxQuality.Enabled || !Present(_room) || NCombatRoom.Instance != _room || !CombatManager.Instance.IsInProgress)
        {
            Hide();
            QueueFree();
            return;
        }

        TrimBudget();
        foreach (var cut in _cuts)
        {
            cut.Age += (float)delta * cut.Speed;
            if (cut.Age >= FadeTime || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant)
            {
                cut.Flash.Hide();
                continue;
            }
            // A short held contact and a smooth, strictly decreasing tail;
            // the static copy underneath never fades to zero or pulses again.
            float progress = Mathf.Clamp((cut.Age - .08f) / (FadeTime - .08f), 0, 1);
            float alpha = 1 - progress * progress * (3 - 2 * progress);
            cut.Flash.SelfModulate = new Color(1, 1, 1, alpha);
        }
    }
}
