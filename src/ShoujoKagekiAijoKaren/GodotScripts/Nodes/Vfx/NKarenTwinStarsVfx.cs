using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Each disabled relic forms a gold/pink pair just beyond its HUD position; native hits launch the stars.</summary>
public partial class NKarenTwinStarsVfx : Node2D
{
    private static readonly CanvasItemMaterial StarMaterial = new() { BlendMode = CanvasItemMaterial.BlendModeEnum.Mix };
    internal static void PrepareResources() { }
    internal static Godot.Resource?[] PreparedResources => [StarMaterial];

    private const float CondenseTime = .52f;
    private const float FlightTime = .16f;
    private const float ImpactTime = .27f;
    private NCombatRoom _room = null!;
    private NCreature _owner = null!;
    private readonly List<StarShot> _shots = new();
    private readonly List<Vector2?> _relicSources = new();
    private readonly List<StarPair> _pairs = new();
    private float _age, _unit = 1, _speed = 1, _finishedAt = -1;
    private StarShot? _current;
    private static readonly Color Gold = new("#ffc968"), Pink = new("#ffacd9"), Ivory = new("#fff9e6");
    private static bool Enabled => !NonInteractiveMode.IsActive
        && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant;
    private static bool Present(Node? node) => GodotObject.IsInstanceValid(node)
        && node!.IsInsideTree() && !node.IsQueuedForDeletion();

    private sealed class StarPair
    {
        internal Vector2? RelicSource;
        internal Vector2 Center;
        internal float Age;
        internal bool GoldLaunched, PinkLaunched;
        internal Vector2 Position(bool large, float unit) => Center + new Vector2(large ? -14 : 16, large ? -22 : 19) * unit;
    }

    private sealed class StarShot
    {
        internal StarPair Pair = null!;
        internal NCreature? View;
        internal Vector2 Source, Target;
        internal int Index;
        internal float Age, LaunchedAt = -1, ContactAt = -1;
        internal bool Large => Index % 2 == 0;
    }

    // Save HUD coordinates before the relic holders are replaced by locks.
    // Nothing is drawn until an actual native hit requests its pair.
    public static NKarenTwinStarsVfx? Begin(Player player, Creature victim, int relicCount)
    {
        var room = NCombatRoom.Instance;
        if (relicCount <= 0 || !Enabled || !Present(room) || !CombatManager.Instance.IsInProgress
            || room!.GetCreatureNode(player.Creature) is not { } owner || room.GetCreatureNode(victim) == null)
            return null;
        var effect = new NKarenTwinStarsVfx { _room = room, _owner = owner, Name = "KarenTwinStars" };
        foreach (var relic in DisableRelicManager.GetRelicsToDisable(player, relicCount))
        {
            var holder = DisableRelicNodeManager.FindRelicHolder(relic, player);
            effect._relicSources.Add(Present(holder)
                ? holder!.GetGlobalTransformWithCanvas() * (holder.Size * .5f) : null);
        }
        room.CombatVfxContainer.AddChildSafely(effect);
        return effect;
    }

    public override void _Ready()
    {
        TopLevel = true;
        ZAsRelative = false;
        ZIndex = 22;
        Material = StarMaterial;
        _speed = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? 1 / .7f : 1;
        PlaceCanvas();
    }

    // Called only for an actual native hit target. No speculative six-projectile volley.
    public void Launch(Creature victim, int index)
    {
        if (!Present(this) || _finishedAt >= 0 || _room.GetCreatureNode(victim) is not { } view) return;
        PlaceCanvas();
        int pairIndex = index / 2;
        var bounds = Bounds(view);
        var pair = index % 2 == 0 || _pairs.Count == 0
            ? CreatePair(pairIndex, bounds.GetCenter()) : _pairs[^1];
        _current = new StarShot { Pair = pair, View = view, Index = index,
            Source = pair.Position(index % 2 == 0, _unit),
            Target = bounds.GetCenter() + new Vector2(0, (index % 2 == 0 ? -10 : 11) * _unit) };
        _shots.Add(_current);
    }

