using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// Keeps selected Karen cards in the token pool while rendering them with rare-card visuals.
/// </summary>
public static class RareTokenCardVisualPatch
{
    private static bool IsRareTokenVisual(CardModel? card)
    {
        return card is KarenBanana or KarenShineReproduce;
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.BannerMaterial), MethodType.Getter)]
    public static class CardModel_BannerMaterial_Patch
    {
        private static bool Prefix(CardModel __instance, ref Material __result)
        {
            if (!IsRareTokenVisual(__instance))
            {
                return true;
            }

            __result = PreloadManager.Cache.GetMaterial("res://materials/cards/banners/card_banner_rare_mat.tres");
            return false;
        }
    }

    [HarmonyPatch(typeof(NCard), "GetTitleLabelOutlineColor")]
    public static class NCard_GetTitleLabelOutlineColor_Patch
    {
        private static void Postfix(NCard __instance, ref Color __result)
        {
            if (IsRareTokenVisual(__instance.Model))
            {
                __result = StsColors.cardTitleOutlineRare;
            }
        }
    }

    [HarmonyPatch(typeof(NCard), nameof(NCard.ActivateRewardScreenGlow))]
    public static class NCard_ActivateRewardScreenGlow_Patch
    {
        private static void Postfix(NCard __instance)
        {
            if (IsRareTokenVisual(__instance.Model))
            {
                __instance.CardHighlight.Modulate = NCardHighlight.gold;
            }
        }
    }
}
