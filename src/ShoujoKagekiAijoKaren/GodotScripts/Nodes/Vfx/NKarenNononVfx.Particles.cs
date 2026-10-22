using Godot;
using System;
using System.Collections.Generic;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

public partial class NKarenNononVfx
{
    internal static void PrepareResources()
    {
        _atmosphere ??= new GradientTexture2D
        {
            Width = 256,
            Height = 128,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(.5f, .5f),
            FillTo = new Vector2(1, .5f),
            Gradient = new Gradient
            {
                Offsets = [0, .52f, 1],
                Colors = [new Color(.31f, .34f, .46f, .30f), new Color(.31f, .34f, .46f, .13f),
                    new Color(.31f, .34f, .46f, 0)]
            }
        };
    }
    internal static Godot.Resource?[] PreparedResources => [_atmosphere];

    private enum ParticleKind { Dust, Streak, Glint, Shard }
    private static readonly Color WindGray = new("#939bac");
    private static readonly Color DustGray = new("#6b758a");
    private static readonly Color DeepSlate = new("#2b2d3b");
    private static readonly Color PaleShard = new("#b6bac6");
    private static readonly Color ColdGlint = new("#c5cbe0");
    private static readonly Color HiltMote = new("#bc5869");

    private readonly record struct WindParticle(
        ParticleKind Kind, Vector2 Origin, Vector2 Velocity, Vector2 Acceleration,
        Color Tint, float Birth, float Life, float Size, float Phase, float Curl,
        float Drag, float Frequency, float Spin, float Aspect, float Opacity,
        float FadeIn, float FadeOut, Vector2[]? Silhouette, SwordGust LiftGust, SwordGust SweepGust);

    private readonly record struct BladeSample(Vector2 Position, Vector2 Tangent, float Arrival);
    private readonly record struct SwordGust(Vector2 Velocity, Vector2 CurlDirection,
        float Time, float Drag, float Curl, float Frequency, float Spin);
    private readonly record struct DustCluster(Vector2 Center, Vector2 Spread, float Rotation);
    private readonly record struct MistPatch(Rect2 Bounds, Vector2 Drift, float Birth, float Life, float Opacity);

    private readonly List<WindParticle> _particles = new(1600);
    private readonly List<MistPatch> _mistPatches = new(9);
    private Vector2[] _particleLines = [];
    private Color[] _particleColors = [];
    private readonly Vector2[] _shardPolygon = new Vector2[5];
    private Node2D _debrisLayer = null!;
    private static GradientTexture2D _atmosphere = null!;
    private Rect2 _fieldBounds;

