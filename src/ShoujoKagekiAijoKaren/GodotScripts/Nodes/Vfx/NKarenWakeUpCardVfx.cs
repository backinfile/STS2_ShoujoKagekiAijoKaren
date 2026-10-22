using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using System.Collections.Generic;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Selection-result light that only observes the native card and hand layout.</summary>
public partial class NKarenWakeUpCardVfx(CardModel model, NCombatRoom room) : Node2D
{
    // Shared visual resources are prepared during the run loading screen as well as on demand.
    internal static void PrepareResources()
    {
        _fan ??= GD.Load<Texture2D>("res://images/vfx/karen_wake_up_ribbon.png");
    }
    internal static Godot.Resource?[] PreparedResources => [_fan];

    private readonly record struct TrailPoint(Vector2 Position, float Time);
    private sealed class Spark(Vector2 position, Vector2 velocity, float radius, float life)
    {
        public Vector2 Position = position;
        public readonly Vector2 Velocity = velocity;
        public readonly float Radius = radius, Life = life;
        public float Age;
    }

    private static readonly Color Coral = new("#ef6746"), Gold = new("#ffb53e"), Hot = new("#ffe7a4");
    private static Texture2D? _fan;
    private readonly ArrayMesh _silkMesh = new();
    private readonly List<TrailPoint> _trail = new(40);
    private readonly List<Spark> _sparks = new(5);
    private readonly Vector2[] _star = new Vector2[8];
    private NCard? _card;
    private TextureRect? _frame;
    private float _waiting, _age, _landedAt = -1, _stableFor, _spawnClock;
    private int _emitted;
    private Vector2 _previousPoint, _velocity;
    private float _unit = 1, _screenUnit = 1;

    // Called only after native selection returns, immediately before native pile movement.
    // The room owns the decoration; no pooled NCard is changed or receives a child.
    public static void Observe(IEnumerable<CardModel> selected)
    {
        if (NCombatRoom.Instance is not { } room || CombatManager.Instance.IsOverOrEnding) return;
        selected = new List<CardModel>(selected);
        NKarenWakeUpSelectionVfx.CompleteCurrent(room, selected);
        foreach (var card in selected)
        {
            if (LocalContext.IsMe(card.Owner) && !card.HasBeenRemovedFromState)
                room.Ui.AddChild(new NKarenWakeUpCardVfx(card, room) { Name = "KarenWakeUpCardVfx" });
        }
    }

    public override void _Ready()
    {
        ZIndex = 40;
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
    }

    public override void _ExitTree() => _silkMesh.Dispose();

    public override void _Process(double delta)
    {
        if (NCombatRoom.Instance != room || CombatManager.Instance.IsOverOrEnding || model.HasBeenRemovedFromState)
        {
            QueueFree();
            return;
        }
        float dt = Mathf.Min((float)delta, .05f);
        if (_card == null)
        {
            _waiting += (float)delta;
            // Full hands can redirect the card into discard. Never light a card that did not arrive.
            if (model.Pile?.Type == PileType.Hand) _card = NCard.FindOnTable(model);
            if (_card == null)
            {
                if (_waiting > 2) QueueFree();
                return;
            }
            _frame = _card.GetNode<TextureRect>("%Frame");
            _previousPoint = CardCenter();
        }
        if (!GodotObject.IsInstanceValid(_card) || !_card.IsInsideTree() || _card.IsQueuedForDeletion()
            || _card.Model != model || model.Pile?.Type != PileType.Hand || !_card.IsVisibleInTree())
        {
            QueueFree();
            return;
        }

        // Draw in viewport coordinates so old trail/particle positions do not move with the card.
        GlobalTransform = GetCanvasTransform().AffineInverse();
        _age += (float)delta;
        var point = CardCenter();
        var pose = _card.Body.GetGlobalTransformWithCanvas();
        _unit = Mathf.Max(.1f, pose.Scale.Length() / Mathf.Sqrt(2));
        // Small cards still need a readable light at normal viewport size.
        _screenUnit = GetViewportRect().Size.Y / 1080f;
        _velocity = (point - _previousPoint) / Mathf.Max(.001f, dt);
        if (point.DistanceTo(_previousPoint) > 240 * _unit) _trail.Clear();
        _previousPoint = point;
        var trailPoint = ArcPoint(.12f);
        _trail.Add(new TrailPoint(trailPoint, _age));
        while (_trail.Count > 40 || (_trail.Count > 0 && _age - _trail[0].Time > .30f)) _trail.RemoveAt(0);

        if (_landedAt < 0 && _card.GetParent() is NHandCardHolder holder
            && holder.Position.DistanceTo(holder.TargetPosition) < 3 && _age >= .08f)
            _stableFor += dt;
        else _stableFor = 0;
        if (_landedAt < 0 && _stableFor >= .035f)
        {
            _landedAt = _age;
            for (int i = 2; i < 5; i++) EmitSpark(i);
        }

        for (int i = _sparks.Count - 1; i >= 0; i--)
        {
            var spark = _sparks[i];
            spark.Age += dt;
            spark.Position += spark.Velocity * dt;
            if (spark.Age >= spark.Life) _sparks.RemoveAt(i);
        }
        _spawnClock += dt;
        if (_emitted < 2 && _spawnClock >= .055f && _age < .22f)
        {
            _spawnClock = 0;
            EmitSpark(_emitted++);
        }
        if ((_landedAt >= 0 && _age - _landedAt > 1.0f) || _age > 2.5f)
        {
            QueueFree();
            return;
        }
        QueueRedraw();
    }

