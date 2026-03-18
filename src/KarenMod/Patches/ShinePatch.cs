using Godot;
using HarmonyLib;
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
using ShoujoKagekiAijoKaren.src.KarenMod.DynamicVars;

namespace ShoujoKagekiAijoKaren.src.KarenMod.Patches;

/// <summary>
/// Harmony补丁：统一处理 Shine 关键字逻辑
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
public static class ShinePatch
{
    static void Postfix(CardModel __instance, PlayerChoiceContext choiceContext)
    {
        // 查找 Shine 变量
        var shineVar = GetShineVar(__instance);
        if (shineVar == null) return;

        // 延迟执行，等待卡牌动画和状态更新完成
        _ = Task.Run(async () =>
        {
            try
            {
                // 等待卡牌进入弃牌堆
                await Task.Delay(500);

                // 获取当前 Shine 值
                int currentValue = (int)shineVar.BaseValue;

                // 减少 Shine 值
                currentValue--;
                shineVar.BaseValue = currentValue;

                MainFile.Logger.Info($"Card '{__instance.Title}' shine decreased to {currentValue}");

                // 如果 Shine 归零，从游戏中移除卡牌
                if (currentValue <= 0)
                {
                    await RemoveShinedCardWithAnimation(__instance);
                }
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error($"Error in shine processing: {ex}");
            }
        });
    }

    /// <summary>
    /// 获取卡牌上的 KarenShine 变量
    /// </summary>
    private static KarenShineVar? GetShineVar(CardModel card)
    {
        // 通过 Values 遍历查找 KarenShine 变量
        foreach (var dynamicVar in card.DynamicVars.Values)
        {
            if (dynamicVar is KarenShineVar shineVar)
            {
                return shineVar;
            }
        }
        return null;
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
        try
        {
            // 检查是否是本地玩家
            if (!LocalContext.IsMine(card))
                return;

            // 创建卡牌预览节点
            NCard nCard = NCard.Create(card);
            if (nCard == null)
                return;

            // 添加到全局 UI 的卡牌预览容器
            NRun.Instance?.GlobalUi?.CardPreviewContainer?.AddChildSafely(nCard);
            if (nCard.GetParent() == null)
            {
                // 如果无法添加到预览容器，直接销毁并返回
                nCard.QueueFreeSafely();
                return;
            }

            // 更新视觉状态
            nCard.UpdateVisuals(PileType.None, CardPreviewMode.Normal);

            // 创建动画 - 参考商店删牌动画
            Tween tween = nCard.CreateTween();
            if (tween == null)
            {
                nCard.QueueFreeSafely();
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
            tween.TweenCallback(Callable.From(nCard.QueueFreeSafely));

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
    }
}

