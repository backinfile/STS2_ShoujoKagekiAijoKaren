using System.Collections.Generic;
using Godot;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>One additive draw per texture, with particle motion kept in managed state.</summary>
internal partial class NKarenBurnCardParticleLayer(Texture2D? texture, bool aura) : Node2D
{
    internal static CanvasItemMaterial SharedMaterial { get; } = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add
    };
    private struct Particle
    {
        public Vector2 Origin;
        public Color Color;
        public float Age, Duration, Scale, Rotation, Spin;
    }
    private readonly List<Particle> _particles = new(32);
    private readonly KarenVfxCanvasBatch _batch = new(128);
    private readonly Vector2 _textureSize = texture?.GetSize() ?? Vector2.Zero;
    private static bool _auraDirection = true;

    public override void _Ready() => Material = SharedMaterial;

    public void Emit(Vector2 origin, float warmup = 0)
    {
        var particle = new Particle { Origin = origin, Age = warmup };
        if (aura)
        {
            particle.Duration = 2;
            particle.Scale = (float)GD.RandRange(2.5, 2.7);
            particle.Color = new Color((float)GD.RandRange(.6, .7), 0, (float)GD.RandRange(.1, .2));
            particle.Rotation = (float)GD.RandRange(0, Mathf.Tau);
            _auraDirection = !_auraDirection;
            particle.Spin = Mathf.DegToRad((float)(_auraDirection ? GD.RandRange(0, 40) : GD.RandRange(-40, 0)));
        }
        else
        {
            particle.Duration = (float)GD.RandRange(1.3, 1.8);
            particle.Scale = (float)GD.RandRange(.6, 1);
            particle.Color = new Color((float)GD.RandRange(.5, 1), 0, (float)GD.RandRange(0, .2));
            particle.Rotation = Mathf.DegToRad((float)GD.RandRange(-8, 8));
        }
        _particles.Add(particle);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_particles.Count == 0) return;
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var particle = _particles[i];
            particle.Age += (float)delta;
            if (particle.Age >= particle.Duration) _particles.RemoveAt(i);
            else _particles[i] = particle;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (texture == null) return;
        _batch.Clear();
        foreach (var particle in _particles)
        {
            float progress = particle.Age / particle.Duration;
            float fade = Mathf.Clamp(progress <= .5f ? progress * 2 : (1 - progress) * 2, 0, 1);
            fade = fade * fade * (3 - 2 * fade);
            var position = aura ? particle.Origin : particle.Origin + Vector2.Up * (particle.Age * 40);
            var scale = aura ? Vector2.One * particle.Scale
                : new Vector2(particle.Scale * .8f, .1f + particle.Age * 2 * particle.Scale);
            var color = new Color(particle.Color, fade * (aura ? .3f : 1));
            _batch.Quad(position, _textureSize * scale, particle.Rotation + particle.Age * particle.Spin, color);
        }
        _batch.Flush(this, texture);
    }
}
