using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Shine.ShinePatches;

/// <summary>
/// 闪耀牌堆核心补丁
///
/// 统一拦截 CardModel.MoveToResultPileWithoutPlaying，这是所有卡牌打出后的唯一出口。
/// Shine 耗尽时拦截处理，传入 PlayerChoiceContext 供 OnShineExhausted 使用。
/// </summary>
public static class ShinePilePatch
{
    /// <summary>是否应进入闪耀耗尽流程（已初始化 Shine 且当前值==0）</summary>
    private static bool ShouldEnterShinePile(CardModel card)
    {
        if (!card.IsShineCard()) return false;
        return card.GetShineValue() == 0;
    }

    /// <summary>播放闪耀耗尽删牌动画</summary>
    private static void PlayShineDepletionAnimation(CardModel card, NCard? combatCard)
    {
        if (!LocalContext.IsMine(card)) return;
        if (combatCard == null) return;

        var previewContainer = NRun.Instance?.GlobalUi?.CardPreviewContainer;
        if (previewContainer == null) return;
        combatCard.Reparent(previewContainer);

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
        tween.TweenProperty(combatCard, "scale:y", 0, destroyDuration).SetDelay(showDelay);
        tween.Parallel().TweenProperty(combatCard, "scale:x", 1.5f, destroyDuration).SetDelay(showDelay);
        tween.Parallel().TweenProperty(combatCard, "modulate", Colors.Black, destroyDuration * 0.67f).SetDelay(showDelay);
        tween.TweenCallback(Callable.From(combatCard.QueueFreeSafely));
    }

    /// <summary>
    /// 拦截 CardModel.MoveToResultPileWithoutPlaying，处理 Shine 耗尽逻辑
    /// </summary>
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.MoveToResultPileWithoutPlaying))]
    public static class CardModel_MoveToResultPileWithoutPlaying_Patch
    {
        static bool Prefix(CardModel __instance, PlayerChoiceContext choiceContext, ref Task __result)
        {
            // 检查 Shine 耗尽条件
            if (!ShouldEnterShinePile(__instance))
                return true; // 不拦截，执行原方法

            var pile = __instance.Pile;
            if (pile == null || pile.Type != PileType.Play)
                return true; // 不拦截，执行原方法

            // 拦截并处理 Shine 耗尽
            __result = HandleShineDepletionAsync(__instance, choiceContext);
            return false;
        }

        static async Task HandleShineDepletionAsync(CardModel card, PlayerChoiceContext ctx)
        {
            // IsDupe 直接移除（不进入 Shine Pile）
            if (card.IsDupe)
            {
                await CardPileCmd.RemoveFromCombat(card);
                return;
            }

            MainFile.Logger.Info($"[ShinePilePatch] '{card.Title}' shine depleted, entering removal flow");

            // 在 RemoveFromCurrentPile 之前找战斗 NCard
            NCard? combatCardNode = NCard.FindOnTable(card);

            // 播放删除动画
            PlayShineDepletionAnimation(card, combatCardNode);

            // 将战斗实例从当前牌堆完全移除
            card.RemoveFromCurrentPile();
            // RemoveFromState 在游戏程序集内可能是 internal，使用反射调用
            AccessTools.Method(typeof(CardModel), "RemoveFromState")?.Invoke(card, null);

            // 将 DeckVersion 移入闪耀牌堆，传入 ctx
            var target = card.DeckVersion ?? card;
            await ShinePileManager.MoveToShinePile(target, ctx);

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
