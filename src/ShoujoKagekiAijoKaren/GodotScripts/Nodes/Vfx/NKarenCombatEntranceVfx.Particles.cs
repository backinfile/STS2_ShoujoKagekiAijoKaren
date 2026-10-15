using System;
using System.Collections.Generic;
using Godot;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

public partial class NKarenCombatEntranceVfx
{
    private enum ParticleKind { Floor, Cloth, Sword }
    private sealed class Particle
    {
        public int Owner;
        public ParticleKind Kind;
        public float Birth, Life, Radius, Spin, Unit;
        public Vector2 Position, Velocity;
        public bool Born;
    }
    private readonly List<Particle> _particles = new();
    public int ActiveParticleCount
    {
        get { int count = 0; foreach (var p in _particles) if (p.Born && _age < p.Birth + p.Life) count++; return count; }
    }

    private static float Variety(int i) => Mathf.PosMod(Mathf.Sin(i * 127.1f + 311.7f) * 43758.5453f, 1);
    private void SeedParticles()
    {
        for (int owner = 0; owner < _karens.Count; owner++)
        for (int i = 0; i < 48; i++)
        {
            var kind = i < 12 ? ParticleKind.Floor : i < 30 ? ParticleKind.Cloth : ParticleKind.Sword;
            float u = Variety(i + owner * 53), v = Variety(i + 83 + owner * 17);
            _particles.Add(new Particle { Owner = owner, Kind = kind,
                Birth = kind switch { ParticleKind.Floor => .44f + u * .16f, ParticleKind.Cloth => .54f + u * .20f, _ => .79f + u * .12f },
                Life = .30f + v * .28f, Radius = kind == ParticleKind.Cloth ? 2 + u * 2.5f : 1 + u * 1.6f,
                Spin = (v - .5f) * 8,
                Velocity = kind switch { ParticleKind.Floor => new(-42 - u * 75, -12 - v * 22),
                    ParticleKind.Cloth => new(-38 - u * 110, -20 - v * 34),
                    _ => Vector2.FromAngle(-Mathf.Pi * .9f + u * Mathf.Pi * 1.4f) * (24 + v * 55) } });
        }
    }

    private void AdvanceParticles()
    {
        foreach (var p in _particles)
        {
            if (p.Born || _age < p.Birth) continue;
            var karen = _karens[p.Owner];
            if (!GodotObject.IsInstanceValid(karen) || !karen.IsInsideTree()) continue;
            var (body, foot, unit) = Anchor(karen);
            p.Unit = unit; p.Born = true;
            float spread = Variety(p.Owner * 73 + (int)(p.Birth * 1000));
            p.Position = p.Kind switch { ParticleKind.Floor => foot + new Vector2(-18 + spread * 42, -5) * unit,
                ParticleKind.Cloth => body + new Vector2(-20 - spread * 38, -46 - spread * 18) * unit,
                _ => SwordTip(karen, foot, unit) };
        }
    }

    private void DrawParticles(Node2D layer, bool foreground)
    {
        foreach (var p in _particles)
        {
            float t = _age - p.Birth;
            if (!p.Born || t < 0 || t >= p.Life || foreground != (p.Kind == ParticleKind.Sword)) continue;
            float alpha = Smooth(t, 0, .035f) * (1 - Smooth(t, p.Life * .35f, p.Life));
            var point = p.Position + (p.Velocity * t + new Vector2(0, 24 * t * t)) * p.Unit;
            var color = p.Kind switch { ParticleKind.Floor => Blue, ParticleKind.Cloth => Red, _ => Gold };
            if (p.Kind == ParticleKind.Cloth)
            {
                float r = p.Radius * p.Unit, angle = p.Spin * t;
                var axis = Vector2.FromAngle(angle); var side = axis.Orthogonal();
                color.A = alpha * .7f;
                layer.DrawColoredPolygon([point - axis * r * 1.6f, point + side * r * .45f,
                    point + axis * r * 1.6f, point - side * r * .45f], color);
                Soft(layer, point, Vector2.One * r * 5, Red, alpha * .13f);
            }
            else
            {
                Soft(layer, point, new Vector2(p.Kind == ParticleKind.Floor ? 16 : 10, 5) * p.Radius * p.Unit,
                    color, alpha * (p.Kind == ParticleKind.Floor ? .18f : .4f), p.Velocity.Angle());
                if (p.Kind == ParticleKind.Sword)
                {
                    color.A = alpha * .8f;
                    layer.DrawLine(point, point - p.Velocity.Normalized() * p.Radius * 3 * p.Unit, color, p.Unit, true);
                }
            }
        }
    }
}
