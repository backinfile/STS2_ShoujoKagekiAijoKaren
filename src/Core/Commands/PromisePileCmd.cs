using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Commands;

/// <summary>
/// 约定牌堆命令类 - 卡牌效果的统一入口。
/// 对齐游戏原生 CardPileCmd / CreatureCmd 的静态命令类风格。
/// 支持虚空模式：交互重定向到抽牌堆。
/// 
/// 设计笔记：
///     对特殊模式（Void，Infinite)的处理都在Cmd层进行处理转发, PromisePileManager不会转发。
/// </summary>
public static class PromisePileCmd
{

    // ===== Void Mode Detection =====
    private static bool IsVoidMode(Player player) => PromisePileManager.IsVoidMode(player);

    // ==== 无限mode ====

    private static bool IsInfiniteMode(Player player) => PromisePileManager.IsInMode(player, PromisePileMode.InfiniteReinforcement);

    /// <summary>
    /// 将指定卡牌放入约定牌堆（物理从当前牌堆移出，加入队列尾部）。
    /// Void 模式下改为放入抽牌堆顶部。
    /// 调用方应确保卡牌当前在手牌中。
    /// 注意：这个方法不会检测是否需要耗尽进入耗尽牌堆
    /// </summary>
    public static async Task Add(Player player, CardModel card)
    {
        //if (card?.Owner == null)
        //{
        //    MainFile.Logger.Error("Attempted to add a card with null owner to the promise pile. Operation aborted.");
        //    return;
        //}

        if (IsVoidMode(player))
        {
            // Void 模式：放入抽牌堆底部
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Bottom);
            return;
        }

