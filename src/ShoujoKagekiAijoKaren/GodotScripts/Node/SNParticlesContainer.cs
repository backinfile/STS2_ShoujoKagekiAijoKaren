using Godot;
using Godot.Collections;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

namespace ShoujoKagekiAijoKaren.Core;

public partial class SNParticlesContainer : Node2D
{
	private Array<GpuParticles2D> _particles = new();
	private readonly System.Collections.Generic.Dictionary<GpuParticles2D, int> _fullAmounts = new();

	public override void _Ready()
	{
		foreach (var child in GetChildren())
		{
			if (child is GpuParticles2D p)
			{
				_particles.Add(p);
				_fullAmounts[p] = p.Amount;
				p.Amount = KarenVfxQuality.ParticleCount(_fullAmounts[p]);
			}
		}
	}

	public void SetEmitting(bool emitting)
	{
		for (int i = 0; i < _particles.Count; i++)
		{
			_particles[i].Emitting = emitting;
		}
	}

	public void Restart()
	{
		for (int i = 0; i < _particles.Count; i++)
		{
			_particles[i].Amount = KarenVfxQuality.ParticleCount(_fullAmounts[_particles[i]]);
			_particles[i].Restart();
		}
	}
}
