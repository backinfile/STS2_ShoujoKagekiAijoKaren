using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.SaveSystem;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;

namespace ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

/// <summary>
/// 闪耀牌堆（耗尽牌堆） - 管理每张卡牌的独立"闪耀牌堆"
/// 收集耗尽的闪耀牌，这里面的牌不会再次利用，只计数用。
///
/// 设计说明：
/// - 每个玩家拥有独立的闪耀牌堆
/// - 使用 SpireField 将数据附加到 Player 对象
/// - 闪耀牌堆是"虚拟牌堆"，不通过 PileType 枚举管理
/// - 通过 Harmony Patch 拦截 CardPileCmd.Add 实现卡牌重定向
/// </summary>
public static class ShinePileManager
{
    /// <summary>
    /// SpireField 存储每个玩家的闪耀耗尽牌堆（卡牌列表）
    /// </summary>
    private static readonly SpireField<Player, List<CardModel>> _disposedShineCardPile = new(() => []);

    /// <summary>
    /// SpireField 存储本局游戏中已耗尽的不同闪耀牌ID（用于 CarryingGuilt 等效果）
    /// 以 CardId.Entry 为唯一标识，自动去重
    /// </summary>
    private static readonly SpireField<Player, int> _disposedShineCardUniqueCounts = new(() => 0);

    /// <summary>
    /// 获取玩家的闪耀牌堆
    /// </summary>
    public static List<CardModel> GetShinePile(Player player)
    {
        return _disposedShineCardPile.Get(player)!;
    }


    /// <summary>
    /// 检查卡牌是否在闪耀牌堆中
    /// </summary>
    public static bool IsInShinePile(CardModel card)
    {
        if (card?.Owner == null) return false;
        return GetShinePile(card.Owner).Contains(card);
    }

    /// <summary>
    /// 移动卡牌到闪耀牌堆（异步版本，用于战斗中触发扳机）
    /// </summary>
    /// <param name="original">原始卡牌</param>
    /// <param name="ctx">PlayerChoiceContext，由调用方传入</param>
    public static async Task MoveToShinePile(CardModel original, PlayerChoiceContext ctx)
    {
        var combatState = original.CombatState;
        var card = original.DeckVersion ?? original;
        if (card?.Owner == null) return;

        var pile = GetShinePile(card.Owner);

        // 避免重复添加
        if (pile.Contains(card))
        {
            MainFile.Logger.Warn($"[ShinePileManager] Card '{card.Title}' already in shine pile");
            return;
        }

        // 添加到闪耀牌堆
        pile.Add(card.CloneSafeForDeck());
        card.RestoreShineToMax();
        MainFile.Logger.Info($"[ShinePileManager] Card '{card.Title}' added to shine pile (shineMax={card.GetShineMaxValue()})");

        // 记录本局游戏中已耗尽的闪耀牌（用于 CarryingGuilt 等效果）
        UpdateShineCardDisposedCount(card.Owner);

        // 触发卡牌上的闪耀耗尽扳机
        if (original is KarenBaseCardModel karenCard)
        {
            MainFile.Logger.Info($"[ShinePileManager] Triggered OnShineExhausted for '{card.Title}'");
            var inCombat = CombatManager.Instance?.IsInProgress == true && (combatState?.Enemies?.Any(e => e.IsAlive) ?? true);
            await karenCard.OnShineExhausted(ctx, inCombat, combatState);
        }

        // 最终将这个卡牌移出游戏 TODO 等待后续实现耗尽牌堆
        original.RemoveFromState();
        card.RemoveFromState();
    }

    /// <summary>
    /// 添加卡牌到闪耀牌堆（内部方法，假设卡牌已正确处理移除和数据设置）
    /// </summary>
    /// <param name="card"></param>
    public static void AddToShinePileInternal(Player player, CardModel card)
    {
        var pile = GetShinePile(player);
        pile.Add(card);
        MainFile.Logger.Info($"[ShinePileManager] Card '{card.Title}' added to shine pile (internal)");
    }

    /// <summary>
    /// 清空玩家的闪耀牌堆
    /// </summary>
    public static void ClearShinePileInternal(Player player)
    {
        var pile = GetShinePile(player);
        foreach (var card in pile.ToList())
        {
            pile.Remove(card);
            //card.RemoveFromState();
            _ = CardPileCmd.RemoveFromCombat(card); // 确保从战斗中移除
        }
        MainFile.Logger.Info($"[ShinePileManager] Cleared shine pile for player {player?.NetId}");
    }

    /// <summary>
    /// 获取闪耀牌堆中的卡牌数量
    /// </summary>
    public static int GetShinePileCount(Player player)
    {
        return GetShinePile(player).Count;
    }

    /// <summary>
    /// 获取本局游戏中已耗尽的不同闪耀牌数量（按 CardId 去重）
    /// 用于 CarryingGuilt 等卡牌效果
    /// </summary>
    public static int GetDisposedShineCardUniqueCount(Player player)
    {
        if (player == null) return 0;
        return _disposedShineCardUniqueCounts.Get(player);
    }

    /// <summary>
    /// 记录一张闪耀牌被耗尽
    /// 自动去重，同一类型的卡牌只记录一次
    /// </summary>
    public static void UpdateShineCardDisposedCount(Player player)
    {
        if (player == null) return;
        _disposedShineCardUniqueCounts.Set(player, _disposedShineCardPile.Get(player)!.Select(c => c.Id.Entry).Distinct().Count());
        MainFile.Logger.Info($"[ShinePileManager] Updated disposed shine card count for player {player.NetId}: total={GetShinePileCount(player)}, unique={GetDisposedShineCardUniqueCount(player)}");
    }

    public static HoverTip GetShinePileUniqueCountHoverTip(Player player)
    {
        var title = Tips.DisposedPileUniqueCountTitle;
        var description0 = Tips.DisposedPileUniqueCountDesc0;
        var description1 = Tips.DisposedPileUniqueCountDesc1;
        var description = String.Format("{0}{2}{1}", description0, description1, GetDisposedShineCardUniqueCount(player));
        return new HoverTip(title, description);
    }


    /// <summary>播放闪耀耗尽删牌动画</summary>
    public static void PlayShineDepletionAnimation(CardModel card, NCard? combatCard)
    {
        if (!LocalContext.IsMine(card) || combatCard == null) return;

        var previewContainer = NRun.Instance?.GlobalUi?.CardPreviewContainer;
        if (previewContainer == null) return;

        combatCard.Reparent(previewContainer);

        FastModeType fastMode = SaveManager.Instance.PrefsSave.FastMode;
        float showDelay = fastMode switch
        {
            FastModeType.Instant => 0.01f,
            FastModeType.Fast => 0.4f,
            _ => 1.5f
        };
        float destroyDuration = fastMode switch
        {
            FastModeType.Instant => 0.01f,
            FastModeType.Fast => 0.15f,
            _ => 0.3f
        };

        Tween tween = combatCard.CreateTween();
        tween.TweenProperty(combatCard, "scale:y", 0, destroyDuration).SetDelay(showDelay);
        tween.Parallel().TweenProperty(combatCard, "scale:x", 1.5f, destroyDuration).SetDelay(showDelay);
        tween.Parallel().TweenProperty(combatCard, "modulate", Colors.Black, destroyDuration * 0.67f).SetDelay(showDelay);
        tween.TweenCallback(Callable.From(combatCard.QueueFreeSafely));
    }
}
