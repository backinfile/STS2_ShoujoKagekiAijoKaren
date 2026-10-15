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
using System.Linq;

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
    private readonly List<float> _trailTimes = new();
    private readonly Vector2[] _flightPoints = new Vector2[33];
    private readonly float[] _flightLengths = new float[33];
    private Vector2 _flightEnd, _headPosition;
    private float _pathLength;
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

    public static bool IsAnimating(CardModel model) => NRun.Instance?.GlobalUi.GetChildren()
        .OfType<NKarenShineDepleteVfx>().Any(v => v._model == model && !v.IsQueuedForDeletion()) == true;

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
            _duration = SaveManager.Instance.PrefsSave.FastMode switch { FastModeType.Instant => .12f, FastModeType.Fast => .38f, _ => .54f }
        };
        var speed = SaveManager.Instance.PrefsSave.FastMode;
        effect._gatherDuration = effect._fuel
            ? (speed == FastModeType.Instant ? .025f : speed == FastModeType.Fast ? .07f : .10f)
            : (speed == FastModeType.Instant ? .07f : speed == FastModeType.Fast ? .24f : .34f);
        effect._headPosition = source.Origin;
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
            float shrink = Mathf.Lerp(1, .10f, gather);
            PlaceCard(new Transform2D(_source.X * shrink, _source.Y * shrink, _source.Origin));
        }
        float t = Mathf.Clamp((_age - _gatherDuration) / _duration, 0, 1);
        var end = _target.GetGlobalTransformWithCanvas() * (_target.Size * .5f);
        if (_pathLength <= 0 || end.DistanceSquaredTo(_flightEnd) > .25f) BuildFlight(end);
        var p = FlightPoint(TravelProgress(t));
        var direction = (p - _headPosition).Normalized();
        _headPosition = p;
        while (_trailTimes.Count > 0 && _age - _trailTimes[0] > .16f) { _trail.RemoveAt(0); _trailTimes.RemoveAt(0); }
        if (_age >= _gatherDuration && (t < 1 || !_released))
        {
            if (_trail.Count == 0 || p.DistanceSquaredTo(_trail[^1]) > .04f || t >= 1) { _trail.Add(p); _trailTimes.Add(_age); }
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
                    _sparks.Add(new Spark(p, velocity, .22f + index % 5 * .035f, index));
                }
            }
        }
        if (t >= 1) Release();
        if (_age >= _gatherDuration + _duration + .42f) QueueFree();
        QueueRedraw();
    }

    private void BuildFlight(Vector2 end)
    {
        _flightEnd = end;
        var start = _source.Origin; var delta = end - start;
        float unit = GetViewport().GetVisibleRect().Size.Y / 1080f;
        float lift = Mathf.Clamp(Mathf.Abs(delta.X) * .30f + Mathf.Abs(delta.Y) * .18f, 140 * unit, 300 * unit);
        var c1 = start + new Vector2(-delta.X * .12f, -lift);
        var c2 = end + new Vector2(-delta.X * .22f, Mathf.Clamp(Mathf.Abs(delta.Y) * .18f, 40 * unit, 110 * unit));
        _flightLengths[0] = 0; _flightPoints[0] = start;
        for (int i = 1; i < _flightPoints.Length; i++) {
            float t = i / 32f, v = 1 - t;
            _flightPoints[i] = start * (v*v*v) + c1 * (3*v*v*t) + c2 * (3*v*t*t) + end * (t*t*t);
            _flightLengths[i] = _flightLengths[i-1] + _flightPoints[i].DistanceTo(_flightPoints[i-1]);
        }
        _pathLength = _flightLengths[^1];
    }
    // A slow lift (first 30% of time, only 3% of distance), a distinct surge,
    // then braking into a short catch. Peak speed is 3.15 times the average.
    // Integrating smooth velocity ramps avoids jumps at all phase boundaries;
    // arc-length lookup keeps curvature from flattening the intended rhythm.
    internal static float TravelProgress(float t)
    {
        t = Mathf.Clamp(t, 0, 1);
        const float lift = .30f, surge = .32f, brake = .22f, catchTime = .16f;
        const float launchSpeed = .20f, peakSpeed = 3f, catchSpeed = .42f;
        const float liftDistance = lift * launchSpeed / 2;
        const float surgeDistance = surge * (launchSpeed + peakSpeed) / 2;
        const float brakeDistance = brake * (peakSpeed + catchSpeed) / 2;
        const float total = liftDistance + surgeDistance + brakeDistance + catchTime * catchSpeed / 2;
        if (t < lift) return lift * IntegratedRamp(t / lift, 0, launchSpeed) / total;
        if (t < lift + surge) return (liftDistance + surge * IntegratedRamp((t-lift) / surge, launchSpeed, peakSpeed)) / total;
        if (t < lift + surge + brake) return (liftDistance + surgeDistance + brake * IntegratedRamp((t-lift-surge) / brake, peakSpeed, catchSpeed)) / total;
        return (liftDistance + surgeDistance + brakeDistance + catchTime * IntegratedRamp((t-lift-surge-brake) / catchTime, catchSpeed, 0)) / total;
    }
    private static float IntegratedRamp(float t, float from, float to)
        => from * t + (to-from) * (t*t*t - .5f*t*t*t*t);
    private Vector2 FlightPoint(float progress)
    {
        float distance = progress * _pathLength;
        for (int i = 1; i < _flightPoints.Length; i++)
            if (distance <= _flightLengths[i]) return _flightPoints[i-1].Lerp(_flightPoints[i],
                Mathf.Clamp((distance - _flightLengths[i-1]) / Mathf.Max(.001f, _flightLengths[i] - _flightLengths[i-1]), 0, 1));
        return _flightEnd;
    }

    public override void _Draw()
    {
        float gather = Ease(_age / _gatherDuration);
        float fade = 1 - Ease((_age - _gatherDuration - _duration) / .3f);
        if (!_fuel && _age < _gatherDuration)
        {
            // A loose, card-sized outer cloud curls inward as the actual face shrinks.
            // The whole silhouette participates before it becomes a compact flight head.
            for (int i = 0; i < 40; i++)
            {
                float u = Mathf.Clamp((gather - i % 5 * .022f) / (1 - i % 5 * .022f), 0, 1);
                float angle = i * 2.399963f;
                var offset = new Vector2(Mathf.Cos(angle) * (180 + i % 4 * 18), Mathf.Sin(angle) * (245 + i % 5 * 16));
                var edge = _source * offset - _source.Origin;
                var tangent = new Vector2(-edge.Y, edge.X).Normalized() * (22 + i % 7 * 7);
                var point = _source.Origin + edge * (1-u) + tangent * Mathf.Sin(u * Mathf.Pi);
                float previous = Mathf.Max(0, u - .09f);
                var tail = _source.Origin + edge * (1-previous) + tangent * Mathf.Sin(previous * Mathf.Pi);
                float light = Mathf.Sin(u * Mathf.Pi) * (.55f + i % 3 * .12f);
                DrawLine(tail, point, new Color(Gold, light * .15f), 8, true);
                DrawLine(tail, point, new Color(Gold, light), 2.2f, true);
                DrawCircle(point, i % 4 == 0 ? 4 : 2.2f, new Color(Hot, light));
            }
        }
        for (int i = 1; i < _trail.Count; i++)
        {
            float k = (float)i / _trail.Count;
            float light = k * k * fade * Mathf.Clamp(1 - (_age - _trailTimes[i]) / .16f, 0, 1);
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
        var p = _headPosition;
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