    private StarPair CreatePair(int pairIndex, Vector2 target)
    {
        Vector2? relicSource = pairIndex < _relicSources.Count ? _relicSources[pairIndex] : null;
        // With no local HUD (e.g. a remote player), form the pair near the target without inventing relic motes.
        var origin = relicSource ?? target + new Vector2(-420, -230) * _unit;
        var path = target - origin;
        // Form shortly after leaving the relic, along its direct path to the enemy.
        var center = origin + path.Normalized() * Mathf.Min(path.Length() * .16f, 220 * _unit);
        var pair = new StarPair { RelicSource = relicSource, Center = center };
        _pairs.Add(pair);
        return pair;
    }

    // Wait for this relic's light to form both stars; there is no separate global charge-up.
    // Native Instant and noninteractive modes create no node and add no wait.
    public async Task TravelToContact()
    {
        var shot = _current;
        if (!Present(this) || shot == null) return;
        float remaining = Mathf.Max(0, CondenseTime - shot.Pair.Age);
        if (remaining > 0) await Cmd.CustomScaledWait(remaining * .7f, remaining);
        if (!Present(this) || _finishedAt >= 0 || !CombatManager.Instance.IsInProgress) return;
        shot.LaunchedAt = shot.Age;
        if (shot.Large) shot.Pair.GoldLaunched = true;
        else shot.Pair.PinkLaunched = true;
        await Cmd.CustomScaledWait(FlightTime * .7f, FlightTime);
        ConfirmContact();
    }

    public void ConfirmContact()
    {
        if (!Present(this) || _current == null || _current.ContactAt >= 0) return;
        _current.ContactAt = _current.Age;
        NDebugAudioManager.Instance?.Play("slash_attack.mp3", _current.Large ? .30f : .24f);
    }

    public void Finish()
    {
        if (GodotObject.IsInstanceValid(this) && !IsQueuedForDeletion()) _finishedAt = _age;
    }

    public override void _Process(double delta)
    {
        float step = (float)delta * _speed;
        _age += step;
        if (!Present(_room) || _room != NCombatRoom.Instance || !CombatManager.Instance.IsInProgress
            || !Present(_owner) || (_finishedAt >= 0 && _age - _finishedAt > ImpactTime + .05f))
        { Hide(); QueueFree(); return; }
        PlaceCanvas();
        foreach (var pair in _pairs) pair.Age += step;
        foreach (var shot in _shots)
        {
            shot.Age += step;
            if (shot.ContactAt < 0 && Present(shot.View))
                shot.Target = Bounds(shot.View!).GetCenter() + new Vector2(0, (shot.Large ? -10 : 11) * _unit);
        }
        _pairs.RemoveAll(p => p.PinkLaunched);
        _shots.RemoveAll(s => s.ContactAt >= 0 && s.Age - s.ContactAt >= ImpactTime);
        QueueRedraw();
    }

    private void PlaceCanvas()
    {
        GlobalTransform = GetCanvasTransform().AffineInverse();
        _unit = Mathf.Clamp(GetViewport().GetVisibleRect().Size.Y / 1080f, .5f, 2f);
    }

    private static Rect2 Bounds(NCreature view)
    {
        var bounds = view.Visuals.GetNodeOrNull<Control>("%Bounds") ?? view.Hitbox;
        var transform = bounds.GetGlobalTransformWithCanvas();
        var rect = new Rect2(transform * Vector2.Zero, Vector2.Zero);
        return rect.Expand(transform * new Vector2(bounds.Size.X, 0))
            .Expand(transform * new Vector2(0, bounds.Size.Y)).Expand(transform * bounds.Size);
    }

    private static float P(float t, float start, float end) => Mathf.Clamp((t - start) / (end - start), 0, 1);
    private static Color Alpha(Color color, float alpha) => new(color, Mathf.Clamp(alpha, 0, 1));
}
