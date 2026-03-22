using Godot;
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
    private const float FastDuration   = 0.15f;
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
        var nCard = NCard.FindOnTable(card);
        if (nCard == null) return;

        var playerNode = GetCreatureNode(card.Owner);
        if (playerNode == null) return;

        var targetPos = playerNode.VfxSpawnPosition;
        float duration = GetDuration();

        var globalUi = NRun.Instance?.GlobalUi;
        if (globalUi == null) return;

        // 记录手牌中的位置，然后将原始 NCard 从当前父节点移除。
        // 这会将 NCard 从 _selectedHandCardContainer 中物理移除，
        // 防止 OnSelectModeSourceFinished 后续将预览卡牌加回手牌。
        // 参考：CardPileCmd.MoveCardNodeToNewPileBeforeTween 的做法
        var startPos = nCard.GlobalPosition;
        var parent = nCard.GetParent();
        parent?.RemoveChild(nCard);

        // 关键：如果父节点是 NSelectedHandCardHolder，需要将它从容器中移除
        // 这样 OnSelectModeSourceFinished 遍历 _selectedHandCardContainer.Holders 时就不会处理这个 holder
        if (parent is NSelectedHandCardHolder holder)
        {
            holder.GetParent()?.RemoveChild(holder);
            holder.QueueFreeSafely();
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
            FastModeType.Fast    => FastDuration,
            FastModeType.Instant => InstantDuration,
            _                    => NormalDuration,
        };
    }
}
