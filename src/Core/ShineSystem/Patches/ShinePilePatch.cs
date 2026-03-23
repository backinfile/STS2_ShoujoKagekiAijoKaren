using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Shine.ShinePatches;

/// <summary>
/// 闪耀牌堆核心补丁
///
/// 闪耀耗尽流程分两步：
///   Step 1 - 动画结算：
///     - 能力牌：放行 RemoveFromCombat，让游戏自带的能力牌消退动画正常播放
///     - 其他牌：拦截目标牌堆，播放删牌动画，然后将打出的战斗实例从所有牌堆移除
///   Step 2 - 数据处理：
///     - 将该牌对应的牌组（DeckVersion）静默移除
///     - 将 DeckVersion 加入闪耀牌堆（无动画）
/// </summary>
public static class ShinePilePatch
{
    // ── 工具方法 ─────────────────────────────────────────────────────────────

    /// <summary>是否应进入闪耀耗尽流程（已初始化 Shine 且当前值==0）</summary>
    private static bool ShouldEnterShinePile(CardModel card)
    {
        if (!card.IsShineCard()) return false;
        return card.GetShineValue() == 0;
    }

    /// <summary>
    /// 决定进入闪耀牌堆的目标卡牌：优先使用 DeckVersion（牌组中的永久版本），
    /// 若 DeckVersion 为空或就是自身则回退到当前卡牌。
    /// </summary>
    private static CardModel GetShinePileTarget(CardModel card)
    {
        var deck = card.DeckVersion;
        return (deck != null && deck != card) ? deck : card;
    }

    /// <summary>
    /// 播放闪耀耗尽删牌动画，直接操作正在打出的战斗 NCard。
    /// 将其从 PlayContainer 移到顶层 CardPreviewContainer，再播放删除动画。
    /// 仅对本地玩家的牌播放；fire-and-forget，不阻塞调用方。
    /// 动画结束后由 Tween 回调负责 QueueFree，调用方不必再手动销毁节点。
    /// </summary>
    private static void PlayShineDepletionAnimation(CardModel card, NCard? combatCard)
    {
        if (!LocalContext.IsMine(card)) return;
        if (combatCard == null) return;

        // 将战斗 NCard 从 PlayContainer 移到顶层 CardPreviewContainer
        // Reparent 会保留全局位置，之后再 Tween 到屏幕中央
        var previewContainer = NRun.Instance?.GlobalUi?.CardPreviewContainer;
        if (previewContainer == null) return;
        combatCard.Reparent(previewContainer);

        // 根据快速模式调整各阶段时长，对齐游戏原生动画的缩放逻辑
        FastModeType fastMode = SaveManager.Instance.PrefsSave.FastMode;
        float showDelay = fastMode switch
        {
            FastModeType.Instant => 0.01f,
            FastModeType.Fast    => 0.4f,
            _                    => 1.5f
        };
        float destroyDuration = fastMode switch
        {
            FastModeType.Instant => 0.01f,
            FastModeType.Fast    => 0.15f,
            _                    => 0.3f
        };

        Tween tween = combatCard.CreateTween();
        // 消失（并行）：横向拉宽 + 纵向压扁 + 变黑
        tween.TweenProperty(combatCard, "scale:y", 0, destroyDuration).SetDelay(showDelay);
        tween.Parallel().TweenProperty(combatCard, "scale:x", 1.5f, destroyDuration).SetDelay(showDelay);
        tween.Parallel().TweenProperty(combatCard, "modulate", Colors.Black, destroyDuration * 0.67f).SetDelay(showDelay);
        tween.TweenCallback(Callable.From(combatCard.QueueFreeSafely));
    }

    // ── Patch：非能力牌 ──────────────────────────────────────────────────────

