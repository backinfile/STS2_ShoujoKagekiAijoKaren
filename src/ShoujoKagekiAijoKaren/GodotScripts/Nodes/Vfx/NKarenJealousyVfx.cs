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

/// <summary>Contact-only cut and foil particles drifting toward the actual player. No flight path.</summary>
public partial class NKarenJealousyVfx : Node2D
{
    public const float HitTime = .5f;
    public const float FastHitTime = HitTime * .7f;
    private NCombatRoom _room = null!;
    private NCreature _sourceView = null!;
    private NCreature _targetView = null!;
    private readonly Node2D _light = new();
    private Vector2 _source, _target, _towardSource;
    private float _age, _unit = 1, _side = 1, _speed = 1;
    private static GradientTexture2D? _soft;
    private static bool Enabled => !NonInteractiveMode.IsActive
        && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant;

    public static NKarenJealousyVfx? CreateImpact(Creature source, Creature target)
    {
        var room = NCombatRoom.Instance;
        if (!Enabled || room == null || !CombatManager.Instance.IsInProgress
            || room.GetCreatureNode(source) is not { } from || room.GetCreatureNode(target) is not { } to) return null;
        // Native TmpHitSfx precedes WaitBeforeHit; contact audio belongs to the real hit callback.
        NDebugAudioManager.Instance?.Play("blunt_attack.mp3");
        return new NKarenJealousyVfx { _room = room, _sourceView = from, _targetView = to, Name = "KarenJealousyImpact" };
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
        Place(); InitializeParticles(); UpdateParticles(); _light.QueueRedraw();
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
        if (!Present(_sourceView)) return;
        var bounds = Bounds(_sourceView);
        _unit = Mathf.Clamp(bounds.Size.Y / 240f, .35f, 3f);
        _side = _target.X >= bounds.GetCenter().X ? 1 : -1;
        _source = bounds.GetCenter();
        var toward = _source - _target;
        _towardSource = toward.LengthSquared() > .001f ? toward.Normalized() : Vector2.Left;
    }

    public override void _Process(double delta)
    {
        _age += (float)delta * _speed;
        bool cancelled = !Present(_targetView) || !Present(_sourceView);
        if (!Present(_room) || NCombatRoom.Instance != _room || cancelled || _age >= .88f) {
            Hide(); QueueFree(); return;
        }
        Place(); UpdateParticles(); _light.QueueRedraw();
    }
}
