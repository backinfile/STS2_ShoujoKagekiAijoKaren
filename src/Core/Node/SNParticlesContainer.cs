using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace ShoujoKagekiAijoKaren.Core;

[GlobalClass]
public partial class SNParticlesContainer : NParticlesContainer
{
    public override void _Ready()
    {
        var particles = new Godot.Collections.Array<GpuParticles2D>();
        foreach (var child in GetChildren())
        {
            if (child is GpuParticles2D p)
                particles.Add(p);
        }
        Traverse.Create(this).Field<Godot.Collections.Array<GpuParticles2D>>("_particles").Value = particles;
    }
}
