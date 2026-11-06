using System;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Patches;

/// <summary>Observe the native mouse/controller play lifecycle. The tiny tracker
/// notifications can be inlined in the game's precompiled hand code.</summary>
[HarmonyPatch]
internal static class PromiseTransferDragPatch
{
    [HarmonyPostfix, HarmonyPatch(typeof(NPlayerHand), "StartCardPlay")]
    private static void SelectedPostfix(NHandCardHolder holder)
    {
        var cardModel = holder.CardModel;
        if (cardModel == null) return;
        if (PromiseTransferPreviews.ForCard(cardModel) == PromiseTransferPreview.None) return;
        KarenPromiseVfxStarManager.SetTransferPreview(cardModel, true);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(NCardPlay), "Cleanup")]
    private static void Clear(NCardPlay __instance)
    {
        if (__instance.Holder?.CardModel is { } card)
            KarenPromiseVfxStarManager.SetTransferPreview(card, false);
    }
}

// The native wrapper includes choices, repeats, after-play hooks and the final
// pile move. Drag cleanup happens before these asynchronous effects finish.
[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
internal static class PromiseTransferResolutionPatch
{
    [HarmonyPrefix]
    private static void Begin(CardModel __instance, out IDisposable? __state)
        => __state = KarenPromiseVfxStarManager.BeginTransferResolution(__instance);

    [HarmonyPostfix]
    private static void Complete(ref Task __result, IDisposable? __state)
    {
        if (__state != null) __result = AwaitResolution(__result, __state);
    }

    private static async Task AwaitResolution(Task play, IDisposable scope)
    {
        try { await play; }
        finally { scope.Dispose(); }
    }

    [HarmonyFinalizer]
    private static void Failed(Exception? __exception, IDisposable? __state)
    {
        if (__exception != null) __state?.Dispose();
    }
}
