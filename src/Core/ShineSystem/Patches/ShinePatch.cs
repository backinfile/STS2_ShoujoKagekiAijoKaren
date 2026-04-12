using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Godot.HttpRequest;

namespace ShoujoKagekiAijoKaren.src.Core.Shine.ShinePatches;

/// <summary>
/// Harmony补丁：统一处理 Shine 关键字逻辑
/// 使用 SpireField 支持任何卡牌动态添加闪耀
///
/// 工作流程：
/// 1. OnPlayWrapper Postfix 中减少闪耀值
/// 2. ShinePilePatch 拦截 CardPileCmd.Add，检查闪耀值==0并重定向到闪耀牌堆
/// </summary>
public static class ShinePatch
{
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
    public static class ShineValuePatch
    {
        static void Prefix(CardModel __instance, PlayerChoiceContext choiceContext)
        {
            // 只处理有闪耀值的卡牌
            if (!__instance.HasShine()) return;

            // 减少闪耀值
            var newValue = __instance.DecreaseShine();
            MainFile.Logger.Info($"卡牌 '{__instance.Title}' 闪耀值减少至 {newValue}");

            // 同步到 deckVersion 卡牌，确保下次从牌堆抽牌时 clone 获得正确的值
            var deckVersion = __instance.DeckVersion;
            if (deckVersion != null && deckVersion != __instance && deckVersion.IsShineCard())
            {
                deckVersion.SetShineCurrent(Math.Min(newValue, deckVersion.GetShineValue()));
                MainFile.Logger.Info($"[ShinePatch] Synced deckVersion '{deckVersion.Title}' shine to {newValue}");
            }

            BeforePlayWrapper(__instance, choiceContext);
        }
    }


