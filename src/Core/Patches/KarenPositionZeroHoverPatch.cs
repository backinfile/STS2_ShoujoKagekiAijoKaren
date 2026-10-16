using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

[HarmonyPatch]
internal static class KarenPositionZeroHoverPatch
{
    [HarmonyPatch(typeof(NHandCardHolder), "DoCardHoverEffects")]
    [HarmonyPostfix]
    private static void Hover(NHandCardHolder __instance, bool isHovered)
    {
        if (__instance.CardNode is {} card) NKarenPositionZeroVfx.Hover(card, isHovered);
    }

    [HarmonyPatch(typeof(NHandCardHolder), nameof(NHandCardHolder.Clear))]
    [HarmonyPrefix]
    private static void Clear(NHandCardHolder __instance)
    {
        if (__instance.CardNode is {} card) NKarenPositionZeroVfx.Hover(card, false);
    }
}
