using System;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>One preparation and a thrust that travels through the full victim silhouette per native hit. Visuals never schedule damage.</summary>
public partial class NKarenShineBarrageVfx : Node2D
{
    // Shared visual resources are prepared during the run loading screen as well as on demand.
    internal static void PrepareResources()
    {
        _shader ??= KarenResourceLoader.Load<Shader>("res://shaders/karen_shine_barrage.gdshader", nameof(NKarenShineBarrageVfx));
        _sparkTexture ??= new GradientTexture2D { Width = 12, Height = 40,
            Fill = GradientTexture2D.FillEnum.Radial, FillFrom = new(.5f, .5f), FillTo = new(1, .5f),
            Gradient = new Gradient { Offsets = [0, .3f, 1], Colors = [Colors.White, new(1, 1, 1, .7f), Colors.Transparent] } };
    }
    internal static Godot.Resource?[] PreparedResources => [_shader, _sparkTexture];

    public const float HitInterval = .12f;
    public const float FastHitInterval = HitInterval * .7f;
    private NCombatRoom _room = null!;
    private NCreature _owner = null!;
    private NCreature? _victim;
    private readonly Node2D _blade = new();
    private CpuParticles2D? _sparks;
    private ShaderMaterial? _material;
    private static Shader? _shader;
    private static GradientTexture2D? _sparkTexture;
    private Vector2 _source, _target, _exit, _tip, _tail;
    private Rect2 _targetBounds;
    private float _age, _speed = 1, _unit = 1, _finishedAt = -1;
    private bool _impact, _final, _burstStarted;
    private int _index;
    private static readonly Color Coral = new("#ff526e"), Gold = new("#ffd18b"), Ivory = new("#fff4db");
    private static bool Enabled => !NonInteractiveMode.IsActive
        && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant;
    private static bool Present(Node? node) => GodotObject.IsInstanceValid(node)
        && node!.IsInsideTree() && !node.IsQueuedForDeletion();

    public static NKarenShineBarrageVfx? Create(Creature owner, Creature target)
        => Make(owner, target, false, 0);

    public static NKarenShineBarrageVfx? Hit(Creature owner, Creature target, int index)
    {
        var hit = Make(owner, target, true, index);
        if (hit != null) NDebugAudioManager.Instance?.Play("slash_attack.mp3", .48f);
        return hit;
    }

    private static NKarenShineBarrageVfx? Make(Creature owner, Creature target, bool impact, int index)
    {
        var room = NCombatRoom.Instance;
        if (!Enabled || !Present(room) || !CombatManager.Instance.IsInProgress
            || room!.GetCreatureNode(owner) is not { } from || room.GetCreatureNode(target) is not { } to)
            return null;
        return new NKarenShineBarrageVfx { _room = room, _owner = from, _victim = to,
            _impact = impact, _index = index, Name = impact ? "KarenBarrageHit" : "KarenBarragePreparation" };
    }

    public static void Complete(NKarenShineBarrageVfx? preparation, NKarenShineBarrageVfx? lastHit)
    {
        if (Present(preparation))
        {
            preparation!._finishedAt = preparation._age;
            if (preparation._sparks != null) preparation._sparks.Emitting = false;
        }
        // The native command decides the last actual hit, including hit-count hooks and early kills.
        if (Present(lastHit)) lastHit!._final = true;
    }

    public override void _Ready()
    {
        TopLevel = true;
        GlobalTransform = GetCanvasTransform().AffineInverse();
        ZIndex = 21;
        _speed = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? 1 / .7f : 1;
        Place();
        AddChild(_blade);
        if (_impact)
        {
            PrepareResources();
            _material = new ShaderMaterial { Shader = _shader };
            _blade.Material = _material;
            _blade.Draw += DrawBlade;
        }
        CreateParticles();
        KarenVfxQuality.Redraw(this);
        KarenVfxQuality.Redraw(_blade);
    }

