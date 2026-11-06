using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Wind-up and flight belong to one attack; only its real hit creates the impact.</summary>
public partial class NKarenBaseballVfx : Node2D
{
    // Shared visual resources are prepared during the run loading screen as well as on demand.
    internal static void PrepareResources()
    {
        _soft ??= new GradientTexture2D { Width = 128, Height = 128, Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new(.5f, .5f), FillTo = new(1, .5f), Gradient = new Gradient {
                Offsets = [0, .2f, .5f, 1], Colors = [Colors.White, new(1, 1, 1, .6f), new(1, 1, 1, .12f), Colors.Transparent] } };
    }
    internal static Godot.Resource?[] PreparedResources => [_soft];

    public const float HitTime = .35f;
    public const float FastHitTime = HitTime * .7f;
    private const float SwingTime = .15f;
    private NCombatRoom _room = null!;
    private NCreature? _sourceView;
    private NCreature _targetView = null!;
    private readonly Node2D _light = new() { ShowBehindParent = true };
    private float _age, _unit = 1, _side = 1, _speed = 1, _confirmedAt = -1;
    private bool _impact;
    private Vector2 _source, _target, _launch;
    private static GradientTexture2D? _soft;

    private static bool Enabled => KarenVfxQuality.Enabled && !NonInteractiveMode.IsActive
        && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant;

    public static NKarenBaseballVfx? Create(Creature source, Creature target)
    {
        var room = NCombatRoom.Instance;
        if (!Enabled || room == null || !CombatManager.Instance.IsInProgress
            || room.GetCreatureNode(source) is not { } from || room.GetCreatureNode(target) is not { } to) return null;
        return new NKarenBaseballVfx { _room = room, _sourceView = from, _targetView = to, Name = "KarenBaseballFlight" };
    }

    public static Node2D? CreateImpact(Creature target, NKarenBaseballVfx? flight)
    {
        // Retire the ball at the real hit, including a hitch that delayed a visual frame.
        if (GodotObject.IsInstanceValid(flight) && flight!.IsInsideTree() && !flight.IsQueuedForDeletion())
            flight._confirmedAt = flight._age;
        var room = NCombatRoom.Instance;
        if (!Enabled || room == null || room.GetCreatureNode(target) is not { } view) return null;
        NDebugAudioManager.Instance?.Play("blunt_attack.mp3");
        return new NKarenBaseballVfx { _room = room, _targetView = view, _impact = true,
            _unit = GodotObject.IsInstanceValid(flight) ? flight!._unit : 1,
            _side = GodotObject.IsInstanceValid(flight) ? flight!._side : 1, Name = "KarenBaseballImpact" };
    }

    public override void _Ready()
    {
        TopLevel = true;
        GlobalTransform = Transform2D.Identity;
        ZIndex = 20;
        _speed = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? 1 / .7f : 1;
        PrepareResources();
        _light.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        AddChild(_light);
        _light.Draw += DrawLight;
        Place();
        KarenVfxQuality.Redraw(this); KarenVfxQuality.Redraw(_light);
    }

    private static bool Present(NCreature? view) => GodotObject.IsInstanceValid(view)
        && view!.IsInsideTree() && !view.IsQueuedForDeletion();

    private Rect2 Bounds(NCreature view)
    {
        // Native ScaleTo changes Visuals without always rebuilding the interaction hitbox.
        // Follow the rendered bounds so scaling, flipping and moving poses stay attached.
        var hitbox = view.Visuals.GetNodeOrNull<Control>("%Bounds") ?? view.Hitbox;
        var transform = GetGlobalTransformWithCanvas().AffineInverse() * hitbox.GetGlobalTransformWithCanvas();
        var bounds = new Rect2(transform * Vector2.Zero, Vector2.Zero);
        bounds = bounds.Expand(transform * new Vector2(hitbox.Size.X, 0));
        bounds = bounds.Expand(transform * new Vector2(0, hitbox.Size.Y));
        return bounds.Expand(transform * hitbox.Size);
    }

    private void Place()
    {
        if (Present(_targetView)) _target = Bounds(_targetView).GetCenter();
        if (_impact || !Present(_sourceView)) return;
        var bounds = Bounds(_sourceView!);
        _unit = Mathf.Clamp(bounds.Size.Y / 240f, .35f, 3f);
        _side = _target.X >= bounds.GetCenter().X ? 1 : -1;
        _source = bounds.GetCenter() + new Vector2(_side * bounds.Size.X * .12f, -bounds.Size.Y * .05f);
        _launch = SwingPoint(1);
    }

    public override void _Process(double delta)
    {
        _age += (float)delta * _speed;
        bool roomGone = !GodotObject.IsInstanceValid(_room) || !_room.IsInsideTree() || NCombatRoom.Instance != _room;
        bool flightCancelled = !_impact && _confirmedAt < 0
            && (!CombatManager.Instance.IsInProgress || !Present(_sourceView) || !Present(_targetView));
        bool expired = _impact ? _age >= .43f : _confirmedAt >= 0 ? _age - _confirmedAt >= .16f : _age >= .8f;
        if (roomGone || flightCancelled || expired) { Hide(); QueueFree(); return; }
        // A removed target leaves a short fading impact at its last valid position.
        Place();
        KarenVfxQuality.Redraw(this); KarenVfxQuality.Redraw(_light);
    }

    private Vector2 SwingPoint(float t)
    {
        float angle = Mathf.Lerp(-2.25f, .15f, t);
        return _source + new Vector2(_side * Mathf.Cos(angle) * 90, Mathf.Sin(angle) * 78) * _unit;
    }

    private Vector2 FlightPoint(float t) => _launch.Lerp(_target, t)
        + Vector2.Up * (Mathf.Sin(t * Mathf.Pi) * 45 * _unit);

    public override void _Draw()
    {
        if (_impact) return;
        if (_confirmedAt >= 0) return;
        if (_age < SwingTime)
            Ball(SwingPoint(Mathf.SmoothStep(0, 1, _age / SwingTime)), 13 * _unit);
        else
            Ball(FlightPoint(Mathf.Clamp((_age - SwingTime) / (HitTime - SwingTime), 0, 1)), 20 * _unit);
    }
}