    private void CreateParticles()
    {
        // A fresh visual-only seed makes every cast different without touching combat RNG.
        var random = new Random();
        float Rand(float min, float max) => min + (max - min) * random.NextSingle();
        float Gaussian() => Mathf.Sqrt(-2 * Mathf.Log(Mathf.Max(.0001f, random.NextSingle())))
            * Mathf.Cos(Rand(0, Mathf.Tau));
        var hilt = Source(169, 271);
        var view = GetViewportRect();
        var inverse = GetGlobalTransformWithCanvas().AffineInverse();
        var topLeft = view.Position + view.Size * new Vector2(.01f, .105f);
        var bottomRight = view.Position + view.Size * new Vector2(.99f, .715f);
        _fieldBounds = new Rect2(inverse * topLeft, Vector2.Zero)
            .Expand(inverse * new Vector2(bottomRight.X, topLeft.Y))
            .Expand(inverse * new Vector2(topLeft.X, bottomRight.Y))
            .Expand(inverse * bottomRight);
        Vector2 Field(float x, float y) => _fieldBounds.Position + _fieldBounds.Size * new Vector2(x, y);

        BladeSample[] SampleBlade(Func<float, Vector2> point, float start, float duration)
        {
            var samples = new BladeSample[25];
            for (int i = 0; i < samples.Length; i++)
            {
                float u = i / (float)(samples.Length - 1);
                var tangent = (point(Mathf.Min(1, u + .01f)) - point(Mathf.Max(0, u - .01f))).Normalized();
                // Match the cubic ease used to reveal the actual sword ribbon.
                samples[i] = new BladeSample(point(u), tangent, start + duration * (1 - Mathf.Pow(1 - u, 1f / 3f)));
            }
            return samples;
        }
        var liftSamples = SampleBlade(LiftPoint, LiftStart, LiftDrawTime);
        var sweepSamples = SampleBlade(SweepPoint, SweepStart, SweepDrawTime);

        SwordGust Gust(BladeSample[] samples, Vector2 origin, float birth, bool shard, bool rising)
        {
            if (rising && birth > LiftStart + .51f) return default;
            var closest = samples[0];
            float distanceSquared = float.MaxValue;
            foreach (var sample in samples)
            {
                float candidate = origin.DistanceSquaredTo(sample.Position);
                if (candidate >= distanceSquared) continue;
                distanceSquared = candidate;
                closest = sample;
            }
            var offset = origin - closest.Position;
            var normal = new Vector2(-closest.Tangent.Y, closest.Tangent.X);
            float side = offset.Dot(normal) < 0 ? -1 : 1;
            var outward = offset.LengthSquared() > _unit * _unit ? offset.Normalized() : normal * side;
            float reach = (rising ? 150 : 270) * _unit;
            float strength = Mathf.Exp(-Mathf.Sqrt(distanceSquared) / reach)
                * Mathf.Exp(-Mathf.Max(0, birth - closest.Arrival) / .95f) * Rand(.75f, 1.25f);
            float speed = (shard ? 185 : 310) * (rising ? .72f : 1) * strength;
            var direction = (closest.Tangent * .8f + outward * .9f).Normalized().Rotated(Rand(-.22f, .22f));
            return new SwordGust(direction * speed * _unit, normal * side,
                Mathf.Max(birth, closest.Arrival) + Rand(.012f, .045f), Rand(.28f, .5f),
                (shard ? 14 : 26) * strength * _unit, Rand(7, 12),
                side * (shard ? Rand(7, 16) : 0) * strength);
        }

        // The cut finishes first. Its wake then lifts nearby fragments before reaching
        // the rest of the battlefield; spatial jitter keeps this from becoming a row.
        float WakeBirth(Vector2 origin)
        {
            float distance = float.MaxValue;
            for (int sample = 0; sample <= 16; sample++)
                distance = Mathf.Min(distance, origin.DistanceTo(SweepPoint(sample / 16f)));
            return DebrisStart + Mathf.Min(.28f, distance / (1500 * _unit)) + Rand(.01f, .13f);
        }

        // Uneven pockets plus sparse background coverage leave natural gaps.
        var clusters = new DustCluster[random.Next(6, 11)];
        for (int i = 0; i < clusters.Length; i++)
            clusters[i] = new DustCluster(Field(Rand(.07f, .93f), Rand(.08f, .92f)),
                _fieldBounds.Size * new Vector2(Rand(.025f, .105f), Rand(.055f, .23f)), Rand(0, Mathf.Tau));

        Vector2 Scatter()
        {
            if (random.NextSingle() < .38f)
                return Field(Rand(.025f, .975f), Rand(.04f, .96f));
            for (int attempt = 0; attempt < 6; attempt++)
            {
                var cluster = clusters[random.Next(clusters.Length)];
                var point = cluster.Center
                    + new Vector2(Gaussian() * cluster.Spread.X, Gaussian() * cluster.Spread.Y).Rotated(cluster.Rotation);
                if (_fieldBounds.HasPoint(point)) return point;
            }
            return Field(Rand(.04f, .96f), Rand(.06f, .94f));
        }

        void Add(ParticleKind kind, Vector2 origin, Vector2 velocity, float birth, float life,
            float size, Color tint, float curl, float depth, bool ambient = false, bool affectedBySword = true)
        {
            Vector2[]? silhouette = null;
            if (kind == ParticleKind.Shard)
            {
                silhouette = new Vector2[5];
                for (int corner = 0; corner < silhouette.Length; corner++)
                {
                    float angle = corner * Mathf.Tau / silhouette.Length + Rand(-.24f, .24f);
                    silhouette[corner] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Rand(.58f, 1.15f);
                }
            }
            _particles.Add(new WindParticle(kind, origin, velocity * _unit,
                new Vector2(Rand(-55, 55), Rand(-35, 65)) * depth * _unit,
                tint, birth, Mathf.Min(life, Lifetime - birth), size * _unit,
                Rand(0, Mathf.Tau), curl * _unit, Rand(.2f, .95f), ambient ? Rand(.8f, 2.2f) : Rand(1.7f, 9.5f),
                Rand(-9, 9), Rand(.28f, .95f), ambient ? Rand(.5f, .85f) : Rand(.3f, .9f),
                Rand(.035f, .16f), Rand(.22f, .58f), silhouette,
                affectedBySword ? Gust(liftSamples, origin, birth, kind == ParticleKind.Shard, rising: true) : default,
                affectedBySword ? Gust(sweepSamples, origin, birth, kind == ParticleKind.Shard, rising: false) : default));
        }

        for (int i = 0; i < 460; i++)
        {
            bool ambient = i < 160;
            float depth = Rand(.3f, 1.1f), direction = Rand(0, Mathf.Tau);
            bool glint = !ambient && random.NextSingle() < .022f;
            var velocity = new Vector2(Mathf.Cos(direction), Mathf.Sin(direction))
                * (ambient ? Rand(3, 16) : Rand(10, 45)) * depth;
            var origin = ambient && random.NextSingle() < .55f
                ? SweepPoint(Rand(0, 1)) + new Vector2(Rand(-70, 70), Rand(-145, 145)) * _unit
                : Scatter();
            Add(glint ? ParticleKind.Glint : ParticleKind.Dust, origin, velocity,
                ambient ? Rand(-.3f, -.08f) : WakeBirth(origin), ambient ? 2.6f : Rand(.6f, 1.45f),
                ambient ? Rand(1.3f, 2.5f) : Rand(.8f, glint ? 2.8f : 2),
                ambient ? WindGray.Lerp(PaleShard, Rand(.1f, .4f))
                    : glint ? ColdGlint : DeepSlate.Lerp(WindGray, Rand(.35f, .95f)),
                (ambient ? Rand(1, 3) : Rand(2, 8)) * depth, depth, ambient);
        }

        // Most of the field is actual irregular debris, with a few larger near-camera pieces.
        for (int i = 0; i < 660; i++)
        {
            float depth = Rand(.35f, 1.35f), angle = Rand(0, Mathf.Tau), roll = random.NextSingle();
            float size = roll < .68f ? Rand(1.2f, 3.4f) : roll < .94f ? Rand(3.4f, 6.8f) : Rand(7, 12);
            var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * .8f) * Rand(8, 45) * depth;
            Color tint = random.NextSingle() < .28f
                ? DustGray.Lerp(PaleShard, Rand(.2f, .95f))
                : DeepSlate.Lerp(DustGray, Rand(0, .95f));
            var origin = Scatter();
            Add(ParticleKind.Shard, origin, velocity, WakeBirth(origin),
                Rand(.65f, 1.5f), size, tint, Rand(4, 24) * depth, depth);
        }