    /// <summary>
    /// MutableClone补丁 - 在卡牌克隆时复制闪耀值
    /// 解决SpireField数据在MemberwiseClone后丢失的问题
    /// </summary>
    [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.MutableClone))]
    public static class MutableClone_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(AbstractModel __instance, AbstractModel __result)
        {
            // __instance 是原卡牌（canonical 或 mutable）
            // __result 是新克隆的卡牌（mutable）
            if (__instance is not CardModel source || __result is not CardModel clone)
                return;

            // 只处理闪耀牌
            if (!source.IsShineCard())
                return;

            // 复制闪耀值
            int currentValue = source.GetShineValue();
            int maxValue = source.GetShineMaxValue();


            // 直接使用内部字段设置，确保精确复制（不是累加）
            clone.SetShineMax(maxValue);
            clone.SetShineCurrent(currentValue);
            //MainFile.Logger.Info($"[MutableClone_Patch] Cloned '{clone.Title}' shine values: current={clone.GetShineValue()}, max={clone.GetShineMaxValue()}");
        }
    }


    /// <summary>存储每张卡牌对应的 PlayerChoiceContext</summary>
    private static readonly SpireField<CardModel, PlayerChoiceContext?> CardContext = new(() => null);

    /// <summary>是否应进入闪耀耗尽流程（已初始化Shine且当前值==0）</summary>
    public static bool ShouldEnterShinePile(CardModel card)
    {
        if (!card.IsShineCard()) return false; // 不是闪耀牌不要进约定牌堆
        if (card.GetShineValue() <= 0) return true; // 闪耀值耗尽了，需要进约定牌堆
        if (card.ShouldEnterShinePileAfterPlay()) return true; // 主动耗尽的，需要进约定牌堆
        return false; // 闪耀值还没耗尽
    }


    /// <summary>
    /// 卡牌即将被放入闪耀牌堆时，记录 PlayerChoiceContext 以供后续 Patch 使用
    /// </summary>
    /// <param name="card"></param>
    /// <param name="choiceContext"></param>
    public static void BeforePlayWrapper(CardModel card, PlayerChoiceContext choiceContext)
    {
        // 闪耀牌都有可能因为某些原因进入耗尽牌堆，直接记录上就行
        if (card.IsShineCard())
        {
            CardContext[card] = choiceContext;
        }
    }



    /// <summary>
    /// 修改卡牌打出后的结果牌堆和位置
    /// </summary>
    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyCardPlayResultPileTypeAndPosition))]
    public static class Hook_ModifyCardPlayResultPileTypeAndPosition_Patch
    {
        public static bool Prefix(ref (PileType pileType, CardPilePosition position) __result, CardModel card, ref IEnumerable<AbstractModel> modifiers)
        {
            //MainFile.Logger.Info($"[ShinePilePatch] Checking if '{card.Title}' should enter ShineDepletePile...");
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
    [HarmonyPatch([typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool)])]
    [HarmonyPriority(Priority.First)]
    public static class CardPileCmd_Add_Patch
    {

        /// <summary>
        /// 把要移动到约定牌堆的牌取出来
        /// </summary>
        public static bool Prefix(IEnumerable<CardModel> cards, CardPile newPile, CardPilePosition position, AbstractModel? source, bool skipVisuals, ref Task<IReadOnlyList<CardPileAddResult>> __result)
        {
            var takeOverCards = new List<CardModel>();
            foreach (var card in cards)
            {
                // 放行非闪耀牌堆的添加
                if (TakeOverCardPileAddCmd(card, newPile.Type))
                {
                    // 稍后统一处理这些卡牌，先从原牌堆移除
                    takeOverCards.Add(card);
                    MainFile.Logger.Info($"TakeOverCardPileAddCmd card {card.Title}");
                }
            }
            if (takeOverCards.Count > 0)
            {
                return Async.Prefix<IReadOnlyList<CardPileAddResult>>(ref __result, async () => {
                    cards = cards.Where(c => !takeOverCards.Contains(c)).ToList(); // 从原参数列表中移除这些卡牌，剩下的正常添加
                    var originResult = await OriginalMethodStub(cards, newPile, position, source, skipVisuals);
                    var myResult = await HandleShineDepletePileAsync(takeOverCards);
                    return originResult.Concat(myResult).ToList();
                });
            }

            return true; 
        }

        [HarmonyReversePatch]
        public static Task<IReadOnlyList<CardPileAddResult>> OriginalMethodStub(IEnumerable<CardModel> cards, CardPile newPile, CardPilePosition position, AbstractModel? source, bool skipVisuals)
        {
            throw new NotImplementedException("会自动被替换，不会走到这里");
        }


        /// <summary>
        /// patch原本游戏中的卡牌移动
        /// </summary>
        private static bool TakeOverCardPileAddCmd(CardModel card, PileType newPileType)
        {
            // 添加到闪耀耗尽牌堆的命令
            if (newPileType == KarenCustomEnum.ShineDepletePile)
            {
                return true;
            }
            // 某些卡牌会将自己移动到特定牌堆，修改他们的转向
            if (newPileType.IsCombatPile() && ShouldEnterShinePile(card))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 处理闪耀耗尽牌堆逻辑（替换 CardPileCmd.Add 的自定义方法）
        /// </summary>
        private static async Task<IReadOnlyList<CardPileAddResult>> HandleShineDepletePileAsync(List<CardModel> takeOverCards)
        {
            var result = new List<CardPileAddResult>();
            foreach (var card in takeOverCards)
            {
                // 异步执行闪耀耗尽处理
                result.Add(await HandleShineDepletePileAsync(card));
            }
            return result;
        }

        /// <summary>
        /// 处理闪耀耗尽牌堆逻辑（替换 CardPileCmd.Add 的自定义方法）
        /// </summary>
        private static async Task<CardPileAddResult> HandleShineDepletePileAsync(CardModel card)
        {
            var oldPile = card.Pile;

            // 从 SpireField 获取 ctx
            var choiceContext = CardContext.Get(card);
            CardContext.Set(card, null); // 清空历史记录

            // 如果没有ctx，就直接打印错误
            if (choiceContext == null)
            {
                MainFile.Logger.Error($"[ShinePilePatch] No PlayerChoiceContext found for '{card.Title}' when adding to ShineDepletePile!");
                await CardPileCmd.RemoveFromCombat(card);
                return new CardPileAddResult { cardAdded = card, success = false };
            }

            // IsDupe 直接移除（不进入 Shine Pile）
            if (card.IsDupe)
            {
                await CardPileCmd.RemoveFromCombat(card);
                MainFile.Logger.Info($"[ShinePilePatch] '{card.Title}' is a dupe, removed from combat without entering ShinePile");
                return new CardPileAddResult { cardAdded = card, success = false };
            }

            MainFile.Logger.Info($"[ShinePilePatch] '{card.Title}' handling shine depletion");

            // 播放删除动画
            NCard? combatCardNode = NCard.FindOnTable(card);
            ShinePileManager.PlayShineDepletionAnimation(card, combatCardNode);

            // 移入 ShinePile，传入 choiceContext
            await ShinePileManager.MoveToShinePile(card, choiceContext);

            // 返回移动结果
            return new CardPileAddResult { cardAdded = card, success = true, oldPile = oldPile, modifyingModels = [] };
        }
    }
}
