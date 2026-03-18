using Godot;
using HarmonyLib;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Runs;
using ShoujoKagekiAijoKaren;
using System;
using System.Threading.Tasks;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

namespace ShoujoKagekiAijoKaren.src.KarenMod.Patches;

/// <summary>
/// Harmony补丁：统一处理 Shine 关键字逻辑
/// 使用 SpireField 支持任何卡牌动态添加闪耀
///
/// 拦截点：在卡牌进入弃牌堆或Power牌结算前处理
/// 当闪耀值归零时，直接从游戏中移除卡牌，不进入弃牌堆
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
public static class ShinePatch
{
    static void Postfix(CardModel __instance, PlayerChoiceContext choiceContext)
    {
        // 只处理有闪耀值的卡牌
        if (!__instance.HasShine()) return;

        // 减少闪耀值
        var newValue = __instance.DecreaseShine();
        MainFile.Logger.Info($"Card '{__instance.Title}' shine decreased to {newValue}");

        // 如果闪耀值归零，执行移除
        if (newValue <= 0)
        {
            // 异步执行移除（不阻塞当前流程，拦截补丁会根据闪耀值判断）
            _ = Task.Run(async () =>
            {
                try
                {
                    await RemoveShinedCardWithAnimation(__instance);
                }
                catch (Exception ex)
                {
                    MainFile.Logger.Error($"Error removing card with depleted shine: {ex}");
                }
            });
        }
    }

    /// <summary>
    /// 检查卡牌是否应该被移除（闪耀值已归零）
    /// </summary>
    public static bool ShouldRemoveCard(CardModel card)
    {
        // 有闪耀属性且当前值<=0（HasShine只检查>0，所以这里用GetShineValue检查）
        return card.IsShineInitialized() && card.GetShineValue() <= 0;
    }

    /// <summary>
    /// 移除卡牌并播放动画（参考商店删牌动画）
    /// </summary>
    private static async Task RemoveShinedCardWithAnimation(CardModel card)
    {
        try
        {
            // 确保在 RunState 中
            if (card.Owner?.RunState == null) return;

            // 如果卡牌还在手牌中，等待一下确保动画完成
            if (card.Pile?.Type == PileType.Hand)
            {
                await Task.Delay(300);
            }

            // 播放卡牌移除动画（类似商店删牌）
            await PlayCardRemovalAnimation(card);

            // 从当前牌堆移除
            if (card.Pile != null)
            {
                card.RemoveFromCurrentPile();
                MainFile.Logger.Info($"Card '{card.Title}' removed from pile (shine depleted)");
            }

            // 标记为已从状态中移除
            card.RemoveFromState();

            MainFile.Logger.Info($"Card '{card.Title}' fully removed from game (shine depleted)");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Error removing card with depleted shine: {ex}");
        }
    }

