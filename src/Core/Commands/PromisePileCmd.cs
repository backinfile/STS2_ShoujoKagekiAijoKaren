using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Commands;

/// <summary>Character actions use one logical pile; mode and movement lifecycle belong to the manager.</summary>
public static class PromisePileCmd
{
    private static bool IsVoidMode(Player player) => PromisePileManager.IsVoidMode(player);
    private static bool IsInfiniteMode(Player player) => PromisePileManager.IsInMode(player, PromisePileMode.InfiniteReinforcement);
    public static async Task Add(Player player, CardModel card)
        => await CardPileCmd.Add(card, PromisePileManager.EffectivePile(player));
    public static async Task Add(Player player, IEnumerable<CardModel> cards, bool skipVisuals = false)
        => await CardPileCmd.Add(cards, PromisePileManager.EffectivePile(player), skipVisuals: skipVisuals);
    public static async Task AddToken<T>(Player player, ICombatState combatState, int cnt = 1) where T : CardModel
    {
        var cards = Enumerable.Range(0, Math.Max(0, cnt)).Select(_ => combatState.CreateCard<T>(player)).ToArray();
        await CardPileCmd.AddGeneratedCardsToCombat(cards, KarenCustomEnum.PromisePile, player);
    }
    public static Task<CardModel?> Draw(PlayerChoiceContext context, Player player) => IsVoidMode(player)
        ? CardPileCmd.Draw(context, player) : PromisePileManager.DrawFromPromisePileAsync(context, player);
    public static async Task Draw(PlayerChoiceContext context, Player player, int count)
    {
        for (int i = 0; i < count; i++) if (await Draw(context, player) == null) break;
    }
    public static async Task DrawToFullHand(PlayerChoiceContext context, Player player)
    {
        while (player.PlayerCombatState != null && !CombatManager.Instance.IsOverOrEnding && !PlayerUtils.IsHandFull(player))
            if (await Draw(context, player) == null) break;
    }
    public static async Task SelectedToHand(PlayerChoiceContext context, Player player, int count,
        Action<IEnumerable<CardModel>>? beforeMove = null)
    {
        if (IsVoidMode(player))
        {
            await CardPileCmdEx.SelectFromDrawPileToHand(context, player, count, beforeMove);
            return;
        }
        if (IsInfiniteMode(player))
        {
            for (int i = 0; i < count; i++)
            {
                var next = PromisePileManager.EffectivePile(player).Cards.FirstOrDefault();
                if (next != null) beforeMove?.Invoke([next]);
                if (await Draw(context, player) == null) break;
            }
            return;
        }
        var selected = await PromisePileManager.SelectCards(context, player, PromisePileManager.EffectivePile(player),
            count, Tips.SelectFromPromisePileToHand);
        beforeMove?.Invoke(selected);
        foreach (var card in selected) await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top);
    }
    public static async Task AddFromDiscard(PlayerChoiceContext context, Player player, int count)
        => await PromisePileManager.AddFromPileAsync(context, player, PileType.Discard, count, Tips.PromisePileSelectFromDiscard);
    public static async Task AddFromDraw(PlayerChoiceContext context, Player player, int count)
    {
        if (!IsVoidMode(player))
            await PromisePileManager.AddFromPileAsync(context, player, PileType.Draw, count, Tips.PromisePileSelectFromDraw);
    }
    public static Task<IReadOnlyList<CardModel>> AddFromHand(PlayerChoiceContext context, Player player, int count,
        AbstractModel source, bool optional = false)
        => PromisePileManager.AddFromPileAsync(context, player, PileType.Hand, count, Tips.PromisePileSelectFromHand, source, optional);
    public static async Task MoveAllToDrawPile(Player player, CardPilePosition position = CardPilePosition.Bottom)
    {
        if (!IsVoidMode(player)) await CardPileCmd.Add(PromisePileManager.EffectivePile(player).Cards.ToArray(), PileType.Draw, position);
    }
    public static async Task DiscardAll(PlayerChoiceContext context, Player player)
    {
        if (IsInfiniteMode(player)) return;
        if (IsVoidMode(player)) await CardCmd.Discard(context, PromisePileManager.EffectivePile(player).Cards.ToArray());
        else await PromisePileManager.DiscardAllAsync(context, player);
    }
    public static async Task DiscardCards(PlayerChoiceContext context, Player player, List<CardModel> cards)
    {
        if (cards.Count > 0) await CardCmd.Discard(context, cards.ToArray());
    }
    public static async Task SwitchHand(PlayerChoiceContext context, Player player, AbstractModel? source = null)
    {
        var previous = PromisePileManager.EffectivePile(player).Cards.ToArray();
        var hand = PileType.Hand.GetPile(player).Cards.ToArray();
        if (hand.Length > 0) await Add(player, hand);
        if (previous.Length > 0) await CardPileCmd.Add(previous, PileType.Hand, CardPilePosition.Top, source, false);
        // The existing swap rule also fires when both sides were populated and no physical empty transition occurred.
        if (previous.Length > 0 && hand.Length > 0) await PromisePileHooks.TriggerPromisePileEmpty(player);
    }
    internal static async Task AutoPlayFromPromisePile(PlayerChoiceContext context, Player player, int count)
    {
        if (CombatManager.Instance.IsOverOrEnding) return;
        if (IsVoidMode(player))
        {
            await CardPileCmd.AutoPlayFromDrawPile(context, player, count, CardPilePosition.Top, forceExhaust: false);
            return;
        }
        var cards = new List<CardModel>();
        try
        {
            for (int i = 0; i < count; i++)
            {
                var card = PromisePileManager.EffectivePile(player).Cards.FirstOrDefault();
                if (card == null) break;
                // Movement hooks can fail after the card has already entered Play.
                cards.Add(card);
                var result = await CardPileCmd.Add(card, PileType.Play);
                if (!result.success) break;
                cards[^1] = result.cardAdded;
            }
            foreach (var card in cards)
                if (card.Pile?.Type == PileType.Play) await CardCmd.AutoPlay(context, card, null);
        }
        finally { await RestoreUnplayed(player, cards); }
    }
    public static async Task SelectAndAutoPlayFromPromisePile(PlayerChoiceContext context, Player player)
    {
        if (CombatManager.Instance.IsOverOrEnding) return;
        var cards = await PromisePileManager.SelectCards(context, player, PromisePileManager.EffectivePile(player), 1, Tips.SelectFromPromisePileToPlay);
        if (cards.FirstOrDefault() is not { } card) return;
        CardModel staged = card;
        try
        {
            var result = await CardPileCmd.Add(card, PileType.Play);
            if (!result.success) return;
            staged = result.cardAdded;
            await CardCmd.AutoPlay(context, staged, null);
        }
        finally { await RestoreUnplayed(player, [staged]); }
    }
    private static async Task RestoreUnplayed(Player player, IEnumerable<CardModel> cards)
    {
        if (player.PlayerCombatState == null || CombatManager.Instance.IsOverOrEnding) return;
        var unplayed = cards.Where(card => card.Pile?.Type == PileType.Play).ToArray();
        if (unplayed.Length > 0) await Add(player, unplayed);
    }
    internal static Task EnterMode(Player player, PromisePileMode mode) => PromisePileManager.EnterMode(player, mode);
}
