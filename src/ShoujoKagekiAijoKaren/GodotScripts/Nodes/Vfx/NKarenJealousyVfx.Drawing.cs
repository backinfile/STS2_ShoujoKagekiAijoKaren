using Godot;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

public partial class NKarenJealousyVfx
{
    private static readonly Color Hot = new("#ecfff8"), Mint = new("#8ce6c5"), Teal = new("#237e72"), Gold = new("#e9be72");
    private const int Segments = 32;
    private void Halo(Vector2 point, float size, Color color, float alpha)
    {
        if (alpha > .001f) _light.DrawTextureRect(_soft!, new Rect2(point - Vector2.One * size * _unit,
            Vector2.One * size * 2 * _unit), false, new Color(color, alpha));
    }
    public override void _Draw()
    {
        if (_impact) return;
        float age = Mathf.Min(_age, HitTime);
        float fade = _confirmedAt < 0 ? Mathf.SmoothStep(0, .05f, _age) : 1 - Mathf.SmoothStep(0, .15f, _age - _confirmedAt);
        if (fade < .01f) return;
        float angle = SwingAngle(age);
        var hand = _source + Travel(age);
        var head = HeadPoint(age);
        var neck = head - Direction(angle) * 33 * _unit;
        // One rigid swinging staff and faceted silver head, with no token projectile.
        DrawLine(hand, neck, new Color("#382922", fade), 8 * _unit, true);
        DrawLine(hand, neck, new Color("#925341", fade), 4.5f * _unit, true);
        DrawLine(hand + Vector2.Up * _unit, neck + Vector2.Up * _unit, new Color("#d19970", fade), 1.3f * _unit, true);
        DrawLine(neck - Direction(angle) * 6 * _unit, neck + Direction(angle) * 3 * _unit, new Color(Gold, fade), 11 * _unit, true);
        DrawSetTransform(head, _side > 0 ? angle : Mathf.Pi - angle, Vector2.One * _unit);
        DrawColoredPolygon([new(-30,-9), new(-8,-24), new(24,-12), new(34,0), new(24,12), new(-8,24), new(-30,9)], new Color("#526373", fade));
        DrawColoredPolygon([new(-29,-8), new(-8,-22), new(32,0), new(-10,-4)], new Color("#dce9ed", fade));
        DrawColoredPolygon([new(-29,8), new(-8,22), new(32,0), new(-10,4)], new Color("#93adb8", fade));
        DrawColoredPolygon([new(-29,-3), new(-9,-7), new(32,0), new(-9,7), new(-29,3)], new Color(Hot, fade));
        DrawPolyline([new(-8,-23), new(31,0), new(-8,23)], new Color("#f4ffff", fade), 1.6f, true);
        DrawCircle(new(33,0), 4.5f, new Color("#478caa", fade));
        DrawCircle(new(33,-1), 2.3f, new Color("#b9f5ff", fade));
        DrawSetTransform(Vector2.Zero);
    }
    private void DrawLight()
    {
        if (_impact) { ImpactLight(); return; }
        float end = Mathf.Min(_age, HitTime);
        float fade = _confirmedAt < 0 ? 1 : 1 - Mathf.SmoothStep(0, .18f, _age - _confirmedAt);
        float start = Mathf.Max(0, end - (_age < WindupTime ? .1f : .17f));
        Ribbon(start, end, _age < WindupTime ? 10 : 72, Teal, .32f * fade);
        Ribbon(start, end, _age < WindupTime ? 4 : 40, Mint, .65f * fade);
        Ribbon(start, end, 4, Hot, .85f * fade);
        Halo(HeadPoint(end), 40, Mint, .22f * fade);
        if (_age > WindupTime && _confirmedAt < 0) {
            for (int i = 0; i < 5; i++) {
                float y = (42 + i * 13) * _unit;
                var point = _source + _actorTravel + new Vector2(-_side * (30 + i * 18) * _unit, y);
                _light.DrawLine(point - new Vector2(_side * 40 * _unit, 0), point,
                    new Color(Mint, .13f * fade), 1.4f * _unit, true);
            }
        }
    }
    private void Ribbon(float start, float end, float width, Color color, float alpha)
    {
        if (end - start < .012f || alpha <= .001f) return;
        // Follow the current grip: a local swinging crescent, not the entire dash trajectory.
        var grip = _source + Travel(end);
        Vector2 Point(float age) => grip + Direction(SwingAngle(age)) * ShaftLength * _unit;
        var previous = Point(start);
        for (int i = 1; i <= Segments; i++) {
            float u = i / (float)Segments;
            var point = Point(Mathf.Lerp(start, end, u));
            float taper = Mathf.Pow(Mathf.Sin((i - .5f) / Segments * Mathf.Pi), .8f);
            if (previous.DistanceTo(point) > .08f * _unit)
                _light.DrawLine(previous, point, new Color(color, alpha), Mathf.Max(.6f, width * _unit * taper), true);
            previous = point;
        }
    }
    private void ImpactLight()
    {
        float fade = 1 - Mathf.SmoothStep(.045f, .31f, _age);
        if (fade < .005f) return;
        float flash = Mathf.Exp(-_age * 35);
        Halo(_target, 82, Mint, flash * .65f);
        var along = new Vector2(_side, .13f).Normalized();
        var normal = along.Orthogonal();
        float length = (32 + 85 * flash) * _unit;
        _light.DrawColoredPolygon([_target - along * length, _target + normal * 7 * _unit * fade,
            _target + along * length * .7f, _target - normal * 7 * _unit * fade], new Color(Hot, fade));
        _light.DrawLine(_target - normal * 48 * flash * _unit, _target + normal * 48 * flash * _unit,
            new Color(Hot, flash), 2.5f * _unit, true);
        float radius = (20 + _age * 240) * _unit;
        if (_age > .015f && fade > .02f) {
            _light.DrawSetTransform(_target, -.13f * _side, new Vector2(.34f, 1));
            _light.DrawArc(Vector2.Zero, radius, -.6f, 3.7f, 32, new Color(Mint, fade * .55f), 2 * _unit, true);
            _light.DrawSetTransform(Vector2.Zero);
        }
        for (int i = 0; i < 12; i++) {
            float angle = -.95f + i / 11f * 1.8f;
            var direction = Direction(angle);
            var point = _target + direction * (12 + _age * (180 + i % 4 * 72)) * _unit;
            var side = direction.Orthogonal() * (2.3f + i % 3) * fade * _unit;
            var tip = direction * (9 + i % 4 * 5) * fade * _unit;
            if (fade > .03f) _light.DrawColoredPolygon([point + tip, point + side, point - tip * .55f, point - side],
                new Color(i % 5 == 0 ? Gold : Mint, fade * .9f));
        }
    }
}
