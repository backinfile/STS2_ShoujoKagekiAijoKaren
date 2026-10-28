using Godot;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

public partial class NKarenBaseballVfx
{
    private static readonly Color Gold = new(1, .70f, .30f), Warm = new(1, .94f, .76f), Red = new(1, .18f, .19f);
    private const int Segments = 32;
    private readonly Vector2[] _ribbon = new Vector2[Segments * 2];
    private readonly Vector2[] _seam = new Vector2[17];
    private readonly Vector2[] _shard = new Vector2[4];

    private void Halo(Vector2 center, float radius, Color color, float alpha)
    {
        if (radius <= 0 || alpha <= 0) return;
        _light.DrawTextureRect(_soft!, new Rect2(center - Vector2.One * radius, Vector2.One * radius * 2), false, new Color(color, alpha));
    }

    private void DrawLight()
    {
        if (_impact) { ImpactLight(); DrawImpactSeams(); return; }
        float swing = Mathf.Clamp(_age / SwingTime, 0, 1);
        float fade = 1 - Mathf.SmoothStep(SwingTime, SwingTime + .14f, _age);
        if (fade > .001f)
        {
            float end = Mathf.SmoothStep(0, 1, swing), start = Mathf.Max(0, end - .8f);
            Ribbon(true, start, end, 30, Gold, fade * .16f);
            Ribbon(true, start, end, 12, Gold, fade * .6f);
            Ribbon(true, start, end, 3.5f, Warm, fade);
            StitchedArc(_source, new Vector2(100 * _side, 86) * _unit, -2.25f + start * 2.4f, -2.25f + end * 2.4f, fade * .85f);
        }
        if (_age < SwingTime) return;
        float t = Mathf.Clamp((_age - SwingTime) / (HitTime - SwingTime), 0, 1);
        float tail = _confirmedAt < 0 ? 1 : 1 - Mathf.Clamp((_age - _confirmedAt) / .16f, 0, 1);
        float first = Mathf.Max(0, t - .72f);
        Ribbon(false, first, t, 32, Gold, .16f * tail);
        Ribbon(false, first, t, 12, Gold, .8f * tail);
        Ribbon(false, first, t, 3.2f, Warm, tail);
        for (int i = 0; i < 14; i += KarenVfxQuality.ParticleStride)
        {
            float u = i / 13f;
            if (u < first || u > t - .055f) continue;
            var tangent = (FlightPoint(Mathf.Min(1, u + .01f)) - FlightPoint(Mathf.Max(0, u - .01f))).Normalized();
            var normal = tangent.Orthogonal();
            var point = FlightPoint(u) + normal * (Mathf.Sin(u * 11) * 8 * _unit);
            float opacity = (u - first) / .72f * tail;
            _light.DrawLine(point - normal * 3 * _unit, point + normal * 3 * _unit + tangent * 2 * _unit,
                new Color(Red, opacity), 1.5f * _unit, true);
        }
        if (_confirmedAt < 0) Halo(FlightPoint(t), 44 * _unit, Warm, .7f);
    }

    private void Ribbon(bool swing, float start, float end, float width, Color color, float alpha)
    {
        if (end - start < .008f || alpha <= .001f) return;
        _ribbon[0] = swing ? SwingPoint(start) : FlightPoint(start);
        _ribbon[Segments] = swing ? SwingPoint(end) : FlightPoint(end);
        for (int i = 1; i < Segments; i++)
        {
            float u = i / (float)Segments, t = Mathf.Lerp(start, end, u);
            var point = swing ? SwingPoint(t) : FlightPoint(t);
            var tangent = swing ? SwingPoint(t + .005f) - SwingPoint(t - .005f) : FlightPoint(t + .005f) - FlightPoint(t - .005f);
            var normal = tangent.Normalized().Orthogonal();
            float taper = Mathf.Pow(Mathf.Sin(u * Mathf.Pi), .7f) * width * _unit;
            _ribbon[i] = point + normal * taper * .5f;
            _ribbon[Segments * 2 - i] = point - normal * taper * .5f;
        }
        _light.DrawColoredPolygon(_ribbon, new Color(color, alpha));
    }

