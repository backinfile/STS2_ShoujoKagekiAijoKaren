using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>
/// NONNON's selected E design: rising silver edge, a red hilt glint, then one sweeping gust.
/// Only owns drawing. Damage, strength loss, and Shine remain in the native card flow.
/// </summary>
public partial class NKarenNononVfx : Node2D
{
    public const float HitTime = .66f;
    public const float FastHitTime = HitTime * FastDurationScale;
    private const float FastDurationScale = .7f;
    private const float Lifetime = 2.35f;
    private const float LiftStart = .16f;
    private const float LiftDrawTime = .23f;
    private const float SweepStart = .53f;
    private const float SweepDrawTime = .20f;
    private const float DebrisStart = SweepStart + SweepDrawTime + .06f;
    private const int Segments = 48;
    private static readonly Color Ice = new("#87b6d7");
    private static readonly Color Silver = new("#d9ebf5");
    private static readonly Color White = new("#f6fcff");
    private static readonly Color Red = new("#ff385f");
    private static readonly Color Warm = new("#fff1da");

    private NCombatRoom _room = null!;
    private NCreature _ownerNode = null!;
    private NCreature[] _targets = [];
    private readonly List<Vector2> _hitPoints = [];
    private readonly Vector2[] _ribbon = new Vector2[Segments * 2];
    private Transform2D _portraitToEffect;
    private Rect2 _portraitRect;
    private Vector2 _sourceFloor;
    private Vector2 _sweepEnd;
    private float _unit;
    private float _timeScale = 1f;
    private float _age;
    private bool _usePortrait;

    /// <summary>AttackCommand adds this node to the attacker's own combat VFX container.</summary>
    public static NKarenNononVfx? Create(Player owner)
    {
        if (NonInteractiveMode.IsActive || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant)
            return null;

        var room = NCombatRoom.Instance;
        var creature = room?.GetCreatureNode(owner.Creature);
        if (room == null || creature == null || owner.Creature.CombatState == null)
            return null;

        return new NKarenNononVfx
        {
            _room = room,
            _ownerNode = creature,
            _targets = owner.Creature.CombatState.GetOpponentsOf(owner.Creature)
                .Where(target => target.IsAlive)
                .Select(target => room.GetCreatureNode(target))
                .OfType<NCreature>().ToArray(),
            _timeScale = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? FastDurationScale : 1f
        };
    }

    public override void _Ready()
    {
        ZIndex = 10;
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        CaptureGeometry();
        CreateParticles();
        QueueRedraw();
    }

    private void CaptureGeometry()
    {
        var inverseCanvas = GetGlobalTransformWithCanvas().AffineInverse();
        var body = _ownerNode.Visuals.GetNodeOrNull<Sprite2D>("Visuals");
        if (body?.Texture != null)
        {
            // This is the same Karen portrait used by the HTML study. Convert its actual
            // full transform, including parent scaling, rather than adding global offsets.
            _portraitRect = body.GetRect();
            _portraitToEffect = inverseCanvas * body.GetGlobalTransformWithCanvas();
            _unit = (_portraitToEffect.Y * _portraitRect.Size.Y).Length() / 230f;
            _usePortrait = true;
            _sourceFloor = Source(221, 391);
        }
        else
        {
            // A different character can obtain this card. Anchor it to that owner too.
            var bounds = BoundsInEffect(_ownerNode.Hitbox);
            _sourceFloor = new Vector2(bounds.GetCenter().X, bounds.End.Y);
            _unit = Mathf.Max(.4f, bounds.Size.Y / 230f);
        }

        float farRight = _sourceFloor.X + 650f * _unit;
        float farLeft = _sourceFloor.X - 650f * _unit;
        if (_targets.Length > 0)
        {
            farRight = float.MinValue;
            farLeft = float.MaxValue;
            foreach (var target in _targets)
            {
                var bounds = BoundsInEffect(target.Hitbox);
                _hitPoints.Add(bounds.GetCenter());
                farRight = Mathf.Max(farRight, bounds.End.X);
                farLeft = Mathf.Min(farLeft, bounds.Position.X);
            }
        }

        float targetX = _hitPoints.Count > 0 ? _hitPoints.Average(point => point.X) : farRight;
        float targetY = _hitPoints.Count > 0 ? _hitPoints.Average(point => point.Y) : _sourceFloor.Y - 110 * _unit;
        bool right = targetX >= _sourceFloor.X;
        _sweepEnd = new Vector2(right ? farRight + 55 * _unit : farLeft - 55 * _unit, targetY - 24 * _unit);
    }

