using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using System;

namespace ShoujoKagekiAijoKaren.src.Core.Shine.ShinePatches;

/// <summary>
/// 闪耀牌堆核心补丁 - 简化方案：SpireField + AsyncLocal
///
/// 原理：
/// 1. OnPlayWrapper 状态机 Prefix：将 choiceContext 存入 AsyncLocal
/// 2. ModifyCardPlayResultPileTypeAndPosition Prefix：判定闪耀耗尽，从 AsyncLocal 取 ctx 存入 SpireField
/// 3. CardPileCmd.Add Prefix：拦截 ShineDepletePile，从 SpireField 取 ctx 调用 HandleShineDepletePileAsync
/// </summary>
public static class ShinePilePatch
{
    /// <summary>存储每张卡牌对应的 PlayerChoiceContext</summary>
    private static readonly SpireField<CardModel, PlayerChoiceContext?> CardContext = new(() => null);

    /// <summary>是否应进入闪耀耗尽流程（已初始化Shine且当前值==0）</summary>
    private static bool ShouldEnterShinePile(CardModel card)
    {
        if (!card.IsShineCard()) return false;
        return card.GetShineValue() == 0;
    }

    /// <summary>
    /// 卡牌即将被放入闪耀牌堆时，记录 PlayerChoiceContext 以供后续 Patch 使用
    /// </summary>
    /// <param name="card"></param>
    /// <param name="choiceContext"></param>
    public static void BeforePlayWrapper(CardModel card, PlayerChoiceContext choiceContext)
    {
        if (ShouldEnterShinePile(card))
        {
            CardContext[card] = choiceContext;
            MainFile.Logger.Info($"[ShinePilePatch] '{card.Title}' marked for shine depletion (shine=0)");
        }
    }


    /// <summary>
    /// 处理闪耀耗尽牌堆逻辑（替换 CardPileCmd.Add 的自定义方法）
    /// </summary>
    public static async Task HandleShineDepletePileAsync(
        PlayerChoiceContext choiceContext,
        CardModel card,
        PileType pile,
        CardPilePosition position,
        AbstractModel? source,
        bool skipVisuals)
    {
        // IsDupe 直接移除（不进入 Shine Pile）
        if (card.IsDupe)
        {
            await CardPileCmd.RemoveFromCombat(card);
            return;
        }

        MainFile.Logger.Info($"[ShinePilePatch] '{card.Title}' handling shine depletion");

        // 在 RemoveFromCurrentPile 之前找战斗 NCard（用于播放动画）
        NCard? combatCardNode = NCard.FindOnTable(card);

        // 播放删除动画
        ShinePileManager.PlayShineDepletionAnimation(card, combatCardNode);

        // 移入 ShinePile，传入 choiceContext
        await ShinePileManager.MoveToShinePile(card, choiceContext);

        // 触发 GlobalMoveSystem 事件（从 Play 到 null，表示离开战斗）
        //var combatState = card.CombatState ?? card.Owner?.Creature?.CombatState;
        //if (combatState != null && card.Owner != null)
        //{
        //    await Hook.AfterCardChangedPiles(
        //        card.Owner.RunState,
        //        combatState,
        //        target,
        //        PileType.Play,
        //        null
        //    );
        //}
    }

    /// <summary>
    /// 修改卡牌打出后的结果牌堆和位置
    /// </summary>
    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyCardPlayResultPileTypeAndPosition))]
    public static class Hook_ModifyCardPlayResultPileTypeAndPosition_Patch
    {
        public static bool Prefix(ref (PileType pileType, CardPilePosition position) __result, CardModel card, ref IEnumerable<AbstractModel> modifiers)
        {
            MainFile.Logger.Info($"[ShinePilePatch] Checking if '{card.Title}' should enter ShineDepletePile...");
            // 需要耗尽的移动到耗尽牌堆
            if (ShouldEnterShinePile(card))
            {
                __result = (KarenCustomEnum.ShineDepletePile, CardPilePosition.Bottom);
                modifiers = [];
                MainFile.Logger.Info($"[ShinePilePatch] '{card.Title}' -> {__result}");
                return false;
            }
            // 不满足条件的放过
            return true;
        }
    }

    /// <summary>
    /// Patch 3: Prefix CardPileCmd.Add
    /// 拦截 ShineDepletePile，从 SpireField 取 ctx 调用 HandleShineDepletePileAsync
    /// </summary>
    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add))]
    [HarmonyPatch(new Type[] { typeof(CardModel), typeof(PileType), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool) })]
    public static class CardPileCmd_Add_Patch
    {
        public static bool Prefix(ref Task<CardPileAddResult> __result, CardModel card, PileType newPileType, CardPilePosition position, AbstractModel? source, bool skipVisuals)
        {
            MainFile.Logger.Info($"[ShinePilePatch] Intercepting Add of '{card.Title}' to {newPileType}...");
            if (newPileType != KarenCustomEnum.ShineDepletePile)
                return true;

            // 从 SpireField 获取 ctx
            var ctx = CardContext.Get(card);
            CardContext.Set(card, null!); // 清空
            if (ctx == null)
            {
                MainFile.Logger.Error($"[ShinePilePatch] No PlayerChoiceContext found for '{card.Title}' when adding to ShineDepletePile!");
                __result = Task.FromResult(new CardPileAddResult
                {
                    cardAdded = card,
                    success = false
                });
                return false;
            }

            // 异步执行闪耀耗尽处理，这个方法的返回值没用，直接返回null简化处理
            __result = Handle(ctx, card, newPileType, position, source, skipVisuals);
            return false;
        }

        private static async Task<CardPileAddResult> Handle(
            PlayerChoiceContext choiceContext, CardModel card, PileType pile, CardPilePosition position, AbstractModel? addedBy, bool skipVisuals)
        {
            await HandleShineDepletePileAsync(choiceContext, card, pile, position, addedBy, skipVisuals);
            return new CardPileAddResult
            {
                cardAdded = card,
                success = false
            };
        }
    }
}

/// <summary>
/// 战斗结束后打印 Karen 玩家的闪耀牌堆内容（调试日志）
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.AfterCombatEnd))]
public static class Player_AfterCombatEnd_ShinePilePatch
{
    [HarmonyPostfix]
    static void Postfix(Player __instance)
    {
        if (__instance.Character is not Karen) return;

        var pile = ShinePileManager.GetShinePile(__instance);
        int total = pile.Count;
        int unique = ShinePileManager.GetDisposedShineCardUniqueCount(__instance);

        if (total == 0)
        {
            MainFile.Logger.Info($"[ShinePile] 战斗结束 — 闪耀牌堆为空");
            return;
        }

        var cardList = string.Join(", ", pile.Select(c => $"{c.Title}({c.GetShineMaxValue()})"));
        MainFile.Logger.Info($"[ShinePile] 战斗结束 — 共 {total} 张（{unique} 种）: {cardList}");
    }
}
