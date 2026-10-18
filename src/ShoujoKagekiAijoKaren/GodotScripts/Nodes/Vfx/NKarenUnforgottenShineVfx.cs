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
    public const float HitTime = .20f;
    public const float FastHitTime = HitTime * .7f;
    private const float FadeTime = 1.2f;
    private const float RetainedOpacity = .20f;
    private const string GroupName = "KarenUnforgottenScars";
    private static readonly Vector2 TexturePivot = new(.5f, .5f);
    private static Texture2D? _texture;

    private sealed class FadingCut(Sprite2D sprite, float speed)
    {
        public readonly Sprite2D Sprite = sprite;
        public readonly float Speed = speed;
        public float Age;
    }

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
    private readonly List<FadingCut> _fading = new();
    private NCombatRoom _room = null!;
    private int _nextScar;

    public static NKarenUnforgottenShineVfx? Create(Creature owner, Creature target)
    {
        var room = NCombatRoom.Instance;
        if (NonInteractiveMode.IsActive || room == null || !CombatManager.Instance.IsInProgress
            || room.GetCreatureNode(owner) is not { } attacker
            || room.GetCreatureNode(target) is not { } victim)
            return null;

        _texture ??= KarenResourceLoader.LoadTexture(
            "res://images/vfx/unforgettable/lingering-star.png", nameof(NKarenUnforgottenShineVfx));
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

        group.AddCut(placement);
        // AttackCommand attaches a newly returned node. An existing battle group
        // receives the next cut in place and must not be attached a second time.
        return existing ? null : group;
    }

    private Vector2 PickHitPoint(Vector2 center, Vector2 spread, float size)
    {
        var neighbors = new List<Vector2>();
        foreach (var node in _retained.GetChildren())
        {
            if (node is not Sprite2D sprite) continue;
            var anchor = sprite.GetGlobalTransformWithCanvas().Origin;
            if (anchor.DistanceSquaredTo(center) < size * size * 2.25f)
                neighbors.Add(anchor);
        }

        // Best of eight bounded samples keeps successive scars distinct without
        // pushing a hit away from the target or retrying forever in a crowded area.
        Vector2 best = center;
        float bestDistance = -1;
        for (int sample = 0; sample < 8; sample++)
        {
            float direction = GD.Randf() * Mathf.Tau;
            var candidate = center + Vector2.FromAngle(direction) * spread * Mathf.Sqrt(GD.Randf());
            if (neighbors.Count == 0) return candidate;
            float nearest = float.MaxValue;
            foreach (var anchor in neighbors)
                nearest = Mathf.Min(nearest, candidate.DistanceSquaredTo(anchor));
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

    private void AddCut(Transform2D placement)
    {
        int id = ++_nextScar;
        _retained.AddChild(MakeSprite(placement, $"Scar{id}"));
        var mode = SaveManager.Instance.PrefsSave.FastMode;
        if (mode == FastModeType.Instant) return;

        var flash = MakeSprite(placement, $"Hit{id}");
        _flashes.AddChild(flash);
        _fading.Add(new FadingCut(flash, mode == FastModeType.Fast ? 1 / .7f : 1));
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
        if (!Present(_room) || NCombatRoom.Instance != _room || !CombatManager.Instance.IsInProgress)
        {
            Hide();
            QueueFree();
            return;
        }

        for (int i = _fading.Count - 1; i >= 0; i--)
        {
            var cut = _fading[i];
            cut.Age += (float)delta * cut.Speed;
            if (cut.Age >= FadeTime || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant)
            {
                cut.Sprite.Hide();
                cut.Sprite.QueueFree();
                _fading.RemoveAt(i);
                continue;
            }
            // A short held contact and a smooth, strictly decreasing tail;
            // the static copy underneath never fades to zero or pulses again.
            float progress = Mathf.Clamp((cut.Age - .08f) / (FadeTime - .08f), 0, 1);
            float alpha = 1 - progress * progress * (3 - 2 * progress);
            cut.Sprite.SelfModulate = new Color(1, 1, 1, alpha);
        }
    }
}
