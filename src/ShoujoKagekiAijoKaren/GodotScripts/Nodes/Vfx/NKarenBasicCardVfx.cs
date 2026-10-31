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

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Basic attack silhouettes at the real native hit cue. Never moves the actor or schedules rules.</summary>
public partial class NKarenBasicCardVfx : Node2D
{
    public enum Shape { Strike, ShineStrike, PromiseDraw }
    private static CanvasItemMaterial? _blend;
    internal static void PrepareResources()
        => _blend ??= new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
    internal static Resource?[] PreparedResources => [_blend];
    private static readonly Color Rose = new("#ff607b"), Pearl = new("#fff1da"), Gold = new("#ffd285"), Blue = new("#accaff");
    private readonly Vector2[] _ribbon = new Vector2[98], _edge = new Vector2[49];
    private readonly Color[] _colors = new Color[98];
    private NCombatRoom _room = null!;
    private NCreature _view = null!;
    private Shape _shape;
    private Vector2 _center, _half;
    private float _side, _unit, _age, _speed;
    private static bool Present(Node? node) => GodotObject.IsInstanceValid(node) && node!.IsInsideTree() && !node.IsQueuedForDeletion();
    public static NKarenBasicCardVfx? Hit(Creature owner, Creature target, Shape shape)
    {
        var room = NCombatRoom.Instance;
        if (!KarenVfxQuality.Enabled || NonInteractiveMode.IsActive || !CombatManager.Instance.IsInProgress
            || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant || !Present(room)
            || room!.GetCreatureNode(owner) is not {} actor || room.GetCreatureNode(target) is not {} victim) return null;
        return new NKarenBasicCardVfx { Name = "KarenBasic" + shape, _room = room, _view = victim, _shape = shape,
            _side = victim.VfxSpawnPosition.X >= actor.VfxSpawnPosition.X ? 1 : -1 };
    }
    public override void _Ready()
    {
        PrepareResources(); Material = _blend;
        TopLevel = true; GlobalTransform = GetCanvasTransform().AffineInverse(); ZIndex = 24;
        _speed = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? 1 / .7f : 1;
        Sample();
        NDebugAudioManager.Instance?.Play("slash_attack.mp3", .60f);
        KarenVfxQuality.Redraw(this);
    }
    private void Sample()
    {
        GlobalTransform = GetCanvasTransform().AffineInverse();
        if (!Present(_view)) return;
        var bounds = _view.Visuals.GetNodeOrNull<Control>("%Bounds") ?? _view.Hitbox;
        var transform = bounds.GetGlobalTransformWithCanvas();
        var rect = new Rect2(transform * Vector2.Zero, Vector2.Zero);
        rect = rect.Expand(transform * new Vector2(bounds.Size.X, 0));
        rect = rect.Expand(transform * new Vector2(0, bounds.Size.Y)).Expand(transform * bounds.Size);
        _center = rect.GetCenter(); _half = rect.Size * .5f;
        _unit = Mathf.Clamp(rect.Size.Y / 240f, .45f, 1.5f);
    }
    public override void _Process(double delta)
    {
        _age += (float)delta * _speed;
        if (!Present(_room) || _room != NCombatRoom.Instance || _age >= .48f
            || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant) { Hide(); QueueFree(); return; }
        KarenVfxQuality.Redraw(this);
    }
    private static float P(float t, float a, float b) => Mathf.Clamp((t - a) / (b - a), 0, 1);
    public override void _Draw()
    {
        float fade = 1 - Mathf.SmoothStep(.12f, .48f, _age);
        float head = 1 - Mathf.Pow(1 - P(_age, 0, .105f), 3);
        float tail = P(_age, .08f, .38f);
        float extent = Mathf.Clamp(_half.Y * 1.05f, 88 * _unit, 155 * _unit);
        var tint = _shape == Shape.ShineStrike ? Gold : _shape == Shape.PromiseDraw ? Blue : Rose;
        for (int layer = 0; layer < 3; layer++)
        {
            float width = (layer == 0 ? 18 : layer == 1 ? 7 : 1.7f) * _unit;
            for (int i = 0; i < 49; i++)
            {
                float u = i / 48f, t = Mathf.Lerp(tail, head, u);
                Vector2 Point(float q) => _center + new Vector2((q * 2 - 1) * extent * _side,
                    (.65f - q * 1.3f - Mathf.Sin(q * Mathf.Pi) * (_shape == Shape.PromiseDraw ? .34f : .18f)) * extent);
                var at = Point(t); var normal = (Point(t + .001f) - Point(t - .001f)).Normalized().Orthogonal();
                float w = width * Mathf.Pow(Mathf.Max(.001f, Mathf.Sin(u * Mathf.Pi)), .7f);
                _ribbon[i] = at + normal * w; _ribbon[97 - i] = at - normal * w * .45f; _edge[i] = at;
                _colors[i] = _colors[97 - i] = new Color(layer == 2 ? Pearl : tint, fade * (layer == 0 ? .12f : layer == 1 ? .85f : .95f));
            }
            if (head - tail > .005f) DrawPolygon(_ribbon, _colors);
        }
        int count = KarenVfxQuality.ParticleCount(_shape == Shape.ShineStrike ? 12 : 7);
        for (int i = 0; i < count; i++)
        {
            float angle = i * 2.399963f;
            var axis = new Vector2(Mathf.Cos(angle) * _side, Mathf.Sin(angle) * .6f - .1f);
            float travel = P(_age, .025f, .40f);
            var at = _center + axis * (12 + travel * (60 + i % 3 * 16)) * _unit;
            DrawLine(at - axis * (6 + i % 3 * 3) * fade * _unit, at,
                new Color(i % 3 == 0 ? Pearl : tint, fade * .78f), (i % 3 == 0 ? 2 : 1.3f) * _unit, true);
        }
    }
}
