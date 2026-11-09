using ShoujoKageki.Core.V1.Presentation;
using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards;
using ShoujoKagekiAijoKaren.Core;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

public enum KarenVfxLevel { Full, Low, Off }

/// <summary>Local presentation budgets; gameplay and card ownership stay native.</summary>
internal static class KarenVfxQuality
{
    internal static KarenVfxLevel Level => (KarenVfxLevel)(int)CoreEffects.Level;
    internal static bool Enabled => CoreEffects.Enabled;
    internal static bool AmbientEffects => Enabled && CoreEffects.AmbientEffects;
    internal static int ParticleStride => CoreEffects.ParticleStride;
    internal static int ParticleCount(int count) => CoreEffects.ParticleCount(count);
    internal static float SpawnInterval(float interval) => Enabled ? CoreEffects.SpawnInterval(interval) : float.PositiveInfinity;
    internal static void Redraw(CanvasItem? surface) => CoreEffects.Redraw(surface, Enabled);
    private static IEnumerable<Node> Walk(Node node)
    {
        yield return node;
        foreach (var child in node.GetChildren())
            foreach (var item in Walk(child)) yield return item;
    }
    internal static void ApplyLevel()
    {
        CoreEffects.RefreshRegistered();
        if (Engine.GetMainLoop() is not SceneTree tree) return;
        if (!Enabled) ShoujoKagekiAijoKaren.src.Core.Patches.KarenCharSelectVideoController.Stop(immediatelyRestoreMusic: true);
        foreach (var item in Walk(tree.Root))
        {
            if (item is SNEnergyCounter energy) energy.RefreshEffectQuality();
            if (item is SNCreatureVisuals actor) actor.RefreshEffectQuality();
            if (item is SNParticlesContainer particles) particles.RefreshEffectQuality();
        }
    }
}
