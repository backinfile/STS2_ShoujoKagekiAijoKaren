using Godot;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

public partial class NKarenTwinStarsVfx
{
    public override void _Draw()
    {
        foreach (var pair in _pairs) DrawPair(pair);
        foreach (var shot in _shots)
        {
            if (shot.LaunchedAt < 0) continue;
            if (shot.ContactAt < 0) DrawFlight(shot);
            else DrawImpact(shot);
        }
    }

    private void DrawPair(StarPair pair)
    {
        float finishFade = _finishedAt < 0 ? 1 : 1 - P(_age - _finishedAt, 0, ImpactTime);
        float growth = Mathf.SmoothStep(0, 1, P(pair.Age, .12f, .40f));
        // Six motes leave this actual relic, split into two strands and become the twin stars.
        if (pair.RelicSource is { } source)
        {
            float transit = P(pair.Age, 0, .30f);
            for (int j = 0; j < 6; j++)
            {
                bool large = j % 2 == 0;
                var target = pair.Position(large, _unit);
                float progress = Mathf.Clamp(transit * 1.3f - j * .045f, 0, 1);
                var bend = new Vector2((j % 3 - 1) * 16 * _unit, -14 * _unit);
                Vector2 At(float t) => source.Lerp(target, t) + bend * (Mathf.Sin(t * Mathf.Pi));
                var at = At(progress);
                float alpha = (1 - P(progress, .82f, 1)) * finishFade;
                DrawLine(At(Mathf.Max(0, progress - .10f)), at,
                    Alpha(large ? Gold : Pink, alpha * .65f), 2 * _unit, true);
                Star(at, (8 + j % 3) * _unit, 6 * _unit, large ? Gold : Pink, alpha, -.3f);
            }
            if (pair.Age < .20f)
                Star(source, 20 * _unit, 18 * _unit, Gold, (1 - pair.Age / .20f) * finishFade, 0);
        }
        foreach (bool large in new[] { true, false })
        {
            if (large ? pair.GoldLaunched : pair.PinkLaunched) continue;
            var at = pair.Position(large, _unit);
            float size = (large ? 1 : .68f) * growth * _unit;
            Star(at, 30 * size, 28 * size, large ? Gold : Pink, growth * finishFade, -.18f);
        }
    }

    private void DrawFlight(StarShot shot)
    {
        var direction = (shot.Target - shot.Source).Normalized();
        var normal = direction.Orthogonal();
        float progress = P(shot.Age - shot.LaunchedAt, 0, FlightTime);
        var head = shot.Source.Lerp(shot.Target, progress);
        float size = shot.Large ? 1 : .66f;
        float length = Mathf.Min(head.DistanceTo(shot.Source), 165 * _unit * size);
        var tail = head - direction * length;
        Color color = shot.Large ? Gold : Pink;
        // A narrow tapered core, not a solid beam from character to target.
        DrawColoredPolygon([tail, head - normal * 6 * _unit * size,
            head + direction * 13 * _unit * size, head + normal * 6 * _unit * size], Alpha(color, .15f));
        DrawLine(tail, head, Alpha(color, .45f), 3 * _unit * size, true);
        DrawLine(tail.Lerp(head,.3f), head, Alpha(Ivory,.9f), 1.2f * _unit, true);
        for (int j = 0; j < 3; j++)
        {
            var point = head - direction * (35 + j * 35) * _unit * size;
            if (point.DistanceTo(shot.Source) > head.DistanceTo(shot.Source)) continue;
            Star(point + normal * (j % 2 == 0 ? -3 : 4) * _unit,
                (4 - j) * _unit, (2.3f - j * .4f) * _unit, color, .6f - j*.12f, direction.Angle());
        }
        Star(head, 66 * _unit * size, 31 * _unit * size, color, 1, direction.Angle());
    }

    private void DrawImpact(StarShot shot)
    {
        float age = shot.Age - shot.ContactAt;
        float fade = 1 - P(age, .025f, ImpactTime);
        float size = shot.Large ? 1 : .70f;
        var direction = (shot.Target - shot.Source).Normalized();
        var normal = direction.Orthogonal();
        var color = shot.Large ? Gold : Pink;
        float flare = 1 + .25f * Mathf.Sin(P(age, 0, .10f) * Mathf.Pi);
        Star(shot.Target, 44 * _unit * size * flare, 34 * _unit * size * flare,
            color, fade, direction.Angle() + .17f);
        for (int j = 0; j < 7; j++)
        {
            float angle = j * 2.39996f + shot.Index * .65f;
            var drift = Vector2.FromAngle(angle);
            float distance = (10 + (j % 3) * 7 + age * (100 + j * 12)) * _unit;
            var at = shot.Target + drift * distance + Vector2.Down * (age * age * 45 * _unit);
            float a = fade * (j < 3 ? .8f : .55f);
            if (j < 3)
                Star(at, (5-j*.6f) * _unit, (3+j*.5f) * _unit, color, a, angle);
            else
            {
                // Small blush petal/foil slivers echo the actual card portrait.
                DrawSetTransform(at, angle + age * 2);
                DrawColoredPolygon([new Vector2(-6,0)*_unit, new Vector2(0,-2)*_unit,
                    new Vector2(7,0)*_unit, new Vector2(0,2)*_unit], Alpha(j % 2 == 0 ? Pink : Gold, a));
                DrawSetTransform(Vector2.Zero);
            }
        }
        // Brief fading tail retained only near the contact point.
        float tailFade = 1 - P(age, 0, .08f);
        DrawLine(shot.Target - direction * 80 * _unit, shot.Target,
            Alpha(color, tailFade * .55f), 1.5f * _unit, true);
    }

    private void Star(Vector2 center, float width, float height, Color color, float alpha, float angle)
    {
        if (alpha <= .001f || width < .1f || height < .1f) return;
        DrawSetTransform(center, angle);
        Vector2[] Shape(float scale) => [new(-width*scale,0), new(-width*.14f*scale,-height*.14f*scale),
            new(0,-height*scale), new(width*.14f*scale,-height*.14f*scale), new(width*scale,0),
            new(width*.14f*scale,height*.14f*scale), new(0,height*scale), new(-width*.14f*scale,height*.14f*scale)];
        DrawColoredPolygon(Shape(1.35f), Alpha(color,alpha*.09f));
        DrawColoredPolygon(Shape(1), Alpha(color,alpha*.9f));
        DrawColoredPolygon(Shape(.48f), Alpha(Ivory,alpha));
        DrawColoredPolygon(Shape(.20f), Alpha(Colors.White,alpha));
        DrawSetTransform(Vector2.Zero);
    }
}
