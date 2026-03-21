using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.SaveSystem;
using ShoujoKagekiAijoKaren.src.Models.Characters;
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
        return _shinePile.Get(player)!;
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
    /// 获取闪耀牌堆中不同种类卡牌的数量（按 CardId 去重）
    /// </summary>
    public static int GetUniqueCardCount(Player player)
    {
        return GetShinePile(player).Select(c => c.Id.Entry).Distinct().Count();
    }

    /// <summary>
    /// 卡牌进入闪耀牌堆事件
    /// </summary>
    public static event System.Action<CardModel>? OnCardEnteredShinePile;

    /// <summary>
    /// 卡牌离开闪耀牌堆事件
    /// </summary>
    public static event System.Action<CardModel>? OnCardLeftShinePile;

    // ─────────────────────────────────────────────────────────────────────
    // 存档支持
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 收集单个玩家的闪耀牌堆存档数据。
    /// ShinePile 中的卡牌可能仍在 Deck.Cards（未被物理移出），用 IndexOf 定位；
    /// 不在 Deck.Cards 时 Index = -1，仅依赖 CardId 恢复。
    /// </summary>
    public static List<ShineSaveData> CollectShinePileData(Player player)
    {
        var shinePile = GetShinePile(player);
        if (shinePile.Count == 0) return new List<ShineSaveData>();

        var result = new List<ShineSaveData>();
        var deckList = player.Deck.Cards.ToList();

        foreach (var card in shinePile)
        {
            int index = deckList.IndexOf(card);
            result.Add(new ShineSaveData
            {
                CardId = card.Id.Entry,
                Index = index,
                ShineCurrent = card.GetShineValue(),
                ShineMax = card.GetShineMaxValue()
            });
            MainFile.Logger.Info($"[ShinePileManager] 收集耗尽卡牌 {card.Id.Entry} (deckIndex={index}, shine={card.GetShineValue()}/{card.GetShineMaxValue()})");
        }
        return result;
    }

    /// <summary>
    /// 遍历所有玩家，收集 Karen 玩家的闪耀牌堆存档数据。
    /// </summary>
    public static Dictionary<int, List<ShineSaveData>> CollectAllPlayersShinePileData(IReadOnlyList<Player> players)
    {
        var result = new Dictionary<int, List<ShineSaveData>>();
        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            if (p.Character is not Karen) continue;
            var pileData = CollectShinePileData(p);
            if (pileData.Count > 0)
                result[i] = pileData;
        }
        return result;
    }

    /// <summary>
    /// 恢复所有玩家的闪耀牌堆数据。
    /// </summary>
    public static void RestoreAllPlayersShinePileData(IReadOnlyList<Player> players, KarenRunSaveData data)
    {
        if (data.PlayerShinePileData == null || data.PlayerShinePileData.Count == 0) return;

        int totalRestored = 0;
        foreach (var (playerIdx, pileList) in data.PlayerShinePileData)
        {
            if (playerIdx < 0 || playerIdx >= players.Count) continue;
            var p = players[playerIdx];
            if (p.Character is not Karen) continue;
            RestoreShinePileData(p, pileList);
            totalRestored += pileList.Count;
        }
        MainFile.Logger.Info($"[ShinePileManager] 恢复 {data.PlayerShinePileData.Count} 名玩家共 {totalRestored} 张耗尽卡牌");
    }

    /// <summary>
    /// 将存档中的闪耀牌堆数据恢复到指定玩家：从 Deck.Cards 找到对应卡牌，
    /// 恢复 Shine 值后调用 AddToShinePile 将其移入闪耀牌堆。
    /// </summary>
    private static void RestoreShinePileData(Player player, List<ShineSaveData> saveData)
    {
        if (saveData == null || saveData.Count == 0) return;

        var deckList = player.Deck.Cards.ToList();

        foreach (var entry in saveData)
        {
            CardModel? card = null;

            // 优先：下标+ID 双重校验
            if (entry.Index >= 0 && entry.Index < deckList.Count
                && deckList[entry.Index].Id.Entry == entry.CardId)
            {
                card = deckList[entry.Index];
            }

            // 回退：按 CardId 找第一张未在闪耀牌堆的匹配牌
            if (card == null)
            {
                card = deckList.FirstOrDefault(c =>
                    c.Id.Entry == entry.CardId && !IsInShinePile(c));
            }

            if (card == null)
            {
                MainFile.Logger.Warn($"[ShinePileManager] 未找到耗尽卡牌 {entry.CardId} (index={entry.Index})，跳过");
                continue;
            }

            card.SetShineMax(entry.ShineMax);
            card.SetShineCurrent(entry.ShineCurrent);
            AddToShinePile(card);
            MainFile.Logger.Info($"[ShinePileManager] 恢复耗尽卡牌 {card.Id.Entry} (shine={entry.ShineCurrent}/{entry.ShineMax})");
        }
    }
}