    private static Rect2 Bounds(NCreature view)
    {
        var bounds = view.Visuals.GetNodeOrNull<Control>("%Bounds") ?? view.Hitbox;
        var transform = bounds.GetGlobalTransformWithCanvas();
        var rect = new Rect2(transform * Vector2.Zero, Vector2.Zero);
        rect = rect.Expand(transform * new Vector2(bounds.Size.X, 0));
        rect = rect.Expand(transform * new Vector2(0, bounds.Size.Y));
        return rect.Expand(transform * bounds.Size);
    }

    private void Place()
    {
        GlobalTransform = GetCanvasTransform().AffineInverse();
        if (Present(_victim))
        {
            _targetBounds = Bounds(_victim!);
            _target = _targetBounds.GetCenter();
        }
        if (!Present(_owner)) return;
        var bounds = Bounds(_owner);
        _unit = Mathf.Clamp(bounds.Size.Y / 240f, .4f, 2f);
        float side = _target.X >= bounds.GetCenter().X ? 1 : -1;
        _source = bounds.GetCenter() + new Vector2(bounds.Size.X * .22f * side, -bounds.Size.Y * .10f);
        if (_impact) PlaceThrust();
    }

    private void PlaceThrust()
    {
        var direction = (Contact - _source).Normalized();
        float x = Mathf.Abs(direction.X) < .0001f ? float.PositiveInfinity
            : ((direction.X > 0 ? _targetBounds.End.X : _targetBounds.Position.X) - Contact.X) / direction.X;
        float y = Mathf.Abs(direction.Y) < .0001f ? float.PositiveInfinity
            : ((direction.Y > 0 ? _targetBounds.End.Y : _targetBounds.Position.Y) - Contact.Y) / direction.Y;
        float exitDistance = Mathf.Max(0, Mathf.Min(x, y));
        _exit = Contact + direction * exitDistance;
        var destination = _exit + direction * (140 * _unit);
        // The head crosses the body first; the tail then follows it out of the far side.
        float travel = P(_age, 0, .07f);
        _tip = Contact.Lerp(destination, 1 - Mathf.Pow(1 - travel, 3));
        float follow = Mathf.SmoothStep(0, 1, P(_age, .025f, .22f));
        _tail = _source.Lerp(destination - direction * (24 * _unit), follow);
    }

    public override void _Process(double delta)
    {
        _age += (float)delta * _speed;
        bool preparationCancelled = !_impact && (!CombatManager.Instance.IsInProgress || !Present(_owner) || !Present(_victim));
        float life = _impact ? .36f : _finishedAt >= 0 ? _finishedAt + .16f : 3f;
        if (!Present(_room) || NCombatRoom.Instance != _room || preparationCancelled || _age >= life)
        { Hide(); QueueFree(); return; }
        // Retain the last sampled position if a lethal hit removes the victim's visual.
        Place();
        if (_material != null) _material.SetShaderParameter("age", _age);
        if (_impact && _sparks != null && !_burstStarted && _age >= .04f)
        {
            _sparks.Position = _exit;
            _sparks.Direction = (Contact - _source).Normalized();
            _sparks.Emitting = true;
            _burstStarted = true;
        }
        if (!_impact && _sparks != null)
        {
            _sparks.Position = _source;
            _sparks.Emitting = _finishedAt < 0;
        }
        KarenVfxQuality.Redraw(this);
        KarenVfxQuality.Redraw(_blade);
    }

    private Vector2 Contact => _target + new Vector2(0, ((_index % 4) switch { 0 => -10, 1 => 9, 2 => -3, _ => 3 }) * _unit);
    private static float P(float t, float start, float end) => Mathf.Clamp((t - start) / (end - start), 0, 1);
    private static Color Alpha(Color color, float alpha) => new(color, Mathf.Clamp(alpha, 0, 1));

