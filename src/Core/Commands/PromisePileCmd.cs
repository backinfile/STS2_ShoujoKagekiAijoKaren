using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Commands;

/// <summary>
/// 约定牌堆命令类 - 卡牌效果的统一入口。
/// 对齐游戏原生 CardPileCmd / CreatureCmd 的静态命令类风格。
/// 支持虚空模式：交互重定向到抽牌堆。
/// </summary>
public static class PromisePileCmd
{
    /// <summary>
    /// 触发玩家身上所有 KarenBasePower 的 OnCardAddedToPromisePile 扳机
    /// </summary>
    private static async Task TriggerPowerOnCardAdded(Player player, CardModel card)
    {
        if (player?.Creature == null) return;
        foreach (var power in player.Creature.Powers.OfType<KarenBasePower>())
        {
            await power.OnCardAddedToPromisePile(card);
        }
    }

    /// <summary>
    /// 触发玩家身上所有 KarenBasePower 的 OnCardRemovedFromPromisePile 扳机
    /// </summary>
    private static async Task TriggerPowerOnCardRemoved(Player player, CardModel card)
    {
        if (player?.Creature == null) return;
        foreach (var power in player.Creature.Powers.OfType<KarenBasePower>())
        {
            await power.OnCardRemovedFromPromisePile(card);
        }
    }

    // ===== Void Mode Detection =====
    private static bool IsVoidMode(Player player) => PromisePileManager.IsVoidMode(player);

    /// <summary>
    /// 将指定卡牌放入约定牌堆（物理从当前牌堆移出，加入队列尾部）。
    /// Void 模式下改为放入抽牌堆顶部。
    /// 调用方应确保卡牌当前在手牌中。
    /// </summary>
    public static async Task Add(CardModel card)
    {
        if (card?.Owner == null) return;

        if (IsVoidMode(card.Owner))
        {
            // Void 模式：放入抽牌堆顶部，并触发 Power 扳机
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top, null, false);
            await TriggerPowerOnCardAdded(card.Owner, card);
            return;
        }

        await PromisePileManager.AddToPromisePile(card);
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
            if (card != null)
            {
                await TriggerPowerOnCardRemoved(player, card);
            }
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
        if (IsVoidMode(player))
        {
            // Void 模式：从抽牌堆批量抽取，每张都触发 Power 扳机
            for (int i = 0; i < count; i++)
            {
                var card = await CardPileCmd.Draw(choiceContext, player);
                if (card == null) break;
                await TriggerPowerOnCardRemoved(player, card);
            }
            return;
        }

        for (int i = 0; i < count; i++)
        {
            if (await PromisePileManager.DrawFromPromisePileAsync(choiceContext, player) == null)
                break;
        }
    }

    /// <summary>
    /// 从弃牌堆让玩家选择最多 count 张牌放入约定牌堆。
    /// Void 模式下改为放入抽牌堆顶部。
    /// prompt 通常传调用方卡牌的 SelectionScreenPrompt。
    /// </summary>
    public static async Task AddFromDiscard(
        PlayerChoiceContext ctx, Player player, int count, LocString prompt)
    {
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
    /// prompt 通常传调用方卡牌的 SelectionScreenPrompt。
    /// </summary>
    public static async Task AddFromDraw(
        PlayerChoiceContext ctx, Player player, int count, LocString prompt)
    {
        if (IsVoidMode(player))
        {
            // Void 模式：无效果
            return;
        }

        await PromisePileManager.AddFromPileAsync(ctx, player, PileType.Draw, count, prompt);
    }

    /// <summary>
    /// 将约定牌堆中所有牌弃置到弃牌堆（FIFO 顺序）。
    /// Void 模式下改为弃置所有抽牌堆。
    /// </summary>
    public static async Task DiscardAll(PlayerChoiceContext ctx, Player player)
    {
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
    /// </summary>
    private static async Task AddFromPileToDrawPile(
        PlayerChoiceContext ctx, Player player, PileType pileType, int count, LocString prompt)
    {
        var cardPile = pileType.GetPile(player);
        var cards = cardPile.Cards.ToList();
        if (cards.Count == 0) return;

        int selectCount = System.Math.Min(count, cards.Count);
        var prefs = new CardSelectorPrefs(prompt, selectCount, selectCount);

        var selected = await CardSelectCmd.FromSimpleGrid(ctx, cards, player, prefs);
        if (selected == null) return;

        foreach (var card in selected)
        {
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top, null, false);
            await TriggerPowerOnCardAdded(player, card);
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
            await TriggerPowerOnCardRemoved(player, card);
        }
    }
}
