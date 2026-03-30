using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.addons.mega_text;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.GlobalMoveSystem.Patches;
using Godot;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;

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
    private static readonly SpireField<PlayerCombatState, CardPile> _promisePile
        = new(() => new CardPile(KarenCustomEnum.PromisePile));

    /// <summary>获取玩家的约定牌堆链表</summary>
    public static CardPile GetPromisePile(Player player)
        => _promisePile.Get(player.PlayerCombatState!)!;

    public static CardPile GetPromisePile(PlayerCombatState combatState)
        => _promisePile.Get(combatState)!;

    /// <summary>检查卡牌是否在约定牌堆中</summary>
    public static bool IsInPromisePile(CardModel card)
    {
        if (card?.Owner == null) return false;
        return GetPromisePile(card.Owner).Cards.Contains(card);
    }

    /// <summary>检查玩家是否处于虚空模式（PromisePilePower 存在且 IsVoidMode 为 true）</summary>
    public static bool IsVoidMode(Player player)
    {
        if (player?.Creature == null) return false;
        var power = player.Creature.GetPower<KarenPromisePilePower>();
        return power?.IsVoidMode == true;
    }

    /// <summary>触发玩家身上所有 KarenBasePower 的 OnCardAddedToPromisePile 扳机</summary>
    private static async Task TriggerPowerOnCardAdded(Player player, CardModel card)
    {
        if (player?.Creature == null) return;
        foreach (var power in player.Creature.Powers.OfType<KarenBasePower>())
        {
            await power.OnCardAddedToPromisePile(card);
        }
    }

    /// <summary>触发玩家身上所有 KarenBasePower 的 OnCardRemovedFromPromisePile 扳机</summary>
    private static async Task TriggerPowerOnCardRemoved(Player player, CardModel card)
    {
        if (player?.Creature == null) return;
        foreach (var power in player.Creature.Powers.OfType<KarenBasePower>())
        {
            await power.OnCardRemovedFromPromisePile(card);
        }
    }

    /// <summary>
    /// 将卡牌放入约定牌堆（加入链表尾部）。
    /// 会从当前牌堆物理移出（RemoveFromCurrentPile），不触发 CardPileCmd 流程。
    /// </summary>
    public static async Task AddToPromisePile(CardModel card)
    {
        if (card?.Owner == null) return;

        var pile = GetPromisePile(card.Owner);
        if (pile.Cards.Contains(card))
        {
            MainFile.Logger.Warn($"[PromisePile] '{card.Title}' already in promise pile, skipping");
            return;
        }

        // oldPile 必须在 PlayAddAnimation 之前记录：
        // PlayAddAnimation（同步方法）依赖 card.Pile 定位 NCard；RemoveFromCurrentPile 会将 card.Pile 置为 null
        PileType oldPile = card.Pile?.Type ?? PileType.None;

        // 动画在 RemoveFromCurrentPile 之前执行（FindOnTable 依赖 Pile.Type）
        PromisePileAnimator.PlayAddAnimation(card);

        card.RemoveFromCurrentPile();
        pile.AddInternal(card);

        // pile.AddLast 后 IsInPromisePile 为 true，GlobalMovePatch 能正确推断 newPile = PromisePile
        await Hook.AfterCardChangedPiles(card.Owner.RunState, card.CombatState, card, oldPile, null);
        MainFile.Logger.Info($"[PromisePile] Added '{card.Title}' to promise pile (was in {oldPile})");

        if (card is KarenBaseCardModel karenCard)
            await karenCard.OnAddedToPromisePile();

        // 触发 KarenBasePower 扳机
        await TriggerPowerOnCardAdded(card.Owner, card);

        // 更新 Power
        await UpdatePowerAsync(card.Owner);
    }

    /// <summary>
    /// 从约定牌堆取出第一张牌（FIFO），并通过 CardPileCmd.Add 移入手牌顶部。
    /// 返回 null 表示约定牌堆为空。
    /// </summary>
    public static async Task<CardModel?> DrawFromPromisePileAsync(
        PlayerChoiceContext choiceContext, Player player)
    {
        var pile = GetPromisePile(player);
        if (pile.Cards.Count == 0) return null;

        var card = pile.Cards.First();
        pile.RemoveInternal(card);
        if (card is KarenBaseCardModel karenCard)
            await karenCard.OnRemovedFromPromisePile();

        // 触发 KarenBasePower 扳机
        await TriggerPowerOnCardRemoved(player, card);

        await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top);
        await Hook.AfterCardChangedPiles(card.Owner.RunState, card.CombatState, card, KarenCustomEnum.PromisePile, null);

        // 更新 Power
        await UpdatePowerAsync(player);

        if (pile.IsEmpty)
        {
            await PromisePileTriggers.TriggerPromisePileEmpty(player);
        }

        return card;
    }

    /// <summary>
    /// 清空玩家的约定牌堆，将所有卡牌从 CombatState 注销。
    /// 不触发任何扳机，仅清理用。战斗结束时调用；
    /// 此时游戏状态正在销毁，不应触发订阅者逻辑。
    /// 战斗实例被注销后，DeckVersion 仍在 Player.Deck，下场战斗正常使用。
    /// </summary>
    public static void ClearPromisePileInternal(Player player)
    {
        var pile = GetPromisePile(player);
        if (pile.Cards.Count == 0) return;

        int count = pile.Cards.Count;
        while (pile.Cards.Count > 0)
        {
            var card = pile.Cards.First();
            card.RemoveFromCurrentPile();
            card.RemoveFromState();

            // 不触发扳机
            //if (card is KarenBaseCardModel karenCard)
            //    _ = karenCard.OnRemovedFromPromisePile();
        }
    }

    /// <summary>获取约定牌堆中的卡牌数量</summary>
    public static int GetCount(Player player)
    {
        if (player == null) return 0;
        return GetPromisePile(player).Cards.Count;
    }

    /// <summary>
    /// 从指定牌堆（弃牌堆或抽牌堆）让玩家选择最多 count 张牌放入约定牌堆。
    /// 若牌堆为空或实际可选数为 0 则直接返回。
    /// minSelect == maxSelect，选满 N 张后自动确认（无手动按钮）；1 张时单击即确认。
    /// 当 pileType 为 Hand 时使用 FromHand 进行手牌选择, 此时要求传入source。
    /// </summary>
    public static async Task AddFromPileAsync(
        PlayerChoiceContext ctx, Player player, PileType pileType, int count, LocString prompt, AbstractModel? source = null)
    {
        if (player == null) return;

        var cardPile = pileType.GetPile(player);
        // 拍快照：避免选牌过程中列表变动
        var cards = cardPile.Cards.ToList();
        if (cards.Count == 0) return;

        int selectCount = Math.Min(count, cards.Count);
        var prefs = new CardSelectorPrefs(prompt, selectCount, selectCount);

        IEnumerable<CardModel> selected;
        if (pileType == PileType.Hand)
        {
            // 手牌使用 FromHand，显示在手牌区域
            selected = await CardSelectCmd.FromHand(ctx, player, prefs, null, source!);
        }
        else
        {
            selected = await CardSelectCmd.FromSimpleGrid(ctx, cards, player, prefs);
        }
        if (selected == null) return;

        foreach (var card in selected)
            await AddToPromisePile(card);
    }

    /// <summary>
    /// 将约定牌堆中的所有牌依次弃置到弃牌堆（FIFO 顺序）。
    /// 约定牌堆为空时直接返回。
    /// </summary>
    public static async Task DiscardAllAsync(PlayerChoiceContext ctx, Player player)
    {
        if (player == null) return;

        var pile = GetPromisePile(player);
        if (pile.Cards.Count == 0) return;

        while (pile.Cards.Count > 0)
        {
            var card = pile.Cards.First();
            pile.RemoveInternal(card);
            if (card is KarenBaseCardModel karenCard)
                await karenCard.OnRemovedFromPromisePile();

            // 触发 KarenBasePower 扳机
            await TriggerPowerOnCardRemoved(player, card);

            await CardPileCmd.Add(card, PileType.Discard);
            await Hook.AfterCardChangedPiles(card.Owner.RunState, card.CombatState, card, KarenCustomEnum.PromisePile, null);
        }

        await UpdatePowerAsync(player);

        await PromisePileTriggers.TriggerPromisePileEmpty(player);
    }

    /// <summary>
    /// 初始化玩家的 PromisePilePower（战斗开始时调用）。
    /// 仅对华恋角色添加 Power，初始数值为 0。
    /// </summary>
    public static async Task InitPowerAsync(Player player)
    {
        //if (player?.Creature == null) return;
        //if (player.Character is not Karen) return;

        //await PowerCmd.Apply<KarenPromisePilePower>(player.Creature, 1, player.Creature, null);
        //if (player.Creature.GetPower<KarenPromisePilePower>() is { } karenPower)
        //    karenPower.SetCount(0);

        MainFile.Logger.Info($"[PromisePile] Initialized PromisePilePower for player {player.Character.Title}");

        // 立即更新一次
        await UpdatePowerAsync(player);
    }

    /// <summary>
    /// 更新玩家的 PromisePilePower 数值为约定牌堆卡牌数，如不存在则自动创建。
    /// </summary>
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
            var names = GetPromisePile(player).Cards
                .Select(c => c.IsUpgraded ? c.Title + "+" : c.Title)
                .ToArray();
            karenPower.SetCardNames(names);
        }

        // 检查无限强化状态
        await ApplyInfiniteReinforcementIfNeededAsync(player);
    }

    /// <summary>
    /// 检查并应用无限强化效果。如果开启了无限强化，将约定牌堆填充为10张续演。
    /// </summary>
    private static async Task ApplyInfiniteReinforcementIfNeededAsync(Player player)
    {
        if (player?.Creature == null) return;
        if (player.Creature.GetPower<KarenPromisePilePower>() is not { IsInfiniteReinforcement: true }) return;

        await RefillWithContinueAsync(player);
    }


    /// <summary>
    /// 将约定牌堆清空并重新填充为10张续演。
    /// </summary>
    private static async Task RefillWithContinueAsync(Player player)
    {
        if (player?.PlayerCombatState == null) return;

        var pile = GetPromisePile(player);

        var maxCount = CardPile.maxCardsInHand;

        // 先把牌库中的非续演牌移除掉
        foreach (var card in pile.Cards.Where(c => c is not KarenContinue).ToList())
        {
            card.RemoveFromCurrentPile();
            card.RemoveFromState();
            MainFile.Logger.Info($"[PromisePile] Removed '{card.Title}' from promise pile during refill");
        }

        // 然后用续演补齐到10张
        for (int i = 0; i < maxCount - pile.Cards.Count; i++)
        {
            var card = player.RunState.CreateCard<KarenContinue>(player);
            pile.AddInternal(card);
            MainFile.Logger.Info($"[PromisePile] Added '{card.Title}' to promise pile during refill");
        }
    }

    /// <summary>打开约定牌堆查看界面（快照模式，使用原生 NCardPileScreen）</summary>
    public static void ShowScreen(Player player)
    {
        var snapshot = new CardPile(PileType.None);
        foreach (var card in GetPromisePile(player).Cards)
            snapshot.AddInternal(card);

        var screen = NCardPileScreen.ShowScreen(snapshot, System.Array.Empty<string>());

        // 设置标题（注意：必须在设置 Text 后再设置 Visible，因为 NCardPileScreen._Ready 中 default 分支会设置 Visible = false）
        var bottomLabel = screen.GetNode<MegaRichTextLabel>("%BottomLabel");
        if (bottomLabel != null)
        {
            bottomLabel.Text = "[center]" + new LocString("gameplay_ui", "KAREN_PROMISE_PILE_INFO").GetFormattedText();
            bottomLabel.Visible = true;
        }
    }


}
