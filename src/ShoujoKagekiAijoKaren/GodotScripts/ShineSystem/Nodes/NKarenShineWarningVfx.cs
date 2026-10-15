using Godot;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace ShoujoKagekiAijoKaren.src.Core.ShineSystem.Nodes;

/// <summary>Last-light embers follow the card from hand through the whole play.</summary>
public partial class NKarenShineWarningVfx(NCard card, CardModel model) : Node2D
{
    private float _age;
    private bool _warnedInHand;
    private bool _hasPose;
    private Transform2D _previousPose;
    private Vector2 _lightWorld, _lightVelocity;
    private float _spawnClock;
    private int _sparkIndex;
    private sealed class Spark(Vector2 position, Vector2 velocity, float life, float radius, float phase)
    {
        public Vector2 Position = position, Velocity = velocity;
        public float Age;
        public readonly float Life = life, Radius = radius, Phase = phase;
    }
    private readonly List<Spark> _sparks = new(40);
    private readonly Vector2[] _star = new Vector2[8];
    public bool BelongsTo(CardModel? candidate) => ReferenceEquals(candidate, model);
    public override void _Ready() => Visible = false;

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(card) || !BelongsTo(card.Model)) { Hide(); QueueFree(); return; }
        bool inHand = model.Pile?.Type == PileType.Hand;
        bool inPlay = model.Pile?.Type == PileType.Play;
        if (inHand) _warnedInHand = ShineVfx.WillDeplete(model);
        // Shine is decremented at the start of OnPlayWrapper. Keep the hand warning
        // during queue/Play transitions, without mistaking a 2→1 play for depletion.
        Visible = card.DisplayingPile is PileType.Hand or PileType.Play
            && ((inHand && _warnedInHand) || (inPlay && (_warnedInHand || ShineVfx.WillDeplete(model, shineAlreadyDecreased: true))));
        if (!Visible) { _sparks.Clear(); _hasPose = false; return; }
        float dt = Mathf.Min((float)delta, .05f);
        _age += dt;
        var pose = GetGlobalTransformWithCanvas();
        float unit = Mathf.Max(.1f, pose.Scale.Length() / Mathf.Sqrt(2));
        var target = pose * new Vector2(28 + Mathf.Sin(_age * 1.9f) * 12, -233 + Mathf.Sin(_age * 2.7f + .8f) * 7);
        if (!_hasPose) { _lightWorld = target; _lightVelocity = Vector2.Zero; _previousPose = pose; }
        _lightVelocity = (_lightVelocity + (target - _lightWorld) * (110 * dt)) * Mathf.Exp(-13 * dt);
        _lightWorld += _lightVelocity * dt;
        // A little inertia, bounded to this card even during the fast hand→Play tween.
        _lightWorld = target + (_lightWorld - target).LimitLength(38 * unit);
        var inherited = ((pose.Origin - _previousPose.Origin) / Mathf.Max(dt, .001f)).LimitLength(150 * unit) * .12f;
        _previousPose = pose; _hasPose = true;
        for (int i = _sparks.Count - 1; i >= 0; i--) {
            var spark = _sparks[i]; spark.Age += dt;
            spark.Velocity += new Vector2(Mathf.Sin(spark.Age * 6 + spark.Phase) * 12, -6) * (unit * dt);
            spark.Position += spark.Velocity * dt;
            if (spark.Age >= spark.Life) _sparks.RemoveAt(i);
        }
        _spawnClock += dt;
        while (_spawnClock >= .025f) {
            _spawnClock -= .025f;
            if (_sparks.Count >= 40) break;
            int i = _sparkIndex++; float side = i % 2 == 0 ? -1 : 1;
            var origin = i % 3 == 0 ? new Vector2(-90 + i * 43 % 205, -198)
                : new Vector2(side * 139, -180 + i * 71 % 320);
            var velocity = pose.BasisXform(new Vector2(side * (7 + i * 13 % 20), -32 - i * 19 % 42)) + inherited;
            _sparks.Add(new Spark(pose * origin, velocity, .48f + i % 7 * .045f, (1.5f + i % 3 * .65f) * unit, i * 2.39996f));
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        float pulse = .5f + .5f * Mathf.Sin(_age * 3.6f);
        var gold = new Color("#ffcb62");
        var hot = new Color("#fff4ce");
        // A broad, breathing last-light rim remains legible at actual hand scale.
        // Keep it outside the artwork and native cost/shine labels.
        for (int side = -1; side <= 1; side += 2)
        {
            var top = new Vector2(side * 137, -183);
            var bottom = new Vector2(side * 141, 178);
            DrawLine(top, bottom, new Color(gold, .08f + pulse * .10f), 20, true);
            DrawLine(top, bottom, new Color(gold, .25f + pulse * .25f), 7, true);
            for (int j = 0; j < 5; j++)
            {
                float t = Mathf.PosMod(_age * (.22f + j * .015f) + j * .203f + side * .07f, 1);
                float ember = Mathf.Sin(t * Mathf.Pi) * (.65f + .35f * Mathf.Sin(_age * 3.6f - j * .7f));
                var p = new Vector2(side * (139 + Mathf.Sin(_age * 2 + j) * 2), 180 - t * 360);
                DrawLine(p, p + new Vector2(side * 2, 18 + j % 3 * 6), new Color(hot, ember * .9f), 3.5f, true);
            }
        }
        var left = new Vector2(-100, -199);
        var right = new Vector2(120, -199);
        DrawLine(left, right, new Color(gold, .12f + pulse * .15f), 18, true);
        DrawLine(left, right, new Color(gold, .65f + pulse * .25f), 5, true);
        var lastLight = GetGlobalTransformWithCanvas().AffineInverse() * _lightWorld;
        float radius = 13 + pulse * 5;
        for (int layer = 8; layer > 0; layer--)
            DrawCircle(lastLight, radius * 2 * layer / 8f,
                new Color(gold, (.12f + pulse * .12f) * Mathf.Pow(1 - layer / 9f, 2)));
        for (int i = 0; i < _star.Length; i++)
        {
            float angle = i * Mathf.Pi / 4 - Mathf.Pi / 2 + Mathf.Sin(_age * 1.5f) * .12f;
            _star[i] = lastLight + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * (i % 2 == 0 ? 1 : .23f);
        }
        DrawColoredPolygon(_star, new Color(hot, .75f + pulse * .25f));
        // Emit in viewport space: old embers keep drifting when the card translates,
        // rotates or scales, instead of being rigidly carried by its next transform.
        DrawSetTransformMatrix(GetGlobalTransformWithCanvas().AffineInverse());
        foreach (var spark in _sparks) {
            float t = spark.Age / spark.Life, a = Mathf.Sin(Mathf.Min(1, t * 5) * Mathf.Pi / 2) * Mathf.Pow(1 - t, 1.2f);
            DrawCircle(spark.Position, spark.Radius * 2.4f, new Color(gold, a * .16f));
            DrawLine(spark.Position - spark.Velocity * .055f, spark.Position, new Color(gold, a * .7f), spark.Radius * .7f, true);
            DrawCircle(spark.Position, spark.Radius, new Color(hot, a));
        }
        DrawSetTransform(Vector2.Zero);
    }
}