    private Vector2 CardCenter() => _card!.Body.GetGlobalTransformWithCanvas() * (_card.Body.Size * .5f);

    private void EmitSpark(int i)
    {
        var drift = new Vector2((i % 2 == 0 ? -1 : 1) * (12 + i * 3), -27 - i * 5) * _screenUnit;
        _sparks.Add(new Spark(ArcPoint(.12f + i * .17f) + Vector2.Up * 5 * _screenUnit,
            drift + _velocity.LimitLength(80 * _unit) * .04f, (1.5f + i % 3 * .35f) * _screenUnit, .42f + i * .035f));
    }

    // Frame-relative anchors follow the native card without changing its pose.
    private Vector2 ArcPoint(float t)
    {
        if (_frame?.Texture is not { } texture) return CardCenter();
        var size = texture.GetSize();
        size *= Mathf.Min(_frame.Size.X / size.X, _frame.Size.Y / size.Y);
        var offset = (_frame.Size - size) * .5f;
        return _frame.GetGlobalTransformWithCanvas() * (offset + new Vector2(
            size.X * Mathf.Lerp(.03f, .98f, t), size.Y * (.065f - .09f * Mathf.Sin(t * Mathf.Pi))));
    }

    public override void _Draw()
    {
        if (!GodotObject.IsInstanceValid(_card) || _card!.Model != model
            || model.Pile?.Type != PileType.Hand || !GodotObject.IsInstanceValid(_frame)) return;
        float after = _landedAt < 0 ? 0 : _age - _landedAt;
        float fade = _landedAt < 0 ? 1 : 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp((after - .30f) / .70f, 0, 1));
        float arcFade = _landedAt < 0 ? 1 : 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp((after - .20f) / .50f, 0, 1));
        DrawTrail(arcFade);
        DrawFans(arcFade);
        foreach (var spark in _sparks)
        {
            float alpha = Mathf.Pow(1 - spark.Age / spark.Life, 1.5f) * fade;
            DrawCircle(spark.Position, spark.Radius * 3.5f, new Color(Gold, alpha * .10f));
            DrawCircle(spark.Position, spark.Radius, new Color(Gold, alpha * .85f));
            DrawCircle(spark.Position, spark.Radius * .42f, new Color(Hot, alpha));
        }
        float pulse = _landedAt < 0 ? .6f : Mathf.Sin(Mathf.Clamp(after / .30f, 0, 1) * Mathf.Pi);
        var star = ArcPoint(.80f) + Vector2.Up * 3 * _screenUnit;
        DrawGlint(star, (10 + pulse * 6) * _screenUnit, fade * (.6f + pulse * .4f));
    }

    private void DrawTrail(float fade)
    {
        float limit = 220 * _screenUnit, travelled = 0;
        for (int i = _trail.Count - 1; i > 0 && travelled < limit; i--)
        {
            var a = _trail[i - 1].Position;
            var b = _trail[i].Position;
            float length = a.DistanceTo(b);
            if (length < .1f) continue;
            float visible = Mathf.Min(length, limit - travelled);
            a = b + (a - b) * (visible / length);
            float ageFade = Mathf.Clamp(1 - (_age - _trail[i - 1].Time) / .30f, 0, 1);
            float taper = Mathf.Clamp(1 - (travelled + visible * .5f) / limit, 0, 1);
            float alpha = ageFade * fade * Mathf.Pow(taper, .8f);
            // Smooth only the observed native path; do not invent a beam from a pile icon.
            var previous = i > 1 ? _trail[i - 2].Position : a;
            var next = i + 1 < _trail.Count ? _trail[i + 1].Position : b;
            var last = a;
            for (int j = 1; j <= 4; j++)
            {
                var p = a.CubicInterpolate(b, previous, next, j / 4f);
                DrawLine(last, p, new Color(Coral, alpha * .30f), (2 + 16 * taper) * _screenUnit, true);
                DrawLine(last, p, new Color(Gold, alpha * .65f), (.5f + 2.2f * taper) * _screenUnit, true);
                DrawLine(last, p, new Color(Hot, alpha * .65f), (.3f + .7f * taper) * _screenUnit, true);
                last = p;
            }
            travelled += visible;
        }
    }

    private void DrawFans(float fade)
    {
        float spread = Mathf.SmoothStep(0, 1, Mathf.Clamp(_age / .28f, 0, 1));
        var root = ArcPoint(.80f);
        var axis = (ArcPoint(1) - ArcPoint(0)).Normalized();
        var up = new Vector2(axis.Y, -axis.X);
        float width = ArcPoint(0).DistanceTo(ArcPoint(1));
        PrepareResources();
        // Independent silk strips continuously curl and twist around a pinned root.
        // Only this room-owned mesh changes; the native card keeps its own transform.
        const int segments = 28, strands = 3;
        var vertices = new Vector3[(segments + 1) * 2 * strands];
        var uvs = new Vector2[vertices.Length];
        var colors = new Color[vertices.Length];
        var indices = new int[segments * 6 * strands];
        for (int strand = 0; strand < strands; strand++)
        {
            int offset = strand * (segments + 1) * 2;
            float phase = _age * 6.5f + strand * 1.8f;
            for (int j = 0; j <= segments; j++)
            {
                float t = j / (float)segments;
                float x = -width * (1.04f + strand * .16f) * t * spread;
                float rise = width * spread * (
                    (.19f + strand * .10f) * Mathf.Sin(t * Mathf.Pi)
                    + .10f * t * Mathf.Sin(t * 7 - phase)
                    + .045f * t * Mathf.Sin(phase));
                // Twist travels from origin to tip; the silhouette keeps moving after opening.
                float half = width * (.14f + strand * .025f) * spread
                    * Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * Mathf.Pi)), .55f)
                    * (.65f + .35f * Mathf.Sin(t * 8 - phase));
                for (int edge = 0; edge < 2; edge++)
                {
                    int v = offset + j * 2 + edge;
                    vertices[v] = new Vector3(x, -rise + (edge == 0 ? -half : half), 0);
                    uvs[v] = new Vector2(1 - t, edge == 0 ? .15f : .75f);
                    colors[v] = new Color(Colors.White, fade * (1 - strand * .12f));
                }
                if (j == segments) continue;
                int a = offset + j * 2, k = (strand * segments + j) * 6;
                indices[k] = a; indices[k + 1] = a + 1; indices[k + 2] = a + 2;
                indices[k + 3] = a + 1; indices[k + 4] = a + 3; indices[k + 5] = a + 2;
            }
        }
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.TexUV] = uvs;
        arrays[(int)Mesh.ArrayType.Color] = colors;
        arrays[(int)Mesh.ArrayType.Index] = indices;
        _silkMesh.ClearSurfaces();
        _silkMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        DrawMesh(_silkMesh, _fan, new Transform2D(axis, -up, root));
    }

    private void DrawGlint(Vector2 center, float radius, float alpha)
    {
        if (alpha <= .001f) return;
        for (int layer = 3; layer > 0; layer--)
            DrawCircle(center, radius * layer * .75f, new Color(Gold, alpha * .20f / layer));
        DrawCircle(center, radius * .45f, new Color(Gold, alpha * .5f));
        for (int i = 0; i < _star.Length; i++)
        {
            float angle = i * Mathf.Pi / 4 - Mathf.Pi / 2;
            _star[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * (i % 2 == 0 ? 1 : .18f);
        }
        DrawColoredPolygon(_star, new Color(Hot, alpha));
    }
}
