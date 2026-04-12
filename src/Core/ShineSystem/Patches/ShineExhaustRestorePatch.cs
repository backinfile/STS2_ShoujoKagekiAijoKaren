using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System.Collections.Generic;

namespace ShoujoKagekiAijoKaren.src.Core.ShineSystem.Patches;

/// <summary>
/// 当卡牌进入消耗牌堆时，恢复其闪耀值到最大值
/// </summary>
public static class ShineExhaustRestorePatch
{
    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardChangedPiles))]
    public static class CardPileCmd_Add_ExhaustRestore_Postfix
    {
        [HarmonyPrefix]
        public static void Prefix(CardModel card, PileType oldPile)
        {
            // 只有进入消耗牌堆的卡牌才处理
            if (card.Pile?.Type == PileType.Exhaust)
            {
                // 只处理有闪耀值的卡牌
                if (card.IsShineCard())
                {
                    // 恢复闪耀值
                    card.RestoreShineToMax();
                    if (card.GetShineMaxValue() < 0) card.SetShineCurrent(-1);
                    MainFile.Logger.Info($"[ShineExhaustRestorePatch] 卡牌 '{card.Title}' 进入消耗牌堆，闪耀值已恢复 ({card.GetShineValue()}/{card.GetShineMaxValue()})");
                }
            }

        }
    }
}
