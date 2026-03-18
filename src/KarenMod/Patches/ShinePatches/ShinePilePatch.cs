using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.KarenMod.Patches;

/// <summary>
/// 闪耀牌堆核心补丁 - 拦截卡牌流向，重定向到闪耀牌堆
///
/// 方案：完全自定义，不依赖 PileType 枚举
/// - 拦截 CardPileCmd.Add，当卡牌闪耀值==0时重定向
/// - 在 CombatStart/CombatEnd 时管理闪耀牌堆生命周期
/// </summary>
public static class ShinePilePatch
{
    /// <summary>
    /// 检查卡牌是否应该进入闪耀牌堆
    /// 条件：有闪耀属性且当前值==0
    /// </summary>
    private static bool ShouldEnterShinePile(CardModel card)
    {
        if (!card.IsShineInitialized())
            return false;
        return card.GetShineValue() == 0;
    }

    /// <summary>
    /// 拦截 CardPileCmd.Add - 将闪耀值==0的卡牌重定向到闪耀牌堆
    ///
    /// 注意：CardPileCmd.Add 返回 Task<CardPileAddResult>
    /// Harmony 对异步方法的 Patch 需要特殊处理
    /// </summary>
    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add),
        typeof(CardModel), typeof(PileType), typeof(CardPilePosition),
        typeof(AbstractModel), typeof(bool))]
    public static class CardPileCmd_Add_Patch
    {
        /// <summary>
        /// 前置拦截：检查是否应该重定向到闪耀牌堆
        /// 只要卡牌闪耀值==0，无论目标是什么都拦截
        /// </summary>
        static bool Prefix(CardModel card, PileType newPileType, ref Task<CardPileAddResult> __result)
        {
            // 检查闪耀值是否为0
            if (!ShouldEnterShinePile(card))
                return true;

            // 检查是否已经在闪耀牌堆
            if (ShinePileManager.IsInShinePile(card))
            {
                MainFile.Logger.Warn($"[ShinePilePatch] Card '{card.Title}' already in shine pile");
                return true; // 允许正常流程（不应该发生）
            }

            // 重定向到闪耀牌堆
            MainFile.Logger.Info($"[ShinePilePatch] Redirecting '{card.Title}' from {newPileType} to shine pile (shine=0)");
            ShinePileManager.AddToShinePile(card);

            // 构造成功结果（欺骗调用方）
            __result = CreateSuccessResult(card);
            return false; // 阻止原方法执行
        }

        /// <summary>
        /// 构造成功的 Task<CardPileAddResult>
        /// 注意：async 方法的返回值是 Task<T>
        /// </summary>
        private static Task<CardPileAddResult> CreateSuccessResult(CardModel card)
        {
            var result = new CardPileAddResult
            {
                success = true,
                cardAdded = card,
                oldPile = card.Pile,
                modifyingModels = new List<AbstractModel>()
            };
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// 拦截 RemoveFromCombat - Power 牌的特殊处理
    /// </summary>
    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.RemoveFromCombat), typeof(CardModel), typeof(bool))]
    public static class CardPileCmd_RemoveFromCombat_Patch
    {
        static bool Prefix(CardModel card, bool skipVisuals)
        {
            // 检查是否应该进入闪耀牌堆（闪耀值==0）
            if (ShouldEnterShinePile(card))
            {
                // 重定向到闪耀牌堆
                MainFile.Logger.Info($"[ShinePilePatch] Redirecting Power card '{card.Title}' to shine pile (shine=0)");
                ShinePileManager.AddToShinePile(card);

                return false; // 阻止原方法执行
            }

            return true; // 允许正常流程
        }
    }
}
