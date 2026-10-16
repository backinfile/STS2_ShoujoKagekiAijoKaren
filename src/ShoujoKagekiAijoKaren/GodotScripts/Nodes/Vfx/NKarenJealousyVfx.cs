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

/// <summary>A connected mace swing and reversible actor lunge. No reward presentation.</summary>
public partial class NKarenJealousyVfx : Node2D
{
    public const float HitTime = .5f;
    public const float FastHitTime = HitTime * .7f;
    private const float WindupTime = .18f, ReturnTime = .3f, ShaftLength = 142;
    private NCombatRoom _room = null!;
    private NCreature? _sourceView;
    private NCreature _targetView = null!;
    private Sprite2D? _actor;
    private int _actorZLift;
    private readonly Node2D _light = new();
    private readonly Sprite2D[] _ghosts = new Sprite2D[3];
    private readonly float[] _ghostBorn = [-1, -1, -1];
    private int _ghostIndex;
    private float _nextGhost = WindupTime;
    private Vector2 _source, _target, _actorTravel, _actorOffsetLocal;
    private float _age, _unit = 1, _side = 1, _speed = 1, _confirmedAt = -1;
    private bool _impact;
    private static GradientTexture2D? _soft;
    private static ShaderMaterial? _ghostMaterial;
    private static bool Enabled => !NonInteractiveMode.IsActive
        && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant;

    public static NKarenJealousyVfx? Create(Creature source, Creature target)
    {
        var room = NCombatRoom.Instance;
        if (!Enabled || room == null || !CombatManager.Instance.IsInProgress
            || room.GetCreatureNode(source) is not { } from || room.GetCreatureNode(target) is not { } to) return null;
        return new NKarenJealousyVfx { _room = room, _sourceView = from, _targetView = to, Name = "KarenJealousySwing" };
    }

