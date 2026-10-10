using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using System.Collections.Generic;

namespace ShoujoKagekiAijoKaren.src.Core.ShineSystem.Nodes;

/// <summary>Owns only the detached visual; model depletion remains in ShinePileManager.</summary>
public partial class NKarenShineDepleteVfx : Node2D
{
    private NCard _card = null!;
    private CardModel _model = null!;
    private Control _target = null!;
    private NCombatRoom _room = null!;
    private Transform2D _source;
    private readonly List<Vector2> _trail = new();
    private float _age;
    private float _duration;
    private float _gatherDuration;
    private bool _released;
    private bool _fuel;
    private bool _cardVisible;
    private Color _cardColor;
    private Control.MouseFilterEnum _mouseFilter;
    private sealed class Spark(Vector2 position, Vector2 velocity, float life, int variant)
    {
        public Vector2 Position = position;
        public Vector2 Velocity = velocity;
        public float Age;
        public readonly float Life = life;
        public readonly int Variant = variant;
    }
    private readonly List<Spark> _sparks = new();
    private readonly Vector2[] _star = new Vector2[8];
    private float _sparkClock;
    private int _sparkIndex;
    private static readonly Color Gold = new("#ffc96c");
    private static readonly Color Hot = new("#fff5dc");

    public static bool Play(CardModel model, NCard? card)
    {
        var room = NCombatRoom.Instance;
        var ui = NRun.Instance?.GlobalUi;
        var target = ui?.TopBar.GetNodeOrNull<Control>("RightAlignedStuff/ShinePile/Control/Icon");
        if (target == null || card == null || room == null || !LocalContext.IsMine(model)) return false;
        var source = card.GetGlobalTransformWithCanvas();
        var effect = new NKarenShineDepleteVfx
        {
            _card = card, _model = model, _target = target, _source = source, _room = room,
            _mouseFilter = card.MouseFilter,
            _cardVisible = card.Visible,
            _cardColor = card.Modulate,
            _fuel = KarenFuelCardVisuals.IsBurst(card),
            _duration = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant ? .25f : .85f
        };
        bool instant = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant;
        effect._gatherDuration = effect._fuel ? (instant ? .06f : .10f) : (instant ? .10f : .28f);
        effect._trail.Add(source.Origin);
        ui!.AddChild(effect);
        // Removing the visual from the play container prevents native RemoveFromCombat
        // from taking ownership a second time. This node returns it to the pool exactly once.
        card.PlayPileTween?.Kill();
        card.PlayPileTween = null;
        card.Reparent(effect);
        card.MouseFilter = Control.MouseFilterEnum.Ignore;
        // Keep a normal card at its original position during the crossfade. Fuel
        // has already burst, so its face must never reappear for this transition.
        card.Visible = effect._cardVisible && !effect._fuel;
        effect.PlaceCard(source);
        return true;
    }

    public override void _Ready()
    {
        ZIndex = 110;
        TopLevel = true;
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        GlobalTransform = GetCanvasTransform().AffineInverse();
    }