    /// <summary>
    /// 拦截 CardPileCmd.Add（卡牌归堆，主要用于非能力牌打出后进入弃牌堆）。
    ///
    /// 闪耀耗尽时：
    ///   Step 1 - 播放删牌动画；将战斗实例从所有牌堆移除。
    ///   Step 2 - 将 DeckVersion 静默移出牌组，加入闪耀牌堆。
    /// </summary>
    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add),
        typeof(CardModel), typeof(PileType), typeof(CardPilePosition),
        typeof(AbstractModel), typeof(bool))]
    public static class CardPileCmd_Add_Patch
    {
        static bool Prefix(CardModel card, PileType newPileType, ref Task<CardPileAddResult> __result)
        {
            if (!ShouldEnterShinePile(card)) return true;
            if (ShinePileManager.IsInShinePile(GetShinePileTarget(card)))
            {
                MainFile.Logger.Warn($"[ShinePilePatch] '{card.Title}' DeckVersion already in shine pile, skipping");
                return true;
            }

            MainFile.Logger.Info($"[ShinePilePatch] Non-power '{card.Title}' shine depleted, entering removal flow (was heading to {newPileType})");

            // ── Step 1：动画结算 ──────────────────────────────────────────
            // 在 RemoveFromCurrentPile 之前找战斗 NCard（FindOnTable 依赖 Pile.Type 定位）
            NCard? combatCardNode = NCard.FindOnTable(card);

            // 直接操作战斗 NCard 播放删除动画（移到顶层后 Tween 消失，Tween 回调负责 QueueFree）
            PlayShineDepletionAnimation(card, combatCardNode);

            // 将战斗实例从当前牌堆完全移除（NCard 已被 Reparent，不再受 Pile 管理）
            card.RemoveFromCurrentPile();
            // RemoveFromState 在游戏程序集内可能是 internal，使用反射调用
            AccessTools.Method(typeof(CardModel), "RemoveFromState")?.Invoke(card, null);

            // ── Step 2：数据处理 ──────────────────────────────────────────
            // 将 DeckVersion 静默移出牌组，加入闪耀牌堆
            var target = GetShinePileTarget(card);
            ShinePileManager.MoveToShinePile(target);

            MainFile.Logger.Info($"[ShinePilePatch] '{target.Title}' (DeckVersion) moved to shine pile");

            __result = CreateSuccessResult(card);
            return false;
        }

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

    // ── Patch：能力牌 ────────────────────────────────────────────────────────

    /// <summary>
    /// 拦截 CardPileCmd.RemoveFromCombat（能力牌从战斗中移除）。
    ///
    /// 策略：改为 Postfix，让原方法正常执行（播放游戏自带的能力牌消退动画），
    /// 完成后再进行 Step 2 数据处理：将 DeckVersion 加入闪耀牌堆。
    /// </summary>
    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.RemoveFromCombat), typeof(CardModel), typeof(bool))]
    public static class CardPileCmd_RemoveFromCombat_Patch
    {
        [HarmonyPostfix]
        static void Postfix(CardModel card, bool skipVisuals)
        {
            if (!ShouldEnterShinePile(card)) return;

            var target = GetShinePileTarget(card);
            if (ShinePileManager.IsInShinePile(target)) return;

            MainFile.Logger.Info($"[ShinePilePatch] Power card '{card.Title}' shine depleted after RemoveFromCombat");

            // ── Step 2：数据处理（Step 1 动画已由 RemoveFromCombat 自身完成）──
            ShinePileManager.MoveToShinePile(target);

            MainFile.Logger.Info($"[ShinePilePatch] '{target.Title}' (DeckVersion) moved to shine pile");
        }
    }
}


/// <summary>
/// 战斗结束后打印 Karen 玩家的闪耀牌堆内容（调试日志）。
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
        int unique = ShinePileManager.GetUniqueCardCount(__instance);

        if (total == 0)
        {
            MainFile.Logger.Info($"[ShinePile] 战斗结束 — 闪耀牌堆为空");
            return;
        }

        var cardList = string.Join(", ", pile.Select(c => $"{c.Title}({c.GetShineMaxValue()})"));
        MainFile.Logger.Info($"[ShinePile] 战斗结束 — 共 {total} 张（{unique} 种）: {cardList}");
    }
}
