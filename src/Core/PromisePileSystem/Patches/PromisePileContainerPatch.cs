using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using System.Linq;

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

}
