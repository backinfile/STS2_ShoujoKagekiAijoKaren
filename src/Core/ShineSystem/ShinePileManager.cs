using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
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
using ShoujoKagekiAijoKaren.src.Core.SaveSystem;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        pile.Add(card);
        MainFile.Logger.Info($"[ShinePileManager] Card '{card.Title}' added to shine pile (shine={card.GetShineValue()})");

        // 触发卡牌上的闪耀耗尽扳机
        if (original is KarenBaseCardModel karenCard)
        {
            MainFile.Logger.Info($"[ShinePileManager] Triggered OnShineExhausted for '{card.Title}'");
            var inCombat = CombatManager.Instance?.IsInProgress == true && (combatState?.Enemies?.Any(e => e.IsAlive) ?? true);
            await karenCard.OnShineExhausted(ctx, inCombat, combatState);
        }

        // 最终将这个卡牌移出游戏
        original.RemoveFromState();
        card.RemoveFromState();
    }

    /// <summary>
    /// 添加卡牌到闪耀牌堆（内部方法，假设卡牌已正确处理移除和数据设置）
    /// </summary>
    /// <param name="card"></param>
    public static void AddToShinePileInternal(CardModel card)
    {
        var pile = GetShinePile(card.Owner!);
        if (!pile.Contains(card))
        {
            pile.Add(card);
            MainFile.Logger.Info($"[ShinePileManager] Card '{card.Title}' added to shine pile (internal)");
        }
    }

    /// <summary>
    /// 清空玩家的闪耀牌堆
    /// </summary>
    public static void ClearShinePileInternal(Player player)
    {
        var pile = GetShinePile(player);
        var cards = pile.ToList(); // 复制列表避免修改时遍历
        foreach (var card in cards)
        {
            pile.Remove(card);
            card.RemoveFromState();
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
    /// 获取闪耀牌堆中不同种类卡牌的数量（按 CardId 去重）
    /// </summary>
    public static int GetUniqueCardCount(Player player)
    {
        return GetShinePile(player).Select(c => c.Id.Entry).Distinct().Count();
    }


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

        // TODO
        //foreach (var entry in saveData)
        //{
        //    CardModel? card = null;

        //    // 优先：下标+ID 双重校验
        //    if (entry.Index >= 0 && entry.Index < deckList.Count
        //        && deckList[entry.Index].Id.Entry == entry.CardId)
        //    {
        //        card = deckList[entry.Index];
        //    }

        //    // 回退：按 CardId 找第一张未在闪耀牌堆的匹配牌
        //    if (card == null)
        //    {
        //        card = deckList.FirstOrDefault(c =>
        //            c.Id.Entry == entry.CardId && !IsInShinePile(c));
        //    }

        //    if (card == null)
        //    {
        //        MainFile.Logger.Warn($"[ShinePileManager] 未找到耗尽卡牌 {entry.CardId} (index={entry.Index})，跳过");
        //        continue;
        //    }

        //    card.SetShineMax(entry.ShineMax);
        //    card.SetShineCurrent(entry.ShineCurrent);
        //    AddToShinePileInternal(card);
        //    MainFile.Logger.Info($"[ShinePileManager] 恢复耗尽卡牌 {card.Id.Entry} (shine={entry.ShineCurrent}/{entry.ShineMax})");
        //}
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
