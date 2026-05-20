using Godot;
using Godot.Collections;

namespace ShoujoKagekiAijoKaren.Core;

public partial class SNParticlesContainer : Node2D
{
	private Array<GpuParticles2D> _particles = new();

	public override void _Ready()
	{
		foreach (var child in GetChildren())
		{
			if (child is GpuParticles2D p)
				_particles.Add(p);
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
			_particles[i].Restart();
		}
	}
}