        // Flecks trail the completed cut, rather than preceding the rising blade.
        for (int i = 0; i < 120; i++)
        {
            float u = Rand(0, 1), angle = Rand(-Mathf.Pi, Mathf.Pi);
            var origin = SweepPoint(u)
                + new Vector2(Rand(-22, 22), Rand(-27, 27)) * _unit;
            var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Rand(20, 105);
            Add(ParticleKind.Streak, origin, velocity, DebrisStart + u * .07f + Rand(.01f, .10f), Rand(.45f, 1.25f),
                Rand(1.3f, 5), DustGray.Lerp(PaleShard, Rand(.1f, .9f)), Rand(3, 17), Rand(.4f, 1.1f));
        }

        for (int i = 0; i < 18; i++)
        {
            float angle = Rand(0, Mathf.Tau);
            Add(ParticleKind.Dust, hilt, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Rand(12, 45),
                Rand(.28f, .5f), Rand(.3f, .7f), Rand(.8f, 2.2f), HiltMote, Rand(2, 5), .5f, affectedBySword: false);
        }

        foreach (var target in _hitPoints)
        {
            for (int i = 0; i < 22; i++)
            {
                float angle = Rand(0, Mathf.Tau);
                bool glint = random.NextSingle() < .09f;
                Add(glint ? ParticleKind.Glint : ParticleKind.Shard, target,
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Rand(25, 105),
                    DebrisStart + Rand(0, .10f), Rand(.35f, .8f), Rand(1.3f, 3.8f),
                    glint ? ColdGlint : DustGray.Lerp(PaleShard, Rand(.1f, 1)), Rand(3, 12), Rand(.4f, 1));
            }
        }

