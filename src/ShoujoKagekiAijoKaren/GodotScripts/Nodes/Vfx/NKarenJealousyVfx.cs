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

/// <summary>Independent square-foil attack and contact flash. Never changes a creature visual.</summary>
public partial class NKarenJealousyVfx : Node2D
{
    public const float HitTime = .5f;
    public const float FastHitTime = HitTime * .7f;
    private const int SweepSquares = 18, ImpactSquares = 28;
    private NCombatRoom _room = null!;
    private NCreature? _sourceView;
    private NCreature _targetView = null!;
    private readonly Node2D _light = new();
    private Vector2 _source, _target;
    private float _age, _unit = 1, _side = 1, _speed = 1, _confirmedAt = -1;
    private bool _impact;
    private static GradientTexture2D? _soft;
    private static bool Enabled => !NonInteractiveMode.IsActive
        && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant;

    public static NKarenJealousyVfx? Create(Creature source, Creature target)
    {
        var room = NCombatRoom.Instance;
        if (!Enabled || room == null || !CombatManager.Instance.IsInProgress
            || room.GetCreatureNode(source) is not { } from || room.GetCreatureNode(target) is not { } to) return null;
        return new NKarenJealousyVfx { _room = room, _sourceView = from, _targetView = to, Name = "KarenJealousySwing" };
    }

    public static NKarenJealousyVfx? CreateImpact(Creature target, NKarenJealousyVfx? sweep)
    {
        if (Present(sweep)) sweep!._confirmedAt = sweep._age;
        var room = NCombatRoom.Instance;
        if (!Enabled || room == null || room.GetCreatureNode(target) is not { } view) return null;
        // Native TmpHitSfx precedes WaitBeforeHit; contact audio belongs to the real hit callback.
        NDebugAudioManager.Instance?.Play("blunt_attack.mp3");
        return new NKarenJealousyVfx { _room = room, _targetView = view, _impact = true,
            _unit = Present(sweep) ? sweep!._unit : 1, _side = Present(sweep) ? sweep!._side : 1,
            Name = "KarenJealousyImpact" };
    }

    public override void _Ready()
    {
        TopLevel = true;
        GlobalTransform = Transform2D.Identity;
        ZIndex = 20;
        _speed = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? 1 / .7f : 1;
        _soft ??= new GradientTexture2D { Width = 128, Height = 128, Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new(.5f, .5f), FillTo = new(1, .5f), Gradient = new Gradient {
                Offsets = [0, .2f, .55f, 1], Colors = [Colors.White, new(1, 1, 1, .5f), new(1, 1, 1, .06f), Colors.Transparent] } };
        _light.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        AddChild(_light);
        _light.Draw += DrawLight;
        Place(); QueueRedraw(); _light.QueueRedraw();
    }

    private static bool Present(Node? node) => GodotObject.IsInstanceValid(node)
        && node!.IsInsideTree() && !node.IsQueuedForDeletion();
    private Rect2 Bounds(NCreature view)
    {
        var boundsNode = view.Visuals.GetNodeOrNull<Control>("%Bounds") ?? view.Hitbox;
        var matrix = GetGlobalTransformWithCanvas().AffineInverse() * boundsNode.GetGlobalTransformWithCanvas();
        return new Rect2(matrix * Vector2.Zero, Vector2.Zero)
            .Expand(matrix * new Vector2(boundsNode.Size.X, 0))
            .Expand(matrix * new Vector2(0, boundsNode.Size.Y)).Expand(matrix * boundsNode.Size);
    }
    private void Place()
    {
        if (Present(_targetView)) _target = Bounds(_targetView).GetCenter();
        if (_impact || !Present(_sourceView)) return;
        var bounds = Bounds(_sourceView!);
        _unit = Mathf.Clamp(bounds.Size.Y / 240f, .35f, 3f);
        _side = _target.X >= bounds.GetCenter().X ? 1 : -1;
        _source = bounds.GetCenter() + new Vector2(_side * bounds.Size.X * .32f, -bounds.Size.Y * .12f);
    }
    private Vector2 FlightPoint(float progress) => _source.Lerp(_target, progress)
        + Vector2.Up * Mathf.Sin(progress * Mathf.Pi) * 68 * _unit;

    public override void _Process(double delta)
    {
        _age += (float)delta * _speed;
        bool cancelled = !Present(_targetView) || (!_impact && (!Present(_sourceView)
            || (!CombatManager.Instance.IsInProgress && _confirmedAt < 0)));
        float lifetime = _impact ? .62f : _confirmedAt >= 0 ? _confirmedAt + .12f : HitTime + .45f;
        if (!Present(_room) || NCombatRoom.Instance != _room || cancelled || _age >= lifetime) {
            Hide(); QueueFree(); return;
        }
        Place(); QueueRedraw(); _light.QueueRedraw();
    }
}
