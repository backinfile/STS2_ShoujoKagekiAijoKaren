using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

// Three quiet rear-layer traces idle beneath the existing bounded draw-ring stream.
public partial class NKarenPastAndFutureRingVfx : Node2D
{
    private static Shader? _traceShader;
    internal static void PrepareResources()
    {
        _traceShader ??= GD.Load<Shader>("res://materials/vfx/past_future_trace.gdshader");
    }
    internal static Godot.Resource?[] PreparedResources => [_traceShader];

    private static readonly (float Start, float Duration, float Y, float Alpha)[] Rings =
    [
        (0f, 1.15f, 0f, 0.92f), (0.16f, 0.88f, -8f, 0.72f),
        (0.53f, 1.83f, 5f, 0.65f), (0.69f, 0.91f, -3f, 0.8f),
        (0.88f, 1.36f, 2f, 1f), (1.09f, 0.83f, -6f, 0.85f),
        (1.24f, 1.22f, 7f, 0.65f), (1.46f, 1.24f, -2f, 0.88f)
    ];
    private static readonly (float Phase, float X, float Y, float Length, float Period, float Travel, float Tilt)[] AmbientTraces =
    [
        (.10f, 150f, -8f, 72f, 3f, 96f, -.035f),
        (.43f, -145f, 20f, 82f, 4.5f, 108f, .025f),
        (.69f, 160f, 48f, 76f, 3.6f, 100f, -.055f)
    ];
    // Common multiple of the three periods: wrapping the clock cannot jump a trace.
    private const float AmbientPeriod = 18f;
    private float _ambientTime;
    private float _ambientVisibility;
    private readonly ColorRect[] _ambientTraces = new ColorRect[AmbientTraces.Length];
    private readonly Vector2[] _arc = new Vector2[49];
    private NCreature? _creatureNode;
    private Node2D? _front;
    private Tween? _stopTween;
    private bool _stopping;
    private sealed class TravellingRing(float age, float duration, float y, float alpha)
    {
        public float Age = age;
        public readonly float Duration = duration;
        public readonly float Y = y;
        public readonly float Alpha = alpha;
    }
    private readonly List<TravellingRing> _travelling = [];
    private const float StreamInterval = .20f;
    private const float StreamHold = .55f;
    private const int MaxRings = 8;
    private float _streamRemaining;
    private float _nextRing;
    private int _streamIndex;
    private bool _continuing;
    private bool _accentPending;
    private float _radius = 150f;

    public bool BelongsTo(NCreature creature) => _creatureNode == creature;

    public void Init(NCreature creatureNode)
    {
        _creatureNode = creatureNode;
        ZAsRelative = true;
        ZIndex = 0;
        Material ??= new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        if (!GodotObject.IsInstanceValid(_front))
        {
            // A sibling after the actor is necessary: a child of the rear layer remains behind it.
            _front = new Node2D { Name = "PastAndFutureRingFront", Material = Material };
            _front.Draw += DrawFront;
            var parent = creatureNode.GetParent();
            parent.AddChildSafely(_front);
            parent.MoveChild(_front, creatureNode.GetIndex() + 1);
        }
        EnsureAmbientTraces();
        UpdatePlacement();
        UpdateAmbient();
    }

    public void Restart()
    {
        CancelFade();
        _stopping = false;
        Visible = true;
        Modulate = Colors.White;
        QueueRedraw();
    }

