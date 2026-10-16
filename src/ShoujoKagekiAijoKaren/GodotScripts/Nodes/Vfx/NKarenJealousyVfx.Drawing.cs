using Godot;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

public partial class NKarenJealousyVfx
{
    private static readonly Color Hot = new("#f4fff9"), Mint = new("#b5f4d7");
    private static readonly Color[] Foil = [new("#ff93cf"), new("#ffe574"), new("#83efbd"), new("#c4a4ff"), new("#ffb88b")];

    private void Halo(Vector2 point, float radius, Color color, float alpha)
    {
        if (alpha > .001f) _light.DrawTextureRect(_soft!, new Rect2(point - Vector2.One * radius * _unit,
            Vector2.One * radius * 2 * _unit), false, new Color(color, alpha));
    }
    public override void _Draw() => DrawSquares(false);
    private void DrawLight()
    {
        DrawSquares(true);
        if (!_impact) return;
        float flash = Mathf.Exp(-_age * 40);
        Halo(_target, 65, Mint, flash * .48f);
        // A short local contact cut, never a beam across the battlefield.
        float fade = 1 - Mathf.SmoothStep(.08f, .21f, _age);
        if (fade < .005f) return;
        float end = Mathf.SmoothStep(0, .085f, _age);
        var previous = CutPoint(0);
        for (int i = 1; i <= 28; i++) {
            float t = end * i / 28f;
            var point = CutPoint(t);
            float taper = Mathf.Pow(Mathf.Sin(t * Mathf.Pi), .8f);
            if (previous.DistanceTo(point) > .1f * _unit && taper > .01f) {
                _light.DrawLine(previous, point, new Color(Mint, fade * .3f), (5 + 22 * taper) * _unit, true);
                _light.DrawLine(previous, point, new Color(Hot, fade * .92f), (1 + 10 * taper) * _unit, true);
            }
            previous = point;
        }
        _light.DrawLine(_target - Vector2.Up * 35 * _unit * flash, _target + Vector2.Up * 35 * _unit * flash,
            new Color(Hot, flash), 2 * _unit, true);
    }
    private Vector2 CutPoint(float t) => _target + new Vector2(_side * Mathf.Lerp(-145, 135, t),
        Mathf.Lerp(-85, 65, t) + Mathf.Sin(t * Mathf.Pi) * 48) * _unit;

    private void DrawSquares(bool glow)
    {
        int count = _impact ? ImpactSquares : SweepSquares;
        for (int i = 0; i < count; i++) {
            Vector2 point;
            float fade, age;
            if (_impact) {
                // Deterministic fan: flat square faces turn edge-on, slow down, and fall a little.
                age = _age - i % 4 * .012f;
                if (age <= 0) continue;
                fade = Mathf.SmoothStep(0, .035f, age) * (1 - Mathf.SmoothStep(.22f, .57f, age));
                float angle = i * 2.39996f;
                var direction = new Vector2(Mathf.Cos(angle) * _side, Mathf.Sin(angle) * .75f);
                float distance = (1 - Mathf.Exp(-age * 5)) * (95 + i % 7 * 28);
                point = _target + (direction * distance + new Vector2(_side * age * 38, age * age * 125)) * _unit;
            } else {
                float born = .018f + i * .011f;
                age = _age - born;
                if (age <= 0) continue;
                float progress = Mathf.Clamp(age / (HitTime - born), 0, 1);
                float spread = Mathf.Sin(progress * Mathf.Pi);
                point = FlightPoint(progress) + new Vector2(Mathf.Sin(i * 3.7f) * 13,
                    (i % 5 - 2) * 15) * spread * _unit;
                fade = Mathf.SmoothStep(0, .06f, age) * (1 - Mathf.SmoothStep(.9f, 1, progress));
                if (_confirmedAt >= 0) fade *= 1 - Mathf.SmoothStep(0, .08f, _age - _confirmedAt);
            }
            if (fade <= .01f) continue;
            float size = 6 + i % 5 * 2.4f;
            Square(point, size, i * 1.8f + age * (i % 2 == 0 ? 2.6f : -3.1f),
                age * (6 + i % 4) + i, Foil[i % Foil.Length], fade, glow);
        }
    }
    private void Square(Vector2 point, float size, float angle, float phase, Color color, float alpha, bool glow)
    {
        float face = .12f + .88f * Mathf.Abs(Mathf.Cos(phase));
        if (glow) {
            Halo(point, size * 2.8f, color, alpha * .14f * face);
            float glint = Mathf.Pow(Mathf.Max(0, Mathf.Cos(phase * 1.7f)), 14) * alpha;
            if (glint > .02f) {
                var corner = point + new Vector2(size * face, -size).Rotated(angle) * _unit;
                float radius = (size * .7f + 3) * _unit;
                _light.DrawLine(corner - Vector2.Right * radius, corner + Vector2.Right * radius,
                    new Color(Hot, glint), .9f * _unit, true);
                _light.DrawLine(corner - Vector2.Up * radius, corner + Vector2.Up * radius,
                    new Color(Hot, glint), .9f * _unit, true);
                Halo(corner, size * 1.7f, Hot, glint * .4f);
            }
            return;
        }
        // Pastel faces keep the square silhouette readable underneath the additive glints.
        DrawSetTransform(point, angle, new Vector2(face, 1) * _unit);
        Vector2[] quad = [new(-size,-size), new(size,-size), new(size,size), new(-size,size)];
        Color pale = color.Lerp(Colors.White, .38f);
        DrawPolygon(quad, [new(color, alpha * .85f), new(pale, alpha), new(Hot, alpha), new(pale, alpha)]);
        DrawPolyline([quad[0], quad[1], quad[2], quad[3], quad[0]], new Color(Hot, alpha * .75f), .65f, true);
        DrawSetTransform(Vector2.Zero);
    }
}