    private Rect2 BoundsInEffect(Control hitbox)
    {
        var transform = GetGlobalTransformWithCanvas().AffineInverse() * hitbox.GetGlobalTransformWithCanvas();
        var bounds = new Rect2(transform * Vector2.Zero, Vector2.Zero);
        bounds = bounds.Expand(transform * new Vector2(hitbox.Size.X, 0));
        bounds = bounds.Expand(transform * new Vector2(0, hitbox.Size.Y));
        return bounds.Expand(transform * hitbox.Size);
    }

    private Vector2 Source(float x, float y)
    {
        if (_usePortrait)
            return _portraitToEffect * (_portraitRect.Position
                + _portraitRect.Size * new Vector2((x - 38) / 330f, (y - 155) / 230f));
        return _sourceFloor + new Vector2(x - 221, y - 391) * _unit;
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_room) || !_room.IsInsideTree() || NCombatRoom.Instance != _room
            || !GodotObject.IsInstanceValid(_ownerNode) || !_ownerNode.IsInsideTree()
            || !CombatManager.Instance.IsInProgress)
        {
            Hide();
            QueueFree();
            return;
        }

        _age += (float)delta / _timeScale;
        if (_age >= Lifetime)
        {
            Hide();
            QueueFree();
            return;
        }
        QueueRedraw();
        _debrisLayer.QueueRedraw();
    }

    public override void _Draw()
    {
        SilverEdge(Source(205, 355), Source(218, 186), Source(439, 131), LiftStart, LiftDrawTime, 11, .51f);
        // The standing portrait's sword hand is left of her waist.
        HiltGlint(Source(169, 271));

        SilverEdge(Source(300, 222), SweepBend(), _sweepEnd, SweepStart, SweepDrawTime, 18, .66f);

        foreach (var point in _hitPoints)
            HitGlint(point);
        DrawParticles(this, luminous: true);
    }

    private Vector2 SweepBend()
    {
        var start = Source(300, 222);
        return new Vector2(Mathf.Lerp(start.X, _sweepEnd.X, .456f),
            Mathf.Max(start.Y, _sweepEnd.Y) + 85 * _unit);
    }

    private Vector2 SweepPoint(float u) => Bezier(Source(300, 222), SweepBend(), _sweepEnd, u);
    private Vector2 LiftPoint(float u) => Bezier(Source(205, 355), Source(218, 186), Source(439, 131), u);

    private void SilverEdge(Vector2 start, Vector2 bend, Vector2 end, float delay, float drawTime, float width, float life)
    {
        float alpha = Envelope(delay, delay + life, .23f);
        if (alpha <= 0) return;
        float progress = Ease((_age - delay) / drawTime);
        Ribbon(start, bend, end, progress, width * 4.1f, Silver, alpha * .035f);
        Ribbon(start, bend, end, progress, width * 2.25f, Ice, alpha * .13f);
        Ribbon(start, bend, end, progress, width, Silver, alpha * .54f);
        Ribbon(start, bend, end, progress, width * .28f, White, alpha);
    }

    private void Ribbon(Vector2 start, Vector2 bend, Vector2 end, float progress, float width, Color color, float alpha)
    {
        if (progress <= .001f || alpha <= .001f) return;
        width *= _unit * Mathf.Min(1f, progress * 6f);
        _ribbon[0] = start;
        _ribbon[Segments] = Bezier(start, bend, end, progress);
        for (int i = 1; i < Segments; i++)
        {
            float u = progress * i / Segments;
            var point = Bezier(start, bend, end, u);
            var tangent = (2 * (1 - u) * (bend - start) + 2 * u * (end - bend)).Normalized();
            var normal = new Vector2(-tangent.Y, tangent.X);
            float taper = Mathf.Pow(Mathf.Sin(Mathf.Pi * i / Segments), .64f);
            _ribbon[i] = point + normal * width * taper;
            _ribbon[Segments * 2 - i] = point - normal * width * taper * .46f;
        }
        DrawColoredPolygon(_ribbon, new Color(color, alpha));
    }

    private void HiltGlint(Vector2 center)
    {
        float alpha = Envelope(.19f, .83f, .32f);
        if (alpha <= 0) return;
        float pulse = 1 + .18f * Mathf.Sin((_age - .19f) * 19);
        Halo(center, 88 * _unit, Red, alpha * .85f);
        Halo(center, 38 * _unit, Red, alpha * 1.1f);
        Halo(center, 13 * _unit, Warm, alpha);
        for (int i = 0; i < 16; i++)
        {
            float near = i / 16f, far = (i + 1) / 16f;
            var offset = new Vector2(260 * _unit, -3 * _unit);
            var tint = new Color(Red.Lerp(Warm, Mathf.Pow(1 - near, 5)), alpha * Mathf.Pow(1 - near, 1.6f));
            DrawLine(center + offset * near, center + offset * far, new Color(Red, tint.A * .2f), 13 * _unit, true);
            DrawLine(center - offset * near, center - offset * far, new Color(Red, tint.A * .2f), 13 * _unit, true);
            DrawLine(center + offset * near, center + offset * far, tint, 3 * _unit, true);
            DrawLine(center - offset * near, center - offset * far, tint, 3 * _unit, true);
        }
        Star(center, 26 * _unit * pulse, Red, alpha * .9f);
        Star(center, 13 * _unit * pulse, White, alpha);
        DrawCircle(center, 4 * _unit, new Color(Warm, alpha));
    }

    private void HitGlint(Vector2 center)
    {
        float age = _age - HitTime;
        if (age < 0 || age > .5f) return;
        float alpha = 1 - Smooth(age / .5f);
        Halo(center, 72 * _unit, Silver, alpha * .4f);
        Star(center, (1 - Ease(age / .32f)) * 32 * _unit, White, alpha);
        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.Tau / 6 + .3f;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * .65f);
            var point = center + direction * age * 150 * _unit;
            DrawLine(point, point - direction * 8 * _unit, new Color(Silver, alpha * .7f), _unit, true);
        }
    }

    private void Halo(Vector2 center, float radius, Color color, float alpha)
    {
        for (int i = 7; i >= 1; i--)
        {
            float fraction = i / 7f;
            DrawCircle(center, radius * fraction, new Color(color, alpha * (1 - fraction * .8f) / 7f));
        }
    }

    private void Star(Vector2 center, float radius, Color color, float alpha)
    {
        // Subpixel stars collapse at world-space coordinates and cannot be triangulated.
        if (radius <= .75f * _unit) return;
        Vector2[] points = new Vector2[8];
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.Pi / 4;
            float length = i % 2 == 0 ? radius : radius * .18f;
            points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * length;
        }
        DrawColoredPolygon(points, new Color(color, alpha));
    }

    private float Envelope(float start, float end, float fade) =>
        Smooth((_age - start) / .07f) * (1 - Smooth((_age - end + fade) / fade));
    private static float Smooth(float value)
    {
        float u = Mathf.Clamp(value, 0, 1);
        return u * u * (3 - 2 * u);
    }
    private static float Ease(float value) => 1 - Mathf.Pow(1 - Mathf.Clamp(value, 0, 1), 3);
    private static Vector2 Bezier(Vector2 start, Vector2 bend, Vector2 end, float u) =>
        start * ((1 - u) * (1 - u)) + bend * (2 * (1 - u) * u) + end * (u * u);
}
