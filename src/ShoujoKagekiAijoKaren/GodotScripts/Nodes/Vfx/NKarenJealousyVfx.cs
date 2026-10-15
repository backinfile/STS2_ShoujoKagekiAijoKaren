using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Audio;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>A mint crescent cuts a gold star clasp; only a confirmed relic reward raises a new clasp.</summary>
public partial class NKarenJealousyVfx : Node2D
{
    public const float HitTime = .42f;
    public const float FastHitTime = HitTime * .7f;
    private NCombatRoom _room = null!;
    private NCreature? _sourceView;
    private NCreature _targetView = null!;
    private readonly Node2D _light = new();
    private AudioStreamPlayer? _sound;
    private Vector2 _source, _target;
    private float _age, _unit = 1, _side = 1, _speed = 1, _confirmedAt = -1;
    private bool _impact, _reward;
    private int _rewardCount;
    private static GradientTexture2D? _soft;
    private static bool Enabled => !NonInteractiveMode.IsActive
        && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant;

    public static NKarenJealousyVfx? Create(Creature source, Creature target)
    {
        var room = NCombatRoom.Instance;
        if (!Enabled || room == null || !CombatManager.Instance.IsInProgress
            || room.GetCreatureNode(source) is not { } from || room.GetCreatureNode(target) is not { } to) return null;
        return new NKarenJealousyVfx { _room = room, _sourceView = from, _targetView = to, Name = "KarenJealousySweep" };
    }

    public static NKarenJealousyVfx? CreateImpact(Creature target, NKarenJealousyVfx? sweep)
    {
        if (Present(sweep)) sweep!._confirmedAt = sweep._age;
        var room = NCombatRoom.Instance;
        if (!Enabled || room == null || room.GetCreatureNode(target) is not { } view) return null;
        return new NKarenJealousyVfx { _room = room, _targetView = view, _impact = true,
            _unit = Present(sweep) ? sweep!._unit : 1, _side = Present(sweep) ? sweep!._side : 1,
            Name = "KarenJealousyImpact" };
    }

    public static void ConfirmReward(Creature owner, int count)
    {
        if (!Enabled || count <= 0 || NCombatRoom.Instance is not { } room) return;
        // Reward commands may resume off the scene thread. Attach only on Godot's thread.
        Callable.From(() => {
            if (!GodotObject.IsInstanceValid(room) || NCombatRoom.Instance != room || !CombatManager.Instance.IsInProgress
                || room.GetCreatureNode(owner) is not { } view) return;
            room.CombatVfxContainer.AddChild(new NKarenJealousyVfx { _room = room, _targetView = view,
                _reward = true, _rewardCount = Mathf.Clamp(count, 1, 2), Name = "KarenJealousyReward" });
        }).CallDeferred();
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
        AddChild(_light);
        _light.Draw += DrawLight;
        Place();
        if (_reward) _sound = KarenAudioManager.PlaySfxOnGameBus("karen_star_resolve.wav", .3f, 1.12f);
        QueueRedraw(); _light.QueueRedraw();
    }

    private static bool Present(Node? node) => GodotObject.IsInstanceValid(node)
        && node!.IsInsideTree() && !node.IsQueuedForDeletion();

    private Rect2 Bounds(NCreature view)
    {
        var hitbox = view.Visuals.GetNodeOrNull<Control>("%Bounds") ?? view.Hitbox;
        var transform = GetGlobalTransformWithCanvas().AffineInverse() * hitbox.GetGlobalTransformWithCanvas();
        var bounds = new Rect2(transform * Vector2.Zero, Vector2.Zero);
        bounds = bounds.Expand(transform * new Vector2(hitbox.Size.X, 0));
        bounds = bounds.Expand(transform * new Vector2(0, hitbox.Size.Y));
        return bounds.Expand(transform * hitbox.Size);
    }

    private void Place()
    {
        if (Present(_targetView)) {
            var bounds = Bounds(_targetView);
            _target = bounds.GetCenter();
            if (_reward) {
                _unit = Mathf.Clamp(bounds.Size.Y / 240f, .35f, 3f);
                _target += new Vector2(bounds.Size.X * .35f, -bounds.Size.Y * .68f);
            }
        }
        if (_impact || _reward || !Present(_sourceView)) return;
        var from = Bounds(_sourceView!);
        _unit = Mathf.Clamp(from.Size.Y / 240f, .35f, 3f);
        _side = _target.X >= from.GetCenter().X ? 1 : -1;
        _source = from.GetCenter() + new Vector2(_side * from.Size.X * .2f, -from.Size.Y * .1f);
    }

    public override void _Process(double delta)
    {
        _age += (float)delta * _speed;
        bool roomGone = !Present(_room) || NCombatRoom.Instance != _room;
        bool cancelled = !_impact && (!CombatManager.Instance.IsInProgress || !Present(_targetView)
            || (!_reward && !Present(_sourceView)));
        float lifetime = _reward ? 1.18f : _impact ? .56f : _confirmedAt >= 0 ? _confirmedAt + .18f : HitTime + .4f;
        if (roomGone || cancelled || _age >= lifetime) { Hide(); QueueFree(); return; }
        Place(); QueueRedraw(); _light.QueueRedraw();
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_sound)) { _sound!.Stop(); _sound.QueueFree(); }
    }

    private Vector2 SweepPoint(float t) => _source.Lerp(_target, t)
        + Vector2.Up * (Mathf.Sin(t * Mathf.Pi) * 72 * _unit);
    private Vector2 CutPoint(float t) => _target
        + new Vector2(_side * Mathf.Lerp(-135, 140, t), Mathf.Lerp(-74, 76, t) - Mathf.Sin(t * Mathf.Pi) * 24) * _unit;
    private Vector2 RewardPoint(int index) => _target
        + new Vector2((_rewardCount == 1 ? 0 : index == 0 ? -23 : 23), -42 * Mathf.SmoothStep(0, 1, _age / .8f)) * _unit;
}