    /// <summary>
    /// 播放卡牌移除动画
    /// 参考 CardPileCmd.RemoveFromDeck 中的动画实现
    /// </summary>
    private static async Task PlayCardRemovalAnimation(CardModel card)
    {
        NCard nCard = null;
        Tween tween = null;

        try
        {
            // 检查是否是本地玩家
            if (!LocalContext.IsMine(card))
                return;

            // 创建卡牌预览节点
            nCard = NCard.Create(card);
            if (nCard == null)
                return;

            // 添加到全局 UI 的卡牌预览容器
            NRun.Instance?.GlobalUi?.CardPreviewContainer?.AddChildSafely(nCard);
            if (nCard.GetParent() == null)
            {
                // 如果无法添加到预览容器，直接销毁并返回
                nCard.QueueFree();
                return;
            }

            // 更新视觉状态
            nCard.UpdateVisuals(PileType.None, CardPreviewMode.Normal);

            // 创建动画 - 参考商店删牌动画
            tween = nCard.CreateTween();
            if (tween == null)
            {
                nCard.QueueFree();
                return;
            }

            // 动画第一阶段：从 0 缩放到正常大小
            tween.TweenProperty(nCard, "scale", Vector2.One * 1f, 0.25f)
                .From(Vector2.Zero)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);

            // 动画第二阶段：延迟后压扁并变黑（表示被移除）
            tween.TweenProperty(nCard, "scale:y", 0f, 0.3f)
                .SetDelay(1.5f);
            tween.Parallel().TweenProperty(nCard, "scale:x", 1.5f, 0.3f)
                .SetDelay(1.5f);
            tween.Parallel().TweenProperty(nCard, "modulate", Colors.Black, 0.2f)
                .SetDelay(1.5f);

            // 动画结束：销毁卡牌节点
            tween.TweenCallback(Callable.From(() =>
            {
                if (nCard != null && GodotObject.IsInstanceValid(nCard))
                {
                    nCard.QueueFree();
                }
            }));

            // 显式启动动画
            tween.Play();

            // 等待动画完成
            if (tween.IsValid())
            {
                await nCard.ToSignal(tween, Tween.SignalName.Finished);
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Error playing card removal animation: {ex}");
        }
        finally
        {
            // 确保节点被销毁
            if (nCard != null && GodotObject.IsInstanceValid(nCard))
            {
                nCard.QueueFree();
            }
            tween?.Dispose();
        }
    }
}

// NOTE: CardPileCmd.Add 返回 Task<CardPileAddResult>，Harmony Patch 需要特殊处理
// 暂时禁用，待找到正确的异步方法 patch 方式后再启用
/*
/// <summary>
/// 拦截卡牌进入弃牌堆 - 如果闪耀值已归零，则阻止进入弃牌堆
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add), typeof(CardModel), typeof(PileType), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool))]
public static class CardPileCmd_Add_PileType_Patch
{
    static bool Prefix(CardModel card, PileType newPileType, ref CardPileAddResult __result)
    {
        // 检查是否是闪耀卡牌且闪耀值已归零
        if (ShinePatch.ShouldRemoveCard(card))
        {
            // 只有当目标是弃牌堆/抽牌堆/消耗堆时才拦截
            // 如果是进入Play堆（卡牌正在打出），则允许
            if (newPileType == PileType.Discard || newPileType == PileType.Draw || newPileType == PileType.Exhaust)
            {
                MainFile.Logger.Info($"Intercepted card '{card.Title}' from entering {newPileType} (shine depleted)");

                // 返回成功但不实际添加（卡牌已被移除）
                __result = new CardPileAddResult
                {
                    success = true,
                    cardAdded = card,
                    oldPile = card.Pile
                };
                return false; // 阻止原方法执行
            }
        }

        return true; // 允许原方法执行
    }
}
*/

// NOTE: RemoveFromCombat 方法可能不存在或签名不匹配，暂时禁用
/*
/// <summary>
/// 拦截 Power 牌的 RemoveFromCombat - 如果闪耀值已归零，使用商店删除动画替代消耗动画
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.RemoveFromCombat), typeof(CardModel), typeof(bool), typeof(bool))]
public static class CardPileCmd_RemoveFromCombat_Patch
{
    static bool Prefix(CardModel card, bool isBeingPlayed, bool skipVisuals)
    {
        // 检查是否是闪耀卡牌且闪耀值已归零
        if (ShinePatch.ShouldRemoveCard(card))
        {
            // 对于 Power 牌且正在播放时，使用我们的自定义动画
            if (card.Type == CardType.Power && isBeingPlayed && !skipVisuals)
            {
                MainFile.Logger.Info($"Intercepted Power card '{card.Title}' RemoveFromCombat (shine depleted)");

                // 跳过原方法，避免消耗动画
                // 卡牌从当前牌堆移除由 ShinePatch.RemoveShinedCardWithAnimation 处理
                if (card.Pile != null)
                {
                    card.RemoveFromCurrentPile();
                }

                return false; // 阻止原方法执行
            }
        }

        return true; // 允许原方法执行
    }
}
*/
