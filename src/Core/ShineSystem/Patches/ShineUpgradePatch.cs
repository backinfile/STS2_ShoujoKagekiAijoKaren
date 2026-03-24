using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ShoujoKagekiAijoKaren.src.Core.ShineSystem.Patches;

/// <summary>
/// 卡牌升级补丁 - 在卡牌升级后恢复闪耀值
/// 闪耀值如果本来就是满的，会显示金色高亮
/// </summary>
public static class ShineUpgradePatch
{
    private static SpireField<CardModel, bool> InUpgradePreview = new SpireField<CardModel, bool>(() => false);

    public static bool InUpgradePreviewMode(CardModel card)
    {
        return InUpgradePreview.Get(card);
    }

    // 卡牌升级时恢复闪耀值
    [HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Upgrade), typeof(IEnumerable<CardModel>), typeof(CardPreviewStyle))]
    public static class CardCmd_Upgrade_Patch
    {
        static void Prefix(IEnumerable<CardModel> cards)
        {
            foreach (var card in cards)
            {
                card.RestoreShineToMax();
            }
        }
    }

    // 卡牌预览时，先提前设置预览变量

    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
    public static class NCardUpdateVisualsPatch
    {
        static void Prefix(NCard __instance, PileType pileType, CardPreviewMode previewMode)
        {
            var card = __instance.Model;
            if (card == null) return;
            InUpgradePreview.Set(card, previewMode == CardPreviewMode.Upgrade);
            //MainFile.Logger.Info($"NCard.UpdateVisuals Prefix: card={card.Title}, previewMode={previewMode}, InUpgradePreview={InUpgradePreview.Get(card)}");
        }
    }
}
