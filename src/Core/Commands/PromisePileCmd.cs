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

    /// <summary>
    /// 将指定卡牌放入约定牌堆（物理从当前牌堆移出，加入队列尾部）。
    /// Void 模式下改为放入抽牌堆顶部。
    /// 调用方应确保卡牌当前在手牌中。
    /// 注意：这个方法不会检测是否需要耗尽进入耗尽牌堆
    /// </summary>
    public static async Task Add(CardModel card)
    {
        if (card?.Owner == null)
        {
            MainFile.Logger.Error("Attempted to add a card with null owner to the promise pile. Operation aborted.");
            return;
        }

        if (IsVoidMode(card.Owner))
        {
            // Void 模式：放入抽牌堆顶部，并触发 Power 扳机
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top, null, false);
            return;
        }

        await PromisePileManager.AddToPromisePile(card);
    }

    public static async Task AddToken<T>(Player player, CombatState combatState, int cnt = 1) where T : CardModel
    {
        if (CombatManager.Instance.IsOverOrEnding) return;

        MainFile.Logger.Info($"Adding {cnt} token(s) of type {typeof(T).Name} to promise pile for player {player.Creature.Name}");
        for (int i = 0; i < cnt; i++)
        {
            var card = combatState.CreateCard<T>(player);
            CombatManager.Instance.History.CardGenerated(combatState, card, true);
            await Add(card);
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
            var card = await CardPileCmd.Draw(choiceContext, player);
            return card;
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
            if (await PromisePileManager.DrawFromPromisePileAsync(choiceContext, player) == null)
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

        var pile = PromisePileManager.GetPromisePile(player);
        var cards = pile.Cards.ToList();
        if (cards.Count == 0) return;

        int selectCount = System.Math.Min(count, cards.Count);
        var prompt = Tips.PromisePileSelectDraw;
        var prefs = new CardSelectorPrefs(prompt, selectCount, selectCount);

        var selected = await CardSelectCmd.FromSimpleGrid(ctx, cards, player, prefs);
        if (selected == null) return;

        foreach (var card in selected)
        {
            //pile.RemoveInternal(card); 这里不要移除，要给后续牌堆移动提供oldPile
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top, null, false);
        }

        await PromisePileManager.UpdatePowerAsync(player);

        if (pile.IsEmpty)
        {
            await PromisePileHooks.TriggerPromisePileEmpty(player);
        }
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
            // Void 模式：弃置所有抽牌堆
            await DiscardAllDrawPile(ctx, player);
            return;
        }

        await PromisePileManager.DiscardAllAsync(ctx, player);
    }

    // ===== Void Mode Helper Methods =====

    /// <summary>
    /// Void 模式专用：从指定牌堆选牌放入抽牌堆顶部
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

        foreach (var card in selected)
        {
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top, null, false);
        }
    }

    /// <summary>
    /// Void 模式专用：弃置所有抽牌堆
    /// </summary>
    private static async Task DiscardAllDrawPile(PlayerChoiceContext ctx, Player player)
    {
        var drawPile = PileType.Draw.GetPile(player);
        var cards = drawPile.Cards.ToList();

        foreach (var card in cards)
        {
            await CardCmd.Discard(ctx, card);
        }
    }

    /// <summary>
    /// 交换手牌和约定牌堆的所有卡牌。
    /// 流程：1.暂存所有手牌 2.从约定牌堆抽满手牌 3.弃置约定牌堆剩余 4.原手牌放入约定牌堆
    /// Void模式下：使用抽牌堆替代约定牌堆，逻辑相同
    /// </summary>
    public static async Task SwitchHand(PlayerChoiceContext ctx, Player player)
    {
        var handPile = PileType.Hand.GetPile(player);
        var handCards = handPile.Cards.ToList();


        // 步骤1：暂存手牌（移到抽牌堆顶部但不触发Power扳机，最后统一触发）
        var handSave = handCards.ToList(); // 复制列表，避免修改原列表导致枚举问题
        handPile.Clear();

        // 步骤2：从约定牌堆抽满手牌
        await PromisePileCmd.DrawToFullHand(ctx, player);

        // 步骤3：弃置约定牌堆剩余
        await PromisePileCmd.DiscardAll(ctx, player);

        // 步骤4：原手牌放入约定牌堆
        foreach (var card in handCards)
        {
            await PromisePileCmd.Add(card);
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

        // 触发约定牌堆变空扳机
        if (promisePile.IsEmpty)
        {
            await PromisePileHooks.TriggerPromisePileEmpty(player);
        }

        foreach (CardModel item in cards)
        {
            await CardCmd.AutoPlay(choiceContext, item, null);
        }

        await PromisePileManager.UpdatePowerAsync(player);
    }
}
