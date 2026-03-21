using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

/// <summary>
/// 闪耀牌堆管理器 - 管理每张卡牌的独立"闪耀牌堆"
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
    /// SpireField 存储每个玩家的闪耀牌堆（卡牌列表）
    /// </summary>
    private static readonly SpireField<Player, List<CardModel>> _shinePile = new(() => new List<CardModel>());

    /// <summary>
    /// 获取玩家的闪耀牌堆
    /// </summary>
    public static List<CardModel> GetShinePile(Player player)
    {
        return _shinePile.Get(player);
    }

    /// <summary>
    /// 获取卡牌当前所在的闪耀牌堆（如果存在）
    /// </summary>
    public static List<CardModel> GetShinePileForCard(CardModel card)
    {
        if (card?.Owner == null) return null;
        var pile = GetShinePile(card.Owner);
        return pile.Contains(card) ? pile : null;
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
    /// 添加卡牌到闪耀牌堆
    /// </summary>
    public static void AddToShinePile(CardModel card)
    {
        if (card?.Owner == null) return;

        var pile = GetShinePile(card.Owner);

        // 避免重复添加
        if (pile.Contains(card))
        {
            MainFile.Logger.Warn($"[ShinePileManager] Card '{card.Title}' already in shine pile");
            return;
        }

        // 从当前牌堆移除（如果不在手牌/打出区，手动移除）
        if (card.Pile != null && card.Pile.Type != PileType.Hand && card.Pile.Type != PileType.Play)
        {
            card.RemoveFromCurrentPile();
        }

        // 添加到闪耀牌堆
        pile.Add(card);
        MainFile.Logger.Info($"[ShinePileManager] Card '{card.Title}' added to shine pile (shine={card.GetShineValue()})");

        // 触发进入闪耀牌堆事件（供其他系统监听）
        OnCardEnteredShinePile?.Invoke(card);
    }

    /// <summary>
    /// 从闪耀牌堆移除卡牌
    /// </summary>
    public static bool RemoveFromShinePile(CardModel card)
    {
        if (card?.Owner == null) return false;

        var pile = GetShinePile(card.Owner);
        if (pile.Remove(card))
        {
            MainFile.Logger.Info($"[ShinePileManager] Card '{card.Title}' removed from shine pile");
            OnCardLeftShinePile?.Invoke(card);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 清空玩家的闪耀牌堆
    /// </summary>
    public static void ClearShinePile(Player player)
    {
        var pile = GetShinePile(player);
        var cards = pile.ToList(); // 复制列表避免修改时遍历

        foreach (var card in cards)
        {
            RemoveFromShinePile(card);
        }

        MainFile.Logger.Info($"[ShinePileManager] Cleared shine pile for player {player?.NetId}");
    }

    /// <summary>
    /// 将闪耀牌堆中的所有卡牌移回卡组
    /// </summary>
    public static void ReturnAllToDeck(Player player)
    {
        var pile = GetShinePile(player);
        var cards = pile.ToList();

        foreach (var card in cards)
        {
            RemoveFromShinePile(card);
            // 卡牌会自动回到卡组（通过游戏机制）
        }

        MainFile.Logger.Info($"[ShinePileManager] Returned {cards.Count} cards to deck for player {player?.NetId}");
    }

    /// <summary>
    /// 获取闪耀牌堆中的卡牌数量
    /// </summary>
    public static int GetShinePileCount(Player player)
    {
        return GetShinePile(player).Count;
    }

    /// <summary>
    /// 卡牌进入闪耀牌堆事件
    /// </summary>
    public static event System.Action<CardModel> OnCardEnteredShinePile;

    /// <summary>
    /// 卡牌离开闪耀牌堆事件
    /// </summary>
    public static event System.Action<CardModel> OnCardLeftShinePile;
}
