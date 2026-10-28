using System;
using System.Runtime.CompilerServices;
using Godot;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

public enum KarenVfxLevel { Full, Balanced, Low }

/// <summary>Local presentation budgets. Gameplay, animation clocks and card ownership stay native.</summary>
internal static class KarenVfxQuality
{
    private sealed class DrawClock
    {
        public ulong Next;
        public KarenVfxLevel Level;
    }

    private static readonly ConditionalWeakTable<CanvasItem, DrawClock> DrawClocks = new();
    internal static KarenVfxLevel Level => KarenModConfig.VfxLevel;
    internal static bool AmbientEffects => Level != KarenVfxLevel.Low;
    internal static int ParticleStride => Level switch { KarenVfxLevel.Balanced => 2, KarenVfxLevel.Low => 4, _ => 1 };
    internal static int ParticleCount(int count) => count <= 0 ? 0 : Math.Max(1, (count + ParticleStride - 1) / ParticleStride);
    internal static float SpawnInterval(float interval) => interval * ParticleStride;

    // All geometry is still calculated at the original animation time. Only expensive canvas
    // rebuilding is capped; real card transforms, lifetimes, callbacks and tweens run each frame.
    internal static void Redraw(CanvasItem? surface)
    {
        if (surface == null) return;
        var level = Level;
        if (level == KarenVfxLevel.Full) { surface.QueueRedraw(); return; }
        var clock = DrawClocks.GetValue(surface, static _ => new DrawClock());
        ulong now = Time.GetTicksUsec();
        if (clock.Level == level && now < clock.Next) return;
        clock.Level = level;
        clock.Next = now + (level == KarenVfxLevel.Balanced ? 33333UL : 50000UL);
        surface.QueueRedraw();
    }
}
