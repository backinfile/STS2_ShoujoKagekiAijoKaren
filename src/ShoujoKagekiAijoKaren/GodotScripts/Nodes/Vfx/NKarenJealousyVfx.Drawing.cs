using Godot;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

public partial class NKarenJealousyVfx
{
    private static readonly Color Gold = new("#e9b74d"), Hot = new("#fff6ca"), Mint = new("#9ef9d5"), Teal = new("#238a79");
    private const int Segments = 32;
    private readonly Vector2[] _ribbon = new Vector2[Segments * 2];
    private readonly Vector2[] _cord = new Vector2[41];
    private static readonly Vector2[] Star = MakeStar();
    private static Vector2[] MakeStar()
    {
        var points = new Vector2[10];
        for (int i = 0; i < 10; i++) points[i] = Vector2.FromAngle(-Mathf.Pi / 2 + i * Mathf.Pi / 5) * (i % 2 == 0 ? 1 : .46f);
        return points;
    }

    private float SweepProgress => Mathf.SmoothStep(0, 1, Mathf.Clamp((_age - .06f) / (HitTime - .06f), 0, 1));
    private void Halo(Vector2 point, float size, Color color, float alpha)
    {
        if (alpha <= .001f) return;
        _light.DrawTextureRect(_soft!, new Rect2(point - Vector2.One * size * _unit, Vector2.One * size * 2 * _unit), false, new Color(color, alpha));
    }

    public override void _Draw()
    {
        if (_reward) {
            float fade = Mathf.SmoothStep(0, .12f, _age) * (1 - Mathf.SmoothStep(.64f, 1.08f, _age));
            for (int i = 0; i < _rewardCount; i++) Clasp(RewardPoint(i), 20, fade, -.18f + _age * .3f + i * .25f);
        }
        else if (_impact) {
            float fade = 1 - Mathf.SmoothStep(.12f, .47f, _age);
            Clasp(_target + Vector2.Down * _age * 20 * _unit, 30, fade, _age * .7f * _side);
        }
        else if (_confirmedAt < 0) {
            float t = SweepProgress;
            Clasp(SweepPoint(t), 13 + 13 * t, Mathf.SmoothStep(0, .10f, _age), -.25f + t * .25f);
        }
    }

    // Opaque metal under an additive glint: the clasp remains readable at peak brightness.
    private void Clasp(Vector2 point, float radius, float alpha, float angle)
    {
        if (alpha <= .01f) return;
        DrawSetTransform(point, angle, Vector2.One * radius * _unit);
        DrawCircle(Vector2.Zero, 1.08f, new Color("#6b511e", alpha));
        DrawCircle(Vector2.Zero, 1, new Color(Gold, alpha));
        DrawCircle(new(-.07f, -.08f), .88f, new Color("#ffdf7d", alpha));
        DrawArc(Vector2.Zero, .96f, -2.8f, -.25f, 24, new Color(Hot, alpha), .075f, true);
        DrawColoredPolygon(Star, new Color("#c18a2c", alpha));
        // Five separate facets keep the raised five-point star visible.
        for (int i = 0; i < 5; i++) {
            DrawColoredPolygon([Vector2.Zero, Star[i * 2], Star[(i * 2 + 1) % 10]], new Color(i < 2 ? Hot : Gold, alpha));
        }
        DrawSetTransform(Vector2.Zero);
        for (int side = -1; side <= 1; side += 2) {
            for (int i = 0; i < _cord.Length; i++) {
                float t = i / 40f;
                _cord[i] = point + new Vector2(side * (radius * .85f + t * 40), Mathf.Sin(t * 4.7f + _age * 4 + side) * 15 * t + t * 5).Rotated(angle) * _unit;
            }
            Cord(alpha, .8f);
        }
    }

    private void Cord(float alpha, float thickness)
    {
        DrawPolyline(_cord, new Color("#71551f", alpha), 4 * thickness * _unit, true);
        DrawPolyline(_cord, new Color(Gold, alpha), 2.5f * thickness * _unit, true);
        for (int i = 1; i < _cord.Length - 1; i += 2) {
            var tangent = (_cord[i + 1] - _cord[i - 1]).Normalized();
            var slash = (tangent + tangent.Orthogonal()) * 1.6f * thickness * _unit;
            DrawLine(_cord[i] - slash, _cord[i] + slash, new Color(Hot, alpha), thickness * _unit, true);
        }
    }