    private void DrawBlade()
    {
        if (!_impact || _age >= .24f) return;
        var direction = (Contact - _source).Normalized();
        var normal = direction.Orthogonal();
        float progress = P(_age, 0, .24f);
        var tail = _tail + normal * ((_index % 2 == 0 ? -1 : 1) * 5 * _unit * (1 - progress));
        float width = (23 + 5 * (1 - progress)) * _unit;
        _blade.DrawPolygon([tail - normal * width, _tip - normal * width, _tip + normal * width, tail + normal * width],
            [Colors.White, Colors.White, Colors.White, Colors.White], [new(0, 0), new(1, 0), new(1, 1), new(0, 1)]);
    }

    public override void _Draw()
    {
        if (!_impact)
        {
            float alpha = P(_age, 0, .07f) * (1 - P(_age, .16f, .28f));
            float width = (12 + 13 * P(_age, 0, .2f)) * _unit;
            Star(_source, width, width * .32f, Alpha(Ivory, alpha * .8f), -.12f);
            return;
        }
        float fade = 1 - P(_age, .035f, .31f);
        float bloom = 1 + .40f * Mathf.Sin(P(_age, 0, .16f) * Mathf.Pi);
        float finalScale = _final ? 1.18f : 1;
        float size = 39 * _unit * bloom * finalScale;
        // Bright contact is compact, leaving damage numbers and the target silhouette readable.
        Star(Contact, size, size * .72f, Alpha(Coral, fade * .75f), .12f);
        Star(Contact, size * .78f, size * .55f, Alpha(Ivory, fade), .12f);
        Star(Contact, size * .40f, size * .30f, Alpha(Colors.White, fade), .12f);
        if (_final)
        {
            float tail = P(_age, .07f, .12f) * (1 - P(_age, .16f, .36f));
            Star(Contact, 22 * _unit, 34 * _unit, Alpha(Gold, tail * .7f), -.2f);
        }
    }

    private void Star(Vector2 center, float width, float height, Color color, float rotation)
    {
        DrawSetTransform(center, rotation);
        DrawColoredPolygon([new(-width, 0), new(-width * .12f, -height * .12f), new(0, -height),
            new(width * .12f, -height * .12f), new(width, 0), new(width * .12f, height * .12f),
            new(0, height), new(-width * .12f, height * .12f)], color);
        DrawSetTransform(Vector2.Zero);
    }

    private void CreateParticles()
    {
        PrepareResources();
        var scale = new Curve();
        scale.AddPoint(new(0, .35f)); scale.AddPoint(new(.12f, 1)); scale.AddPoint(new(1, 0));
        _sparks = new CpuParticles2D {
            Name = _impact ? "ExitSparks" : "PreparationDust", Emitting = false,
            Amount = KarenVfxQuality.ParticleCount(_impact ? 10 : 5), Lifetime = _impact ? .25f : .20f, OneShot = _impact,
            Explosiveness = _impact ? 1 : 0, LocalCoords = true, SpeedScale = _speed,
            Position = _impact ? _exit : _source, Direction = _impact ? (Contact - _source).Normalized() : Vector2.Up,
            Spread = _impact ? 23 : 25, InitialVelocityMin = (_impact ? 180 : 20) * _unit,
            InitialVelocityMax = (_impact ? 440 : 65) * _unit, Gravity = Vector2.Down * 38 * _unit,
            DampingMin = 90 * _unit, DampingMax = 160 * _unit,
            ParticleFlagAlignY = true, Texture = _sparkTexture,
            ScaleAmountMin = .07f * _unit, ScaleAmountMax = (_impact ? .25f : .12f) * _unit,
            ScaleAmountCurve = scale, Color = _index % 2 == 0 ? Gold : Coral,
            ColorRamp = new Gradient { Offsets = [0, .15f, .65f, 1],
                Colors = [Colors.Transparent, Colors.White, new(1, 1, 1, .8f), Colors.Transparent] },
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add }
        };
        AddChild(_sparks);
        _sparks.Emitting = !_impact;
    }
}
