using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

public static class KarenBurnCardVfxPatch
{
    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
    private static class VisualsPatch
    {
        private static void Postfix(NCard __instance)
        {
            if (__instance.IsNodeReady()) Refresh(__instance);
        }
    }

    [HarmonyPatch(typeof(NCard), "ReloadOverlay")]
    private static class OverlayPatch
    {
        private static void Postfix(NCard __instance) => Refresh(__instance);
    }

    [HarmonyPatch(typeof(NCard), nameof(NCard.OnFreedToPool))]
    private static class PoolPatch
    {
        private static void Prefix(NCard __instance) => Clear(__instance);
    }

    private static readonly SpireField<NCard, NKarenBurnCardVfx?> Effects = new(() => null);

    // Use the same local overlay space as vanilla animated card overlays.
    // Keep our layer separate so native overlays and afflictions can coexist.
    private static void Refresh(NCard __instance)
    {
        var existing = Effects.Get(__instance);
        if (__instance.Model is not { } model || !KarenPromisePilePower.HasBurnDrawEffect(model))
        {
            Clear(__instance);
            return;
        }
        if (GodotObject.IsInstanceValid(existing) && !existing!.IsQueuedForDeletion()
            && existing.BelongsTo(model)) return;

        Clear(__instance);
        var effect = new NKarenBurnCardVfx(__instance, model);
        __instance.OverlayContainer.AddChild(effect);
        Effects.Set(__instance, effect);
    }

    private static void Clear(NCard __instance)
    {
        var existing = Effects.Get(__instance);
        Effects.Set(__instance, null);
        if (!GodotObject.IsInstanceValid(existing)) return;
        // Detach now: pooled NCard nodes can be reused before deferred deletion.
        existing!.GetParent()?.RemoveChild(existing);
        existing.QueueFree();
    }
}