    public static NKarenJealousyVfx? CreateImpact(Creature target, NKarenJealousyVfx? swing)
    {
        if (Present(swing)) swing!._confirmedAt = swing._age;
        var room = NCombatRoom.Instance;
        if (!Enabled || room == null || room.GetCreatureNode(target) is not { } view) return null;
        // Native TmpHitSfx precedes WaitBeforeHit. Play the contact sound at the contact callback.
        NDebugAudioManager.Instance?.Play("blunt_attack.mp3");
        return new NKarenJealousyVfx { _room = room, _targetView = view, _impact = true,
            _unit = Present(swing) ? swing!._unit : 1, _side = Present(swing) ? swing!._side : 1,
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
                Offsets = [0, .18f, .5f, 1], Colors = [Colors.White, new(1, 1, 1, .55f), new(1, 1, 1, .1f), Colors.Transparent] } };
        _light.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        AddChild(_light); _light.Draw += DrawLight;
        if (!_impact && Present(_sourceView) && _sourceView!.Body is Sprite2D actor) {
            _actor = actor;
            // The source normally draws before the monsters. Keep the moving body in front at contact.
            int raised = Mathf.Clamp(actor.ZIndex + 18, -4096, 4095);
            _actorZLift = raised - actor.ZIndex;
            actor.ZIndex = raised;
            _ghostMaterial ??= new ShaderMaterial { Shader = new Shader { Code = """
                shader_type canvas_item;
                render_mode unshaded, blend_add;
                void fragment() { COLOR = vec4(0.23, 0.75, 0.63, texture(TEXTURE, UV).a * COLOR.a); }
                """ } };
            for (int i = 0; i < _ghosts.Length; i++) {
                _ghosts[i] = new Sprite2D { Texture = actor.Texture, Centered = actor.Centered,
                    Hframes = actor.Hframes, Vframes = actor.Vframes, Frame = actor.Frame,
                    RegionEnabled = actor.RegionEnabled, RegionRect = actor.RegionRect,
                    FlipH = actor.FlipH, FlipV = actor.FlipV, Material = _ghostMaterial, Visible = false, ZIndex = -6 };
                AddChild(_ghosts[i]);
            }
        }
        Place(); QueueRedraw(); _light.QueueRedraw();
    }

    private static bool Present(Node? node) => GodotObject.IsInstanceValid(node)
        && node!.IsInsideTree() && !node.IsQueuedForDeletion();
    private Rect2 Bounds(NCreature view)
    {
        var boundsNode = view.Visuals.GetNodeOrNull<Control>("%Bounds") ?? view.Hitbox;
        var transform = GetGlobalTransformWithCanvas().AffineInverse() * boundsNode.GetGlobalTransformWithCanvas();
        var bounds = new Rect2(transform * Vector2.Zero, Vector2.Zero);
        bounds = bounds.Expand(transform * new Vector2(boundsNode.Size.X, 0));
        bounds = bounds.Expand(transform * new Vector2(0, boundsNode.Size.Y));
        return bounds.Expand(transform * boundsNode.Size);
    }
    private void Place()
    {
        if (Present(_targetView)) _target = Bounds(_targetView).GetCenter();
        if (_impact || !Present(_sourceView)) return;
        var from = Bounds(_sourceView!);
        _unit = Mathf.Clamp(from.Size.Y / 240f, .35f, 3f);
        _side = _target.X >= from.GetCenter().X ? 1 : -1;
        _source = from.GetCenter() + new Vector2(_side * from.Size.X * .16f, -from.Size.Y * .4f);
    }
    private Vector2 Direction(float angle) => new(_side * Mathf.Cos(angle), Mathf.Sin(angle));
    private float SwingAngle(float age) => age < WindupTime
        ? Mathf.Lerp(-1.7f, -2.65f, Mathf.SmoothStep(0, WindupTime, age))
        : Mathf.Lerp(-2.65f, .13f, Mathf.SmoothStep(WindupTime, HitTime, age));
    private Vector2 Landing => _target - _source - Direction(.13f) * ShaftLength * _unit;
    private Vector2 Travel(float age)
    {
        var back = new Vector2(-16 * _side, 3) * _unit;
        if (age < WindupTime) return back * Mathf.SmoothStep(0, WindupTime, age);
        return back.Lerp(Landing, Mathf.SmoothStep(WindupTime, HitTime - .035f, age));
    }
    private Vector2 HeadPoint(float age) => _source + Travel(age) + Direction(SwingAngle(age)) * ShaftLength * _unit;

    private void AnimateActor()
    {
        if (!Present(_actor)) return;
        _actorTravel = _confirmedAt < 0 ? Travel(Mathf.Min(_age, HitTime))
            : Landing * (1 - Mathf.SmoothStep(0, ReturnTime, _age - _confirmedAt));
        // Sprite Offset is independent of native attack/hurt position tracks. Own only this additive component.
        var canvasTravel = GetGlobalTransformWithCanvas().BasisXform(_actorTravel);
        var nextOffset = _actor!.GetGlobalTransformWithCanvas().AffineInverse().BasisXform(canvasTravel);
        _actor.Offset += nextOffset - _actorOffsetLocal;
        _actorOffsetLocal = nextOffset;
        if (_confirmedAt < 0 && _age >= _nextGhost && _age < HitTime) {
            var ghost = _ghosts[_ghostIndex];
            ghost.Transform = GetGlobalTransformWithCanvas().AffineInverse() * _actor.GetGlobalTransformWithCanvas();
            ghost.Offset = _actor.Offset;
            _ghostBorn[_ghostIndex] = _age;
            _ghostIndex = (_ghostIndex + 1) % _ghosts.Length;
            _nextGhost = _age + .045f;
        }
        for (int i = 0; i < _ghosts.Length; i++) {
            float fade = _ghostBorn[i] < 0 ? 0 : 1 - Mathf.Clamp((_age - _ghostBorn[i]) / .14f, 0, 1);
            _ghosts[i].Visible = fade > .01f;
            _ghosts[i].Modulate = new Color(1, 1, 1, fade * .22f);
        }
    }
    public override void _Process(double delta)
    {
        _age += (float)delta * _speed;
        bool cancelled = !_impact && (!Present(_sourceView) || !Present(_targetView)
            || (!CombatManager.Instance.IsInProgress && _confirmedAt < 0));
        float lifetime = _impact ? .34f : _confirmedAt >= 0 ? _confirmedAt + ReturnTime + .02f : HitTime + .45f;
        if (!Present(_room) || NCombatRoom.Instance != _room || cancelled || _age >= lifetime) { Hide(); QueueFree(); return; }
        Place();
        if (!_impact) AnimateActor();
        QueueRedraw(); _light.QueueRedraw();
    }
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_actor)) {
            _actor!.Offset -= _actorOffsetLocal;
            _actor.ZIndex -= _actorZLift;
        }
        _actorOffsetLocal = Vector2.Zero;
        _actorZLift = 0;
    }
}