    public void Pulse()
    {
        if (_stopping) return;
        if (_travelling.Count == 0 && _streamRemaining <= 0)
        {
            // Preserve all timing, heights and opacity of the standalone effect.
            foreach (var ring in Rings)
                _travelling.Add(new TravellingRing(-ring.Start, ring.Duration, ring.Y, ring.Alpha));
            _continuing = false;
            return;
        }
        if (!_continuing)
        {
            _continuing = true;
            // Keep every visible ring on its current trajectory. Only replace rings
            // which have not entered yet, so a second draw never resets the picture.
            _travelling.RemoveAll(ring => ring.Age < 0);
            float sinceEntry = float.MaxValue;
            foreach (var ring in _travelling) sinceEntry = Mathf.Min(sinceEntry, ring.Age / 1.5f);
            _nextRing = Mathf.Max(0, StreamInterval - sinceEntry);
            _streamIndex = 0;
        }
        // Refresh a short emission window instead of accumulating one sequence per draw.
        // Rapid triggers share the next bright leading ring; there is no queued tail.
        _streamRemaining = StreamHold;
        _accentPending = true;
    }

    public void Stop()
    {
        if (_stopping) return;
        _stopping = true;
        _streamRemaining = 0;
        _stopTween = CreateTween();
        _stopTween.TweenProperty(this, "modulate", new Color(1f, 1f, 1f, 0f), 0.3f);
        _stopTween.TweenCallback(Callable.From(() =>
        {
            if (_stopping) GodotTreeExtensions.QueueFreeSafely(this);
        }));
    }

    private void CancelFade()
    {
        if (GodotObject.IsInstanceValid(_stopTween)) _stopTween!.Kill();
        _stopTween = null;
    }

    public override void _Ready() => Restart();

    public override void _ExitTree()
    {
        CancelFade();
        if (GodotObject.IsInstanceValid(_front))
        {
            _front!.Draw -= DrawFront;
            _front.Hide();
            GodotTreeExtensions.QueueFreeSafely(_front);
        }
        _front = null;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        for (int i = _travelling.Count - 1; i >= 0; i--)
        {
            var ring = _travelling[i];
            ring.Age += dt * 1.5f;
            if (ring.Age >= ring.Duration) _travelling.RemoveAt(i);
        }
        if (_streamRemaining > 0)
        {
            _streamRemaining = Mathf.Max(0, _streamRemaining - dt);
            _nextRing -= dt;
            if (_streamRemaining > 0 && _nextRing <= 0 && _travelling.Count < MaxRings)
            {
                var style = Rings[_streamIndex++ % Rings.Length];
                // Equal travel speed keeps repeated rings evenly separated. The original
                // slight height variation and brighter draw accents remain readable.
                _travelling.Add(new TravellingRing(0, 1.2f, style.Y, _accentPending ? 1f : .72f));
                _accentPending = false;
                _nextRing = StreamInterval;
            }
        }
        _ambientTime = Mathf.PosMod(_ambientTime + dt, AmbientPeriod);
        // Let the existing card-draw accent lead; recover gently when its last ring leaves.
        float ambientTarget = _travelling.Count > 0 || _streamRemaining > 0 ? .3f : 1f;
        _ambientVisibility = Mathf.MoveToward(_ambientVisibility, ambientTarget, dt * 2.5f);
        UpdatePlacement();
        UpdateAmbient();
        QueueRedraw();
        if (GodotObject.IsInstanceValid(_front))
        {
            _front!.Modulate = Modulate;
            _front.Visible = Visible;
            _front.QueueRedraw();
        }
    }

    public override void _Draw()
    {
        DrawRings(this, false);
    }

    private void EnsureAmbientTraces()
    {
        if (GodotObject.IsInstanceValid(_ambientTraces[0])) return;
        PrepareResources();
        // Parameters remain instance-local; only the immutable shader is shared and pinned.
        var material = new ShaderMaterial { Shader = _traceShader };
        for (int i = 0; i < _ambientTraces.Length; i++)
        {
            var trace = new ColorRect
            {
                Name = "PastAndFutureAmbientTrace" + i,
                Material = material,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Color = Colors.White
            };
            _ambientTraces[i] = trace;
            this.AddChildSafely(trace);
        }
    }

