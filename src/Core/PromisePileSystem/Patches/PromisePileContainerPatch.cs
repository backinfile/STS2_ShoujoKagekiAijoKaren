using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Patches;

public static class PromisePileContainerPatch
{

    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add))]
#if STS2_BETA
    [HarmonyPatch([typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool), typeof(bool)])]
#else
    [HarmonyPatch([typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool)])]
#endif
    public static class CardPileCmd_Add_To_PromisePile_Patch
    {
        /// <summary>
        /// Void模式下，放入约定牌堆改为放入抽牌堆
        /// 这个方法理论上要先于所有方法执行
        /// </summary>
        [HarmonyPriority(Priority.First * 10)]
        public static bool Prefix(IEnumerable<CardModel> cards, ref CardPile newPile, CardPilePosition position, [HarmonyArgument(3)] AbstractModel? source, ref bool skipVisuals, ref Task<IReadOnlyList<CardPileAddResult>> __result)
        {
            if (cards == null || !cards.Any()) return true;

            if (cards.First().Owner is not Player player) return true;

            if (newPile.Type == KarenCustomEnum.PromisePile && PromisePileManager.IsVoidMode(player))
            {
                newPile = PileType.Draw.GetPile(player);
                MainFile.Logger.Info($"Void模式 放入约定牌堆改为放入抽牌堆 count:{cards.Count()}");
            }
            return true;
        }

        /// <summary>
        /// 需要更新约定牌堆能力的计数
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(IEnumerable<CardModel> cards, CardPile newPile, ref Task<IReadOnlyList<CardPileAddResult>> __result)
        {
            if (cards == null || !cards.Any()) return;

            if (cards.First().Owner is not Player player) return;

            if (newPile.Type == KarenCustomEnum.PromisePile || (newPile.Type == PileType.Draw && PromisePileManager.IsVoidMode(player)))
            {
                Async.Postfix(ref __result, async () =>
                {
                    await PromisePileManager.UpdatePowerAsync(player);
                    if (PromisePileManager.IsVoidMode(player))
                    {
                        PromisePileManager.SetPileCountLabel(player, PileType.Draw.GetPile(player).Cards.Count);
                    }
                });
            }
        }
    }
}