    private void Ball(Vector2 center, float radius)
    {
        DrawCircle(center, radius, new Color(.97f, .84f, .65f));
        DrawCircle(center + new Vector2(-.12f, -.12f) * radius, radius * .91f, new Color(1, .98f, .88f));
        DrawCircle(center + new Vector2(-.22f, -.25f) * radius, radius * .48f, new Color(1, 1, .99f));
        float rotation = _age * 23 * _side;
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < _seam.Length; i++)
            {
                float y = -.91f + i / 16f * 1.82f;
                _seam[i] = center + new Vector2(side * (.30f + .40f * y * y), y).Rotated(rotation) * radius;
            }
            DrawPolyline(_seam, new Color(.91f, .13f, .13f), Mathf.Max(.9f, radius * .11f), true);
            for (int i = 2; i < 16; i += 2)
            {
                var tangent = (_seam[i + 1] - _seam[i - 1]).Normalized();
                var stitch = (tangent.Orthogonal() + tangent * .35f) * radius * .18f;
                DrawLine(_seam[i] - stitch, _seam[i] + stitch, new Color(.91f, .13f, .13f), Mathf.Max(.8f, radius * .09f), true);
            }
        }
    }

    private void StitchedArc(Vector2 center, Vector2 radius, float start, float end, float alpha)
    {
        if (end - start < .025f || alpha <= .001f) return;
        for (int i = 0; i < _seam.Length; i++)
        {
            float angle = Mathf.Lerp(start, end, i / 16f);
            _seam[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        _light.DrawPolyline(_seam, new Color(Red, alpha * .65f), 1.4f * _unit, true);
        for (int i = 1; i < 16; i++)
        {
            var tangent = (_seam[i + 1] - _seam[i - 1]).Normalized();
            var normal = tangent.Orthogonal();
            _light.DrawLine(_seam[i] - normal * 3 * _unit, _seam[i] + normal * 3 * _unit + tangent * 2.8f * _unit,
                new Color(Red, alpha), 1.7f * _unit, true);
        }
    }

    private void DrawImpactSeams()
    {
        float fade = 1 - Mathf.SmoothStep(.12f, .38f, _age);
        float spread = 26 + 80 * _age;
        StitchedArc(_target + new Vector2(13 * _side, -9) * _unit,
            new Vector2(1, .88f) * spread * _unit, -1.8f + _age * 3, .72f + _age * 3, fade);
        StitchedArc(_target + new Vector2(-13 * _side, 5) * _unit,
            new Vector2(1, .8f) * spread * _unit, 1.1f - _age * 2, 2.6f - _age * 2, fade * .65f);
    }

    private void ImpactLight()
    {
        float flash = Mathf.Exp(-_age * 27), fade = 1 - Mathf.SmoothStep(.09f, .4f, _age);
        Halo(_target, (62 + _age * 60) * _unit, Gold, flash * 1.4f);
        Halo(_target, 28 * _unit, Warm, flash * 1.5f);
        _light.DrawCircle(_target, Mathf.Max(.1f, 10 * _unit * flash), new Color(Warm, flash));
        for (int i = 0; i < 22; i += KarenVfxQuality.ParticleStride)
        {
            float angle = i * 2.399963f + .18f;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * .8f);
            float speed = 100 + i % 7 * 34;
            var point = _target + direction * (8 + _age * speed) * _unit;
            float length = (12 + i % 5 * 6) * fade * _unit;
            var width = direction.Orthogonal().Normalized() * (1.2f + i % 3 * .35f) * fade * _unit;
            _shard[0] = point + direction * length;
            _shard[1] = point + width;
            _shard[2] = point - direction * length * .5f;
            _shard[3] = point - width;
            if (fade > .035f)
                _light.DrawColoredPolygon(_shard, new Color(i % 5 == 0 ? Red : Warm, fade * .9f));
        }
    }
}
