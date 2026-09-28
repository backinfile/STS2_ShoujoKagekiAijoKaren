using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

// Eight equal upright rings, matching the approved two-second SVG sequence.
public partial class NKarenPastAndFutureRingVfx : Node2D
{
    private static readonly (float Start, float Duration, float Y, float Alpha)[] Rings =
    [
        (0.12f, 1.65f, 0f, 0.92f), (0.32f, 1.04f, -8f, 0.72f),
        (0.53f, 1.83f, 5f, 0.65f), (0.69f, 0.91f, -3f, 0.8f),
        (0.88f, 1.36f, 2f, 1f), (1.09f, 0.83f, -6f, 0.85f),
        (1.24f, 1.22f, 7f, 0.65f), (1.46f, 1.24f, -2f, 0.88f)
    ];
    private readonly Vector2[] _arc = new Vector2[49];
    private NCreature? _creatureNode;
    private Node2D? _front;
    private Tween? _stopTween;
    private bool _stopping;
    private readonly List<float> _sequences = [];
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
        UpdatePlacement();
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
        // Each draw starts immediately while earlier rings finish independently.
        _sequences.Add(0f);
    }

    public void Stop()
    {
        if (_stopping) return;
        _stopping = true;
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
        for (int i = _sequences.Count - 1; i >= 0; i--)
        {
            _sequences[i] += (float)delta;
            if (_sequences[i] >= 2f) _sequences.RemoveAt(i);
        }
        UpdatePlacement();
        QueueRedraw();
        if (GodotObject.IsInstanceValid(_front))
        {
            _front!.Modulate = Modulate;
            _front.Visible = Visible;
            _front.QueueRedraw();
        }
    }

    public override void _Draw() => DrawRings(this, false);
    private void DrawFront() { if (GodotObject.IsInstanceValid(_front)) DrawRings(_front!, true); }

    private void DrawRings(Node2D canvas, bool front)
    {
        foreach (float elapsed in _sequences) DrawSequence(canvas, front, elapsed);
    }

    private void DrawSequence(Node2D canvas, bool front, float elapsed)
    {
        float clock = elapsed * 1.5f;
        float unit = _radius / 92f;
        foreach (var ring in Rings)
        {
            float progress = (clock - ring.Start) / ring.Duration;
            if (progress <= 0f || progress >= 1f) continue;
            float x = 210f - 420f * progress;
            float fade = Mathf.Min(1f, Mathf.Min(progress / 0.13f, (1f - progress) / 0.2f));
            float crossing = Mathf.Exp(-Mathf.Pow(x / 55f, 2f));
            float alpha = fade * ring.Alpha * (0.78f + 0.22f * crossing) * (front ? 1f : 0.75f);
            var center = new Vector2(x, ring.Y + 12f) * unit;
            float start = front ? Mathf.Pi / 2f : -Mathf.Pi / 2f;
            for (int i = 0; i < _arc.Length; i++)
            {
                float angle = start + Mathf.Pi * i / (_arc.Length - 1);
                _arc[i] = center + new Vector2(Mathf.Cos(angle) * 48f, Mathf.Sin(angle) * 92f) * (1.0125f * unit);
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
