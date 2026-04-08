using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

namespace ShoujoKagekiAijoKaren.src.Core.ShineSystem.Patches;

/// <summary>
/// 当卡牌进入消耗牌堆时，恢复其闪耀值到最大值
/// </summary>
public static class ShineExhaustRestorePatch
{
    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add))]
    [HarmonyPatch([typeof(CardModel), typeof(PileType), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool)])]
    public static class CardPileCmd_Add_ExhaustRestore_Postfix
    {
        [HarmonyPostfix]
        public static void Postfix(CardModel card, PileType newPileType)
        {
            // 只有进入消耗牌堆的卡牌才处理
            if (newPileType != PileType.Exhaust)
                return;

            // 只处理有闪耀值的卡牌
            if (!card.IsShineCard())
                return;

            // 恢复闪耀值到最大
            card.RestoreShineToMax();
            MainFile.Logger.Info($"[ShineExhaustRestorePatch] 卡牌 '{card.Title}' 进入消耗牌堆，闪耀值已恢复满 ({card.GetShineMaxValue()})");

            // 同步到 deckVersion，确保数据一致性
            var deckVersion = card.DeckVersion;
            if (deckVersion != null && deckVersion != card && deckVersion.IsShineCard())
            {
                deckVersion.RestoreShineToMax();
                MainFile.Logger.Info($"[ShineExhaustRestorePatch] 同步 deckVersion '{deckVersion.Title}' 闪耀值到最大值");
            }
        }
    }
}