    private void PlaceCard(Transform2D canvasTransform)
    {
        var local = GetGlobalTransformWithCanvas().AffineInverse() * canvasTransform;
        _card.Position = local.Origin;
        _card.Rotation = local.Rotation;
        _card.Scale = local.Scale;
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_room) || NCombatRoom.Instance != _room
            || !GodotObject.IsInstanceValid(_target) || !_target.IsInsideTree() || !CombatManager.Instance.IsInProgress)
        { Release(); QueueFree(); return; }
        float dt = (float)delta;
        _age += dt;
        for (int i = _sparks.Count - 1; i >= 0; i--)
        {
            var spark = _sparks[i];
            spark.Age += dt;
            spark.Velocity += new Vector2(0, 110) * dt;
            spark.Position += spark.Velocity * dt;
            if (spark.Age >= spark.Life) _sparks.RemoveAt(i);
        }
        GlobalTransform = GetCanvasTransform().AffineInverse();
        float gather = Ease(_age / _gatherDuration);
        if (!_released && GodotObject.IsInstanceValid(_card) && ReferenceEquals(_card.Model, _model))
        {
            _card.Modulate = new Color(_cardColor, _cardColor.A * (1 - gather));
            _card.Visible = _cardVisible && !_fuel && _age < _gatherDuration;
        }
        float t = Mathf.Clamp((_age - _gatherDuration) / _duration, 0, 1);
        float u = t * t * (3 - 2 * t);
        var end = _target.GetGlobalTransformWithCanvas() * (_target.Size * .5f);
        var start = _source.Origin;
        var bend = new Vector2(start.X + (end.X - start.X) * .18f, Mathf.Max(75, start.Y - 210));
        var p = start * ((1-u)*(1-u)) + bend * (2*(1-u)*u) + end * (u*u);
        if (t < 1 || !_released)
        {
            var direction = _trail.Count > 0 ? (p - _trail[^1]).Normalized() : Vector2.Up;
            if (p.DistanceSquaredTo(_trail[^1]) > .04f || t >= 1) _trail.Add(p);
            if (_trail.Count > 24) _trail.RemoveAt(0);
            _sparkClock += dt;
            if (t > 0 && _sparkClock >= .014f && _sparks.Count < 90)
            {
                _sparkClock = 0;
                for (int j = 0; j < 2; j++)
                {
                    int index = _sparkIndex++;
                    float side = index % 2 == 0 ? -1 : 1;
                    var normal = new Vector2(-direction.Y, direction.X);
                    var velocity = normal * side * (38 + index * 37 % 92) - direction * (30 + index * 13 % 50);
                    _sparks.Add(new Spark(p, velocity, .35f + index % 5 * .065f, index));
                }
            }
        }
        if (t >= 1) Release();
        if (_age >= _gatherDuration + _duration + .65f) QueueFree();
        QueueRedraw();
    }

    public override void _Draw()
    {
        float gather = Ease(_age / _gatherDuration);
        float fade = 1 - Ease((_age - _gatherDuration - _duration) / .3f);
        if (!_fuel && _age < _gatherDuration)
        {
            // Warm fragments converge while the stationary card fades beneath them.
            // The same light becomes the travelling head once the face is gone.
            float light = Mathf.Sin(gather * Mathf.Pi) * .7f;
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.Tau / 16;
                var edge = _source * new Vector2(Mathf.Cos(angle) * 115, Mathf.Sin(angle) * 170);
                var point = edge.Lerp(_source.Origin, gather);
                var tail = edge.Lerp(_source.Origin, Mathf.Max(0, gather - .10f));
                DrawLine(tail, point, new Color(Gold, light), 2, true);
                DrawCircle(point, 2.3f, new Color(Hot, light));
            }
        }
        for (int i = 1; i < _trail.Count; i++)
        {
            float k = (float)i / _trail.Count;
            float light = k * k * fade;
            DrawLine(_trail[i-1], _trail[i], new Color(Gold, light * .10f), 8 + k * 18, true);
            DrawLine(_trail[i-1], _trail[i], new Color(Gold, light * .45f), 3 + k * 7, true);
            DrawLine(_trail[i-1], _trail[i], new Color(Hot, light), 1 + k * 2.5f, true);
            if (i % 3 == 0)
                DrawCircle(_trail[i] + new Vector2(Mathf.Sin(i*5) * 9, Mathf.Cos(i*3) * 9), 2.5f, new Color(Gold, light));
        }
        foreach (var spark in _sparks)
        {
            float light = Mathf.Pow(1 - spark.Age / spark.Life, 1.3f);
            Color color = spark.Variant % 3 == 0 ? new Color("#ff9455") : Gold;
            DrawCircle(spark.Position, 5, new Color(color, light * .14f));
            DrawLine(spark.Position - spark.Velocity * .028f, spark.Position, new Color(color, light), 1.7f, true);
            DrawCircle(spark.Position, spark.Variant % 4 == 0 ? 2.8f : 1.6f, new Color(Hot, light));
        }
        if (_trail.Count == 0) return;
        var p = _trail[^1];
        float headLight = gather * fade;
        float r = 17 * (.65f + .35f * gather) * fade;
        for (int layer = 8; layer > 0; layer--)
            DrawCircle(p, r * 2 * layer / 8f, new Color(Gold, headLight * .22f * Mathf.Pow(1 - layer / 9f, 2)));
        for (int i = 0; i < _star.Length; i++)
        {
            float angle = i * Mathf.Pi / 4 - Mathf.Pi / 2;
            _star[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (i % 2 == 0 ? 1 : .20f);
        }
        if (headLight > .001f && r > .2f)
        {
            // Triangulate at unit scale: shrinking absolute screen coordinates
            // eventually merges vertices at the last, almost-transparent frame.
            DrawSetTransform(p, 0, Vector2.One * r);
            DrawColoredPolygon(_star, new Color(Hot, headLight));
            DrawSetTransform(Vector2.Zero);
        }
        if (_released && fade > 0)
            DrawArc(p, 8 + (1 - fade) * 24, 0, Mathf.Tau, 36, new Color(Gold, fade * .65f), 2, true);
    }

    private static float Ease(float progress)
    {
        float t = Mathf.Clamp(progress, 0, 1);
        return t * t * (3 - 2 * t);
    }

    private void Release()
    {
        if (_released) return;
        _released = true;
        if (GodotObject.IsInstanceValid(_card) && ReferenceEquals(_card.Model, _model))
        {
            _card.MouseFilter = _mouseFilter;
            _card.Visible = _cardVisible;
            _card.Modulate = _cardColor;
            GodotTreeExtensions.QueueFreeSafely(_card);
        }
    }
    public override void _ExitTree() => Release();
}