    private void UpdateAmbient()
    {
        float unit = _radius / 150f;
        float weight = .68f * _ambientVisibility * KarenVfxComposition.AmbientWeight(_creatureNode?.Entity);
        for (int i = 0; i < _ambientTraces.Length; i++)
        {
            var trace = _ambientTraces[i];
            if (!GodotObject.IsInstanceValid(trace)) continue;
            var style = AmbientTraces[i];
            float progress = Mathf.PosMod(_ambientTime / style.Period + style.Phase, 1f);
            float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp(progress / .1f, 0f, 1f));
            float fadeOut = Mathf.SmoothStep(0f, 1f, Mathf.Clamp((progress - .78f) / .22f, 0f, 1f));
            float wave = Mathf.Sin(Mathf.Tau * progress);
            // Different lifetimes and curved, short flank lanes keep motion readable without a beam.
            var head = new Vector2(style.X - style.Travel * progress, style.Y + 5f * wave) * unit;
            trace.Size = new Vector2(style.Length * (1f + .06f * wave), 18f) * unit;
            trace.Rotation = style.Tilt + .025f * wave;
            trace.Position = head - new Vector2(trace.Size.X * .2f, trace.Size.Y * .5f);
            trace.Modulate = new Color(1f, 1f, 1f, weight * fadeIn * (1f - fadeOut) * (.9f + .1f * wave));
        }
    }
    private void DrawFront() { if (GodotObject.IsInstanceValid(_front)) DrawRings(_front!, true); }

    private void DrawRings(Node2D canvas, bool front)
    {
        float unit = _radius / 92f;
        foreach (var ring in _travelling)
        {
            float progress = ring.Age / ring.Duration;
            if (progress <= 0f || progress >= 1f) continue;
            // Keep travel independent of ring size: larger hoops stay close to the actor.
            float x = 130f - 260f * progress;
            float fade = Mathf.Min(1f, Mathf.Min(progress / 0.07f, (1f - progress) / 0.2f));
            float crossing = Mathf.Exp(-Mathf.Pow(x / 55f, 2f));
            float alpha = fade * ring.Alpha * (0.78f + 0.22f * crossing) * (front ? 1f : 0.75f);
            alpha *= Mathf.Lerp(KarenVfxComposition.AmbientWeight(_creatureNode?.Entity), 1f, crossing);
            var center = new Vector2(x, ring.Y) * unit;
            float start = front ? Mathf.Pi / 2f : -Mathf.Pi / 2f;
            for (int i = 0; i < _arc.Length; i++)
            {
                float angle = start + Mathf.Pi * i / (_arc.Length - 1);
                _arc[i] = center + new Vector2(Mathf.Cos(angle) * 48f, Mathf.Sin(angle) * 92f) * (1.52f * unit);
            }
            Color color = front ? new Color("#def8ff") : new Color("#91d7ed");
            canvas.DrawPolyline(_arc, new Color(color, alpha * 0.04f), 11f * unit, true);
            canvas.DrawPolyline(_arc, new Color(color, alpha * 0.12f), 5f * unit, true);
            canvas.DrawPolyline(_arc, new Color(color, alpha), (front ? 2.3f : 1.6f) * unit, true);
        }
    }

    private void UpdatePlacement()
    {
        if (!GodotObject.IsInstanceValid(_creatureNode))
        {
            GodotTreeExtensions.QueueFreeSafely(this);
            return;
        }
        var visual = _creatureNode!.Visuals.GlobalTransform;
        var size = new Vector2(Mathf.Max(visual.X.Length(), 0.001f), Mathf.Max(visual.Y.Length(), 0.001f));
        GlobalTransform = new Transform2D(new Vector2(size.X, 0f), new Vector2(0f, size.Y),
            _creatureNode.VfxSpawnPosition);
        if (GodotObject.IsInstanceValid(_front)) _front!.GlobalTransform = GlobalTransform;
        var hitbox = _creatureNode.Hitbox;
        _radius = Mathf.Clamp(Mathf.Max(hitbox.Size.X, hitbox.Size.Y) * 0.52f, 115f, 175f);
    }
}
