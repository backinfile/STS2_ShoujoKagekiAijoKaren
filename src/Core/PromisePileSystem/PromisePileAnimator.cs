using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;

/// <summary>
/// 约定牌堆动画
/// - Add 动画：卡牌从手牌位置飞向玩家角色中心并缩小消失（在 RemoveFromCurrentPile 之前调用）
/// - Draw 动画：由 CardPileCmd.Add 的内置动画（scale 0→1 + 飞向手牌同时进行）完成
/// </summary>
public static class PromisePileAnimator
{
    private const float NormalDuration = 0.40f;
    private const float FastDuration = 0.15f;
    private const float InstantDuration = 0.01f;

    // ─── Add 动画 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 放入约定牌堆动画：临时副本从手牌位置飞向玩家角色中心并缩小消失。
    /// 必须在 card.RemoveFromCurrentPile() 之前调用（FindOnTable 依赖 Pile.Type）。
    /// 使用副本而非 Reparent 原始 NCard，避免手牌容器刷新时留下空位。
    /// fire-and-forget，不阻塞逻辑。
    /// </summary>
    public static void PlayAddAnimation(CardModel card)
    {
        if (NGame.Instance == null) return;
        if (NCombatRoom.Instance == null) return;

        var nCard = NCard.FindOnTable(card);
        if (nCard == null)
        {
            nCard = NCard.Create(card)!;
            NCombatRoom.Instance.Ui.AddChildSafely(nCard);
            nCard.UpdateVisuals(PileType.None, CardPreviewMode.Normal);

            // 将新创建的卡牌放到屏幕中心
            Vector2 screenSize = NGame.Instance.GetViewportRect().Size;
            nCard.Position = new Vector2(
                screenSize.X * 0.5f - nCard.Size.X * 0.5f,
                screenSize.Y * 0.5f - nCard.Size.Y * 0.5f
            );
            //nCard = AccessTools.Method(typeof(CardPileCmd), "CreateCardNodeAndUpdateVisuals").Invoke(null, [card, PileType.None, true]) as NCard;
        }

        var playerNode = GetCreatureNode(card.Owner);
        if (playerNode == null) return;

        var targetPos = playerNode.VfxSpawnPosition;
        float duration = GetDuration();

        var globalUi = NRun.Instance?.GlobalUi;
        if (globalUi == null) return;

        var startPos = nCard.GlobalPosition;
        NPlayerHand hand = NCombatRoom.Instance.Ui.Hand;
        NCardPlayQueue playQueue = NCombatRoom.Instance.Ui.PlayQueue;
        Control playContainer = NCombatRoom.Instance.Ui.PlayContainer;
        if (playQueue.IsAncestorOf(nCard))
        {
            playQueue.RemoveCardFromQueueForExecution(card);
        }

        // 从父节点中移除
        if (hand.IsAncestorOf(nCard))
        {
            hand.Remove(card);
        }
        else
        {
            nCard.GetParent()?.RemoveChildSafely(nCard);
        }

        // 创建临时副本做飞行动画
        var animCard = NCard.Create(card);
        if (animCard == null) return;

        globalUi.AddChild(animCard);
        animCard.GlobalPosition = startPos;

        var tween = animCard.CreateTween();
        tween.TweenProperty(animCard, "global_position", targetPos, duration);
        tween.Parallel().TweenProperty(animCard, "scale", Vector2.Zero, duration);
        tween.TweenCallback(Callable.From(animCard.QueueFreeSafely));

        // 原始 NCard 不再需要，安全销毁
        nCard.QueueFreeSafely();
    }

    // ─── 辅助方法 ─────────────────────────────────────────────────────────────

    private static NCreature? GetCreatureNode(Player? player)
    {
        if (player?.Creature == null) return null;
        return NCombatRoom.Instance?.GetCreatureNode(player.Creature);
    }

    private static float GetDuration()
    {
        return SaveManager.Instance?.PrefsSave?.FastMode switch
        {
            FastModeType.Fast => FastDuration,
            FastModeType.Instant => InstantDuration,
            _ => NormalDuration,
        };
    }
}
