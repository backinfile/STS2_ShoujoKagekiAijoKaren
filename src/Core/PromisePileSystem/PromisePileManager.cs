using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Localization;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;

/// <summary>
/// 约定牌堆管理器
///
/// 设计说明：
/// - 每个玩家拥有独立的约定牌堆（FIFO 链表）
/// - 使用 SpireField 将数据附加到 Player 对象
/// - 约定牌堆是"虚拟牌堆"，仅战斗中有效，不通过 PileType 枚举管理
/// - 战斗开始和结束时自动清空
/// </summary>
public static class PromisePileManager
{
    private static readonly SpireField<Player, LinkedList<CardModel>> _promisePile
        = new(() => new LinkedList<CardModel>());

    /// <summary>获取玩家的约定牌堆链表</summary>
    public static LinkedList<CardModel> GetPromisePile(Player player)
        => _promisePile.Get(player)!;

    /// <summary>检查卡牌是否在约定牌堆中</summary>
    public static bool IsInPromisePile(CardModel card)
    {
        if (card?.Owner == null) return false;
        return GetPromisePile(card.Owner).Contains(card);
    }

    /// <summary>
    /// 将卡牌放入约定牌堆（加入链表尾部）。
    /// 会从当前牌堆物理移出（RemoveFromCurrentPile），不触发 CardPileCmd 流程。
    /// </summary>
    public static void AddToPromisePile(CardModel card)
    {
        if (card?.Owner == null) return;

        var pile = GetPromisePile(card.Owner);
        if (pile.Contains(card))
        {
            MainFile.Logger.Warn($"[PromisePile] '{card.Title}' already in promise pile, skipping");
            return;
        }

        // 动画在 RemoveFromCurrentPile 之前执行（FindOnTable 依赖 Pile.Type）
        PromisePileAnimator.PlayAddAnimation(card);

        card.RemoveFromCurrentPile();
        pile.AddLast(card);

        MainFile.Logger.Info($"[PromisePile] '{card.Title}' → promise pile (count={pile.Count})");
        OnCardEntered?.Invoke(card);

        // 更新 Power
        _ = UpdatePowerAsync(card.Owner);
    }

    /// <summary>
    /// 从约定牌堆取出第一张牌（FIFO），并通过 CardPileCmd.Add 移入手牌顶部。
    /// 返回 null 表示约定牌堆为空。
    /// </summary>
    public static async Task<CardModel?> DrawFromPromisePileAsync(
        PlayerChoiceContext choiceContext, Player player)
    {
        var pile = GetPromisePile(player);
        if (pile.Count == 0) return null;

        var card = pile.First!.Value;
        pile.RemoveFirst();
        MainFile.Logger.Info($"[PromisePile] '{card.Title}' ← promise pile → hand (remaining={pile.Count})");
        OnCardLeft?.Invoke(card);

        await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top);

        // 更新 Power
        await UpdatePowerAsync(player);

        return card;
    }

    /// <summary>
    /// 清空玩家的约定牌堆，将所有卡牌从 CombatState 注销。
    /// 战斗结束时调用；战斗实例被注销后，DeckVersion 仍在 Player.Deck，下场战斗正常使用。
    /// </summary>
    public static void ClearPromisePile(Player player)
    {
        var pile = GetPromisePile(player);
        if (pile.Count == 0) return;

        int count = pile.Count;
        while (pile.Count > 0)
        {
            var card = pile.First!.Value;
            pile.RemoveFirst();
            card.RemoveFromCurrentPile();
            AccessTools.Method(typeof(CardModel), "RemoveFromState")?.Invoke(card, null);
            OnCardLeft?.Invoke(card);
        }

        MainFile.Logger.Info($"[PromisePile] Cleared {count} card(s) for player {player?.NetId}");
    }

    /// <summary>获取约定牌堆中的卡牌数量</summary>
    public static int GetCount(Player player)
        => GetPromisePile(player).Count;

    /// <summary>
    /// 从指定牌堆（弃牌堆或抽牌堆）让玩家选择最多 count 张牌放入约定牌堆。
    /// 若牌堆为空或实际可选数为 0 则直接返回。
    /// minSelect == maxSelect，选满 N 张后自动确认（无手动按钮）；1 张时单击即确认。
    /// </summary>
    public static async Task AddFromPileAsync(
        PlayerChoiceContext ctx, Player player, PileType pileType, int count, LocString prompt)
    {
        if (player == null) return;

        var cardPile = pileType.GetPile(player);
        // 拍快照：避免选牌过程中列表变动
        var cards = cardPile.Cards.ToList();
        if (cards.Count == 0) return;

        int selectCount = Math.Min(count, cards.Count);
        var prefs = new CardSelectorPrefs(prompt, selectCount, selectCount);

        var selected = await CardSelectCmd.FromSimpleGrid(ctx, cards, player, prefs);
        if (selected == null) return;

        foreach (var card in selected)
            AddToPromisePile(card);
    }

    /// <summary>
    /// 将约定牌堆中的所有牌依次弃置到弃牌堆（FIFO 顺序）。
    /// 约定牌堆为空时直接返回。
    /// </summary>
    public static async Task DiscardAllAsync(PlayerChoiceContext ctx, Player player)
    {
        if (player == null) return;

        var pile = GetPromisePile(player);
        if (pile.Count == 0) return;

        while (pile.Count > 0)
        {
            var card = pile.First!.Value;
            pile.RemoveFirst();
            MainFile.Logger.Info($"[PromisePile] '{card.Title}' ← promise pile → discard");
            OnCardLeft?.Invoke(card);
            await CardPileCmd.Add(card, PileType.Discard);
        }

        await UpdatePowerAsync(player);
    }

    /// <summary>
    /// 初始化玩家的 PromisePilePower（战斗开始时调用）。
    /// 仅对华恋角色添加 Power，初始数值为 0。
    /// </summary>
    public static async Task InitPowerAsync(Player player)
    {
        if (player?.Creature == null) return;
        if (player.Character is not Karen) return;

        await PowerCmd.Apply<KarenPromisePilePower>(player.Creature, 1, player.Creature, null);
        if (player.Creature.GetPower<KarenPromisePilePower>() is { } karenPower)
            karenPower.SetCount(0);
    }

    /// <summary>更新玩家的 PromisePilePower 数值为约定牌堆卡牌数，如不存在则自动创建。</summary>
    public static async Task UpdatePowerAsync(Player player)
    {
        if (player?.Creature == null) return;

        int count = GetCount(player);
        var creature = player.Creature;

        if (!creature.HasPower<KarenPromisePilePower>())
            await PowerCmd.Apply<KarenPromisePilePower>(creature, 1, creature, null);

        if (creature.GetPower<KarenPromisePilePower>() is { } karenPower)
        {
            karenPower.SetCount(count);
            var names = GetPromisePile(player)
                .Select(c => c.IsUpgraded ? c.Title + "+" : c.Title)
                .ToArray();
            karenPower.SetCardNames(names);
        }
    }

    /// <summary>卡牌进入约定牌堆事件</summary>
    public static event Action<CardModel>? OnCardEntered;

    /// <summary>卡牌离开约定牌堆事件</summary>
    public static event Action<CardModel>? OnCardLeft;
}