        _particleLines = new Vector2[_particles.Count * 4];
        _particleColors = new Color[_particles.Count * 2];
        PrepareResources();
        int mistCount = random.Next(6, 10);
        for (int i = 0; i < mistCount; i++)
        {
            var size = _fieldBounds.Size * new Vector2(Rand(.18f, .46f), Rand(.3f, .72f));
            var center = Field(Rand(.15f, .85f), Rand(.2f, .8f));
            _mistPatches.Add(new MistPatch(new Rect2(center - size * .5f, size),
                new Vector2(Rand(-25, 40), Rand(-15, 15)) * _unit,
                WakeBirth(center), Rand(.7f, 1.2f), Rand(.18f, .5f)));
        }
        _debrisLayer = new Node2D
        {
            ZIndex = -1,
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Mix }
        };
        _debrisLayer.Draw += () =>
        {
            DrawWindAtmosphere(_debrisLayer);
            DrawParticles(_debrisLayer, luminous: false);
            DrawDebris();
        };
        AddChild(_debrisLayer);
    }

    private Vector2 ParticlePosition(in WindParticle particle, float age)
    {
        float x = Mathf.Sin(particle.Phase + age * particle.Frequency) - Mathf.Sin(particle.Phase);
        float y = Mathf.Cos(particle.Phase * .73f + age * particle.Frequency * .61f) - Mathf.Cos(particle.Phase * .73f);
        float travel = particle.Drag * (1 - Mathf.Exp(-age / particle.Drag));
        return particle.Origin + particle.Velocity * travel + particle.Acceleration * (travel * travel * .5f)
            + new Vector2(x, y) * particle.Curl
            + GustOffset(particle.LiftGust, particle.Birth + age)
            + GustOffset(particle.SweepGust, particle.Birth + age);
    }

    private static float GustTravel(in SwordGust gust, float time)
    {
        float age = time - gust.Time;
        return age <= 0 || gust.Drag <= 0 ? 0 : gust.Drag * (1 - Mathf.Exp(-age / gust.Drag));
    }

    private static Vector2 GustOffset(in SwordGust gust, float time)
    {
        float age = time - gust.Time;
        if (age <= 0 || gust.Drag <= 0) return Vector2.Zero;
        float curl = (1 - Mathf.Exp(-age * 12)) * Mathf.Exp(-age / .65f) * Mathf.Sin(age * gust.Frequency);
        return gust.Velocity * GustTravel(gust, time) + gust.CurlDirection * (curl * gust.Curl);
    }

    private float ParticleAlpha(in WindParticle particle, float age)
    {
        var position = ParticlePosition(particle, age);
        float edge = Mathf.Min(Mathf.Min(position.X - _fieldBounds.Position.X, _fieldBounds.End.X - position.X),
            Mathf.Min(position.Y - _fieldBounds.Position.Y, _fieldBounds.End.Y - position.Y));
        return Smooth(age / particle.FadeIn)
            * (1 - Smooth((age / particle.Life - (1 - particle.FadeOut)) / particle.FadeOut))
            * Smooth(edge / (16 * _unit)) * particle.Opacity;
    }

    private void DrawParticles(CanvasItem canvas, bool luminous)
    {
        int lines = 0;
        void Line(Vector2 from, Vector2 to, Color color)
        {
            _particleLines[lines * 2] = from;
            _particleLines[lines * 2 + 1] = to;
            _particleColors[lines++] = color;
        }

        foreach (var particle in _particles)
        {
            float age = _age - particle.Birth;
            if (particle.Kind == ParticleKind.Shard || (particle.Kind == ParticleKind.Glint) != luminous
                || age <= 0 || age >= particle.Life) continue;
            float alpha = ParticleAlpha(particle, age);
            var position = ParticlePosition(particle, age);
            var color = new Color(particle.Tint, alpha * (particle.Kind == ParticleKind.Dust ? .62f : .75f));
            if (particle.Kind == ParticleKind.Glint)
            {
                float size = particle.Size * (.45f + .55f * Mathf.Pow(Mathf.Sin(age * particle.Frequency + particle.Phase), 2));
                var axis = new Vector2(Mathf.Cos(particle.Phase), Mathf.Sin(particle.Phase));
                var normal = new Vector2(-axis.Y, axis.X);
                Line(position - axis * size, position + axis * size, color);
                Line(position - normal * size * .65f, position + normal * size * .65f, color);
            }
            else
            {
                var direction = (ParticlePosition(particle, age + .01f) - position).Normalized();
                Line(position, position - direction * particle.Size, color);
            }
        }
        if (lines > 0)
            canvas.DrawMultilineColors(_particleLines.AsSpan(0, lines * 2), _particleColors.AsSpan(0, lines), .9f * _unit, true);
    }

    private void DrawDebris()
    {
        foreach (var particle in _particles)
        {
            float age = _age - particle.Birth;
            if (particle.Kind != ParticleKind.Shard || age <= 0 || age >= particle.Life) continue;
            float alpha = ParticleAlpha(particle, age);
            if (alpha <= .012f) continue;
            var position = ParticlePosition(particle, age);
            float kickSpin = particle.LiftGust.Spin * GustTravel(particle.LiftGust, _age)
                + particle.SweepGust.Spin * GustTravel(particle.SweepGust, _age);
            float angle = particle.Phase + age * particle.Spin + kickSpin;
            var axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * particle.Size;
            float tumble = .28f + .72f * Mathf.Abs(Mathf.Cos(particle.Phase + age * particle.Frequency + kickSpin * .6f));
            if (particle.Size < 1.8f * _unit)
            {
                _debrisLayer.DrawLine(position - axis * .5f, position + axis * .5f,
                    new Color(particle.Tint, alpha), .9f * _unit, true);
                continue;
            }
            // Keep a minimum projected face width; no degenerate edge-on polygons.
            var normal = new Vector2(-axis.Y, axis.X) * Mathf.Max(.22f, particle.Aspect * tumble);
            for (int corner = 0; corner < _shardPolygon.Length; corner++)
            {
                var shape = particle.Silhouette![corner];
                _shardPolygon[corner] = position + axis * shape.X + normal * shape.Y;
            }
            float light = .76f + .24f * tumble;
            _debrisLayer.DrawColoredPolygon(_shardPolygon,
                new Color(particle.Tint.R * light, particle.Tint.G * light, particle.Tint.B * light, alpha));
        }
    }

    private void DrawWindAtmosphere(CanvasItem canvas)
    {
        float alpha = Envelope(DebrisStart, 2.25f, .7f);
        if (alpha <= 0) return;
        // No repeated arcs: only faint, uneven haze beneath the independently moving fragments.
        canvas.DrawTextureRect(_atmosphere, _fieldBounds, false, new Color(1, 1, 1, alpha * .35f));
        foreach (var patch in _mistPatches)
        {
            float age = _age - patch.Birth;
            if (age <= 0 || age >= patch.Life) continue;
            float opacity = Smooth(age / .18f) * (1 - Smooth((age / patch.Life - .4f) / .6f)) * patch.Opacity;
            canvas.DrawTextureRect(_atmosphere, new Rect2(patch.Bounds.Position + patch.Drift * age, patch.Bounds.Size),
                false, new Color(1, 1, 1, opacity));
        }
    }
}
