using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Patches;

public static class PromisePileContainerPatch
{

    /// <summary>
    /// 支持写法 CardPile.Get(KarenCustomEnum.PromisePile, player)，返回玩家的约定牌堆。
    /// </summary>
    [HarmonyPatch(typeof(CardPile), nameof(CardPile.Get))]
    public static class PromiseGetPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(PileType type, Player player, ref CardPile? __result)
        {
            if (type == KarenCustomEnum.PromisePile)
            {
                __result = PromisePileManager.GetPromisePile(player);
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// 支持写法 CardPile.Get(KarenCustomEnum.PromisePile, player)，返回玩家的约定牌堆。
    /// </summary>
    [HarmonyPatch(typeof(PileTypeExtensions), nameof(PileTypeExtensions.IsCombatPile))]
    public static class PromisePileIsCombatPilePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(PileType pileType, ref bool __result)
        {
            if (pileType == KarenCustomEnum.PromisePile)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }


    private static SpireField<PlayerCombatState, bool> PlayerCombatStatePatched = new SpireField<PlayerCombatState, bool>(() => false);


    /// <summary>
    /// 将CardPile加入AllCardPile
    /// </summary>
    [HarmonyPatch(typeof(PlayerCombatState), nameof(PlayerCombatState.AllPiles), MethodType.Getter)]
    public static class PlayerPilesPatch
    {
        public static void Postfix(
            ref CardPile[] ____piles,
            PlayerCombatState __instance)  // 注意：字段名需确认
        {
            if (PlayerCombatStatePatched.Get(__instance)) return;

            // 添加自定义牌堆到序列
            var customPile = PromisePileManager.GetPromisePile(__instance);
            ____piles = ____piles.Concat([customPile]).ToArray();
            PlayerCombatStatePatched.Set(__instance, true);
        }
    }


    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add))]
    [HarmonyPatch([typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool)])]
    public static class CardPileCmd_Add_To_PromisePile_Patch
    {
        /// <summary>
        /// Void模式下，放入约定牌堆改为放入抽牌堆
        /// 这个方法理论上要先于所有方法执行
        /// </summary>
        [HarmonyPriority(Priority.First * 10)]
        public static bool Prefix(IEnumerable<CardModel> cards, ref CardPile newPile, CardPilePosition position, AbstractModel? source, bool skipVisuals, ref Task<IReadOnlyList<CardPileAddResult>> __result)
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
                });
            }
        }
    }
}