    private void DrawLight()
    {
        if (_reward) { RewardLight(); return; }
        if (_impact) { ImpactLight(); return; }
        float t = SweepProgress;
        float fade = _confirmedAt < 0 ? 1 : 1 - Mathf.SmoothStep(0, .18f, _age - _confirmedAt);
        float start = Mathf.Max(0, t - .58f);
        Ribbon(false, start, t, 64, Teal, fade * .12f);
        Ribbon(false, start, t, 30, Mint, fade * .65f);
        Ribbon(false, start, t, 3, Hot, fade);
        Halo(SweepPoint(t), 48, Mint, .38f * fade);
        // A twisted, bounded gold cord follows the same live source/target curve.
        if (t > .03f) {
            for (int i = 0; i < _cord.Length; i++) {
                float u = Mathf.Lerp(start, t, i / 40f);
                _cord[i] = SweepPoint(u) + Vector2.Down * (Mathf.Sin(u * 13 + _age * 8) * 8 * _unit);
            }
            _light.DrawPolyline(_cord, new Color(Gold, .6f * fade), 1.8f * _unit, true);
        }
    }

    private void Ribbon(bool cut, float start, float end, float width, Color color, float alpha)
    {
        if (end - start < .015f || alpha <= .001f) return;
        Vector2 Point(float t) => cut ? CutPoint(t) : SweepPoint(t);
        _ribbon[0] = Point(start); _ribbon[Segments] = Point(end);
        for (int i = 1; i < Segments; i++) {
            float u = i / (float)Segments, t = Mathf.Lerp(start, end, u);
            var normal = (Point(t + .005f) - Point(t - .005f)).Normalized().Orthogonal();
            float taper = Mathf.Pow(Mathf.Sin(u * Mathf.Pi), .9f) * width * _unit * .5f;
            _ribbon[i] = Point(t) + normal * taper;
            _ribbon[Segments * 2 - i] = Point(t) - normal * taper;
        }
        _light.DrawColoredPolygon(_ribbon, new Color(color, alpha));
    }

    private void Glint(Vector2 point, float size, float alpha, Color color)
    {
        if (alpha <= .01f || size <= .02f) return;
        _light.DrawSetTransform(point, 0, Vector2.One * size * _unit);
        _light.DrawColoredPolygon([new(0, -1), new(.07f, -.09f), new(.52f, 0), new(.07f, .09f),
            new(0, 1), new(-.07f, .09f), new(-.52f, 0), new(-.07f, -.09f)], new Color(color, alpha));
        _light.DrawSetTransform(Vector2.Zero);
    }

    private void ImpactLight()
    {
        float fade = 1 - Mathf.SmoothStep(.08f, .48f, _age);
        float flash = Mathf.Exp(-_age * 22);
        Ribbon(true, 0, 1, 92, Mint, fade * .12f);
        Ribbon(true, 0, 1, 38, Mint, fade * .8f);
        Ribbon(true, 0, 1, 4, Hot, fade);
        Halo(_target, 80, Gold, flash * .7f);
        Glint(_target, 68 * (.4f + flash), flash, Hot);
        for (int i = 0; i < 18; i++) {
            float angle = i * 2.399963f;
            var direction = Vector2.FromAngle(angle);
            var point = _target + direction * (18 + _age * (95 + i % 6 * 32)) * _unit;
            Glint(point, (5 + i % 4 * 2) * fade, fade * .85f, i % 3 == 0 ? Gold : Mint);
        }
    }

    private void RewardLight()
    {
        float fade = Mathf.SmoothStep(0, .12f, _age) * (1 - Mathf.SmoothStep(.7f, 1.18f, _age));
        for (int i = 0; i < _rewardCount; i++) {
            var point = RewardPoint(i);
            Halo(point, 64, Gold, fade * .3f);
            Glint(point + new Vector2(-16, -14) * _unit, 20, fade, Hot);
        }
        float progress = Mathf.SmoothStep(0, 1, _age / .65f);
        for (int turn = 0; turn < 2; turn++) {
            for (int i = 0; i < _cord.Length; i++) {
                float t = i / 40f * progress, angle = t * Mathf.Tau + turn * 1.2f;
                float spread = 24 + turn * 15 + t * 25;
                _cord[i] = _target + new Vector2(Mathf.Cos(angle) * spread, Mathf.Sin(angle) * spread * .25f + 22 - t * 26) * _unit;
            }
            if (progress > .04f) _light.DrawPolyline(_cord, new Color(Gold, fade * .6f), 1.6f * _unit, true);
        }
        for (int i = 0; i < 10; i++) {
            float angle = i * 2.399963f;
            var point = _target + new Vector2(Mathf.Cos(angle) * (25 + i * 4), Mathf.Sin(angle) * 25 - _age * 28) * _unit;
            Glint(point, (5 + i % 3 * 3) * fade, fade * .8f, Hot);
        }
    }
}