        await PromisePileManager.AddToPromisePile(player, card);
    }

    /// <summary>
    /// 将一些牌放入约定牌堆
    /// </summary>
    public static async Task Add(Player player, List<CardModel> cards)
    {
        if (IsVoidMode(player))
        {
            // Void 模式
            await CardPileCmd.Add(cards, PileType.Draw, CardPilePosition.Bottom, null, false);
            return;
        }

        foreach (var card in cards)
        {
            await PromisePileManager.AddToPromisePile(player, card);
        }
    }




    public static async Task AddToken<T>(Player player, CombatState combatState, int cnt = 1) where T : CardModel
    {
        if (CombatManager.Instance.IsOverOrEnding) return;

        MainFile.Logger.Info($"Adding {cnt} token(s) of type {typeof(T).Name} to promise pile for player {player.Creature.Name}");
        for (int i = 0; i < cnt; i++)
        {
            var card = combatState.CreateCard<T>(player);
            CombatManager.Instance.History.CardGenerated(combatState, card, true);
            await Add(player, card);
            await Hook.AfterCardGeneratedForCombat(combatState, card, true);
        }
    }

    /// <summary>
    /// 从约定牌堆取出第一张牌（FIFO），移到手牌顶部。
    /// Void 模式下改为从抽牌堆顶部抽1张。
    /// 返回 null 表示约定牌堆为空（或抽牌堆为空）。
    /// </summary>
    public static async Task<CardModel?> Draw(PlayerChoiceContext choiceContext, Player player)
    {
        if (IsVoidMode(player))
        {
            // Void 模式：从抽牌堆抽1张，并触发 Power 扳机
            MainFile.Logger.Info("Player is in Void mode, drawing from draw pile instead of promise pile.");
            return await CardPileCmd.Draw(choiceContext, player);
        }

        return await PromisePileManager.DrawFromPromisePileAsync(choiceContext, player);
    }

    /// <summary>
    /// 批量从约定牌堆取出 count 张牌，最多取到堆空为止。
    /// Void 模式下改为从抽牌堆批量抽取。
    /// </summary>
    public static async Task Draw(PlayerChoiceContext choiceContext, Player player, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (await Draw(choiceContext, player) == null)
                break;
        }
    }

    /// <summary>
    /// 从约定牌堆中抽牌直到手牌满
    /// </summary>
    /// <param name="choiceContext"></param>
    /// <param name="player"></param>
    /// <returns></returns>
    public static async Task DrawToFullHand(PlayerChoiceContext choiceContext, Player player)
    {
        while (true)
        {
            if (PlayerUtils.IsHandFull(player)) break;
            var drawn = await Draw(choiceContext, player);
            if (drawn == null) break; // 抽不到更多的牌了
        }
    }

    /// <summary>
    /// 从约定牌堆中选择count 张牌加入手牌。
    /// Void 模式下改为从抽牌堆中选择加入手牌。
    /// </summary>
    public static async Task SelectedToHand(PlayerChoiceContext ctx, Player player, int count)
    {
        if (IsVoidMode(player))
        {
            await CardPileCmdEx.SelectFromDrawPileToHand(ctx, player, count);
            return;
        }

        // 无限模式直接改为抽牌，不需要选择
        if (IsInfiniteMode(player))
        {
            await Draw(ctx, player, count);
            return;
        }

        var pile = PromisePileManager.GetPromisePile(player);
        var cards = pile.Cards.ToList();
        if (cards.Count == 0) return;

        int selectCount = System.Math.Min(count, cards.Count);
        var prompt = Tips.SelectFromPromisePileToHand;
        var prefs = new CardSelectorPrefs(prompt, selectCount, selectCount);

        var selected = await CardSelectCmd.FromSimpleGrid(ctx, cards, player, prefs);
        if (selected == null) return;

        foreach (var card in selected)
        {
            //pile.RemoveInternal(card); 这里不要移除，要给后续牌堆移动提供oldPile
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top, null, false);
        }

        await PromisePileManager.UpdatePowerAsync(player);
    }


    /// <summary>
    /// 从弃牌堆让玩家选择最多 count 张牌放入约定牌堆。
    /// Void 模式下改为放入抽牌堆顶部。
    /// </summary>
    public static async Task AddFromDiscard(PlayerChoiceContext ctx, Player player, int count)
    {
        var prompt = Tips.PromisePileSelectFromDiscard;
        if (IsVoidMode(player))
        {
            // Void 模式：从弃牌堆选牌放入抽牌堆顶部
            await AddFromPileToDrawPile(ctx, player, PileType.Discard, count, prompt);
            return;
        }

        await PromisePileManager.AddFromPileAsync(ctx, player, PileType.Discard, count, prompt);
    }

    /// <summary>
    /// 从抽牌堆让玩家选择最多 count 张牌放入约定牌堆。
    /// Void 模式下无效果（直接返回）。
    /// </summary>
    public static async Task AddFromDraw(PlayerChoiceContext ctx, Player player, int count)
    {
        if (IsVoidMode(player))
        {
            // Void 模式：无效果
            return;
        }

        await PromisePileManager.AddFromPileAsync(ctx, player, PileType.Draw, count, Tips.PromisePileSelectFromDraw);
    }

    /// <summary>
    /// 从手牌让玩家选择最多 count 张牌放入约定牌堆。
    /// Void 模式下改为放入抽牌堆顶部。
    /// </summary>
    public static async Task AddFromHand(PlayerChoiceContext ctx, Player player, int count, AbstractModel source)
    {
        var prompt = Tips.PromisePileSelectFromHand;
        if (IsVoidMode(player))
        {
            // Void 模式：从手牌选牌放入抽牌堆顶部
            await AddFromPileToDrawPile(ctx, player, PileType.Hand, count, prompt, source);
            return;
        }

        await PromisePileManager.AddFromPileAsync(ctx, player, PileType.Hand, count, prompt, source);
    }

    /// <summary>
    /// 将约定牌堆中所有牌移动到抽牌堆。
    /// Void 模式下无效果。
    /// </summary>
    public static async Task MoveAllToDrawPile(Player player, CardPilePosition position = CardPilePosition.Bottom)
    {
        if (IsVoidMode(player))
        {
            return;
        }

        var pile = KarenCustomEnum.PromisePile.GetPile(player);
        var cards = pile.Cards.ToList();
        await CardPileCmd.Add(cards, PileType.Draw, position, null, false);
        await PromisePileManager.UpdatePowerAsync(player);
    }

    /// <summary>
    /// 将约定牌堆中所有牌弃置到弃牌堆（FIFO 顺序）。
    /// Void 模式下改为弃置所有抽牌堆。
    /// </summary>
    public static async Task DiscardAll(PlayerChoiceContext ctx, Player player)
    {
        // 无限模式下，可以不弃置
        if (PromisePileManager.IsInMode(player, PromisePileMode.InfiniteReinforcement))
        {
            MainFile.Logger.Info("Player is in Infinite Reinforcement mode, skipping discard.");
            return;
        }


        if (IsVoidMode(player))
        {
            // Void 模式：弃置抽牌堆所有牌
            await CardCmd.Discard(ctx, PileType.Draw.GetPile(player).Cards.ToList());
            return;
        }

        await PromisePileManager.DiscardAllAsync(ctx, player);
    }

    /// <summary>
    /// 将约定牌堆中指定的若干张牌弃置到弃牌堆。
    /// Void 模式下改为弃置抽牌堆中对应的牌。
    /// </summary>
    public static async Task DiscardCards(PlayerChoiceContext ctx, Player player, List<CardModel> cards)
    {
        if (cards == null || cards.Count == 0) return;
        await CardCmd.Discard(ctx, cards.ToList());
    }

    // ===== Void Mode Helper Methods =====

    /// <summary>
    /// Void 模式专用：从指定牌堆选牌放入抽牌堆
    /// 当 pileType 为 Hand 时使用 FromHand 进行手牌选择
    /// </summary>
    private static async Task AddFromPileToDrawPile(
        PlayerChoiceContext ctx, Player player, PileType pileType, int count, LocString prompt, AbstractModel? source = null)
    {
        var cardPile = pileType.GetPile(player);
        var cards = cardPile.Cards.ToList();
        if (cards.Count == 0) return;

        int selectCount = System.Math.Min(count, cards.Count);
        var prefs = new CardSelectorPrefs(prompt, selectCount, selectCount);

        IEnumerable<CardModel> selected;
        if (pileType == PileType.Hand)
        {
            // 手牌使用 FromHand，显示在手牌区域
            selected = await CardSelectCmd.FromHand(ctx, player, prefs, _ => true, source!);
        }
        else
        {
            selected = await CardSelectCmd.FromSimpleGrid(ctx, cards, player, prefs);
        }
        if (selected == null) return;
        await CardPileCmd.Add(selected, PileType.Draw, CardPilePosition.Bottom, null, false);
    }


    /// <summary>
    /// 交换手牌和约定牌堆的所有卡牌。
    /// 流程：1.暂存所有手牌 2.从约定牌堆抽满手牌 3.弃置约定牌堆剩余 4.原手牌放入约定牌堆
    /// Void模式下：使用抽牌堆替代约定牌堆，逻辑相同
    /// </summary>
    public static async Task SwitchHand(PlayerChoiceContext ctx, Player player, AbstractModel? source = null)
    {
        // 先记录约定牌堆中所有卡牌
        bool inVoid = IsVoidMode(player);
        var promiseCards = inVoid
            ? PileType.Draw.GetPile(player).Cards.ToList()
            : KarenCustomEnum.PromisePile.GetPile(player).Cards.ToList();

        // 将所有手牌放入约定牌堆
        List<CardModel> handCards = PileType.Hand.GetPile(player).Cards.ToList();
        if (handCards.Any()) await Add(player, handCards);

        // 然后将约定牌堆中的牌放回手牌
        if (promiseCards.Any()) await CardPileCmd.Add(promiseCards, PileType.Hand, CardPilePosition.Top, source, false);

        // 最后更新计数
        await PromisePileManager.UpdatePowerAsync(player);

        // 触发一次空牌堆扳机（如果约定牌堆之前有牌的话）
        // 手牌若为空，会在HookPatch中触发, 这里不用触发了
        if (promiseCards.Any() && handCards.Any())
        {
            await PromisePileHooks.TriggerPromisePileEmpty(player);
        }
    }

    internal static async Task AutoPlayFromPromisePile(PlayerChoiceContext choiceContext, Player player, int count)
    {
        if (CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        // 空虚状态下直接打出牌库顶牌
        if (IsVoidMode(player))
        {
            await CardPileCmd.AutoPlayFromDrawPile(choiceContext, player, count, CardPilePosition.Top, forceExhaust: false);
            return;
        }

        List<CardModel> cards = new(count);
        CardPile promisePile = KarenCustomEnum.PromisePile.GetPile(player);
        if (promisePile.IsEmpty) return;

        for (int i = 0; i < count; i++)
        {
            if (promisePile.IsEmpty) break;
            CardModel cardModel = promisePile.Cards[0];
            cards.Add(cardModel);
            await CardPileCmd.Add(cardModel, PileType.Play);
        }

        foreach (CardModel item in cards)
        {
            await CardCmd.AutoPlay(choiceContext, item, null);
        }
        await PromisePileManager.UpdatePowerAsync(player);
    }

    internal static async Task EnterMode(Player player, PromisePileMode infiniteReinforcement)
    {
        await PromisePileManager.EnterMode(player, infiniteReinforcement);
    }
}
