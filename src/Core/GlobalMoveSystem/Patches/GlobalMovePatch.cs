using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.GlobalMoveSystem.Patches;

internal static class GlobalMovePatch
{
    /// <summary>
    /// 缓存当前处于打出区的卡牌
    /// </summary>
    private static SpireField<CardModel, PileType> inPlayPile = new(() => PileType.None);


    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardChangedPiles))]
    public static class GlobalMovePilePatch
    {

        // Prefix 在 async 状态机启动前同步执行。
        // 此时卡牌已完成物理移动（CardPileCmd.Add 先移牌再调用 Hook），
        // 故 card.Pile?.Type 即为新牌堆，oldPile 参数为旧牌堆。
        // GlobalMove 不会处理打出区，因为需要特殊处理打出区
        // 会拼接两次移动为一次，例如 (Hand->Play,Play->Discard) 会合并为 (Hand->Discard)
        [HarmonyPrefix]
        private static void Prefix(
            IRunState runState, CombatState? combatState,
            CardModel card, PileType oldPile, AbstractModel? source)
        {
            // 模板不处理
            if (card.IsCanonical) return;

            PileType newPile = card.Pile?.Type ?? PileType.None;
            if (oldPile == null || oldPile == PileType.None || oldPile == PileType.Deck)
            {
                return;
            }
            // 不处理这种目标
            if (newPile == PileType.None || newPile == PileType.Deck)
            {
                inPlayPile.Set(card, PileType.None);
                return;
            }

            // 特殊处理打出区
            if (newPile == PileType.Play)
            {
                inPlayPile.Set(card, oldPile);
                return;
            }

            // 如果是从打出区过来的，合并两次移动
            if (oldPile == PileType.Play)
            {
                var cachedPile = inPlayPile.Get(card);
                if (cachedPile == PileType.None) // 之前没有缓存过，放弃这个
                {
                    return;
                }
                // 拼接两次移动
                oldPile = cachedPile;
                inPlayPile.Set(card, PileType.None);
            }

            GlobalMoveSystem.Invoke(card, oldPile, newPile, source);
        }
    }





    // 战斗结束后清理打出区缓存
    //[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
    //public static class GlobalMoveAfterCombatEndPatch
    //{
    //    [HarmonyPrefix]
    //    private static void Prefix()
    //    {
    //        GlobalMovePatch.inPlayPile.Clear();
    //    }
    //}
}
