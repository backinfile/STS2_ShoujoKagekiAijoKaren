using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token.options;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Commands;

/// <summary>
/// 唤醒之泪药水的Action：从任意牌堆中选择卡牌放入手牌，直到手牌达到上限。
/// </summary>
public static class AwakePotionAction
{
    private static readonly LocString PileSelectionTitle = new("potions", "KAREN_AWAKE_POTION.pileSelectionPrompt");

    public static async Task Execute(PlayerChoiceContext choiceContext, Player player)
    {
        while (GetHandSpace(player) > 0)
        {
            var options = GetAvailableOptions(player);
            if (options.Count == 0)
            {
                return;
            }

            var combatState = player.Creature.CombatState;
            if (combatState == null)
            {
                return;
            }

            var optionCards = CardUtils.CreateTokens(player, combatState, options);
            var selectedOption = await CardSelectCmdEx.FromChooseACardScreen(choiceContext, optionCards, player, canSkip: false, PileSelectionTitle);
            var movedCount = selectedOption switch
            {
                KarenAwakePotionDrawPileOption => await SelectFromDrawPileToHand(choiceContext, player),
                KarenAwakePotionPromisePileOption => await SelectFromPromisePileToHand(choiceContext, player),
                KarenAwakePotionDiscardPileOption => await SelectFromDiscardPileToHand(choiceContext, player),
                _ => 0
            };

            var cardsInCombat = optionCards.Where(card => card.Pile != null).ToList();
            if (cardsInCombat.Count > 0)
            {
                await CardPileCmd.RemoveFromCombat(cardsInCombat);
            }

            if (movedCount == 0)
            {
                return;
            }
        }
    }

    public static Task<int> SelectFromDrawPileToHand(PlayerChoiceContext choiceContext, Player player)
    {
        return SelectFromPileToHand(choiceContext, player, PileType.Draw.GetPile(player), new LocString("card_selection", "KAREN_AWAKE_POTION_SELECT_FROM_DRAW_TO_HAND"));
    }

    public static Task<int> SelectFromDiscardPileToHand(PlayerChoiceContext choiceContext, Player player)
    {
        return SelectFromPileToHand(choiceContext, player, PileType.Discard.GetPile(player), new LocString("card_selection", "KAREN_AWAKE_POTION_SELECT_FROM_DISCARD_TO_HAND"));
    }

    public static Task<int> SelectFromPromisePileToHand(PlayerChoiceContext choiceContext, Player player)
    {
        return SelectFromPileToHand(choiceContext, player, KarenCustomEnum.PromisePile.GetPile(player), new LocString("card_selection", "KAREN_AWAKE_POTION_SELECT_FROM_PROMISE_TO_HAND"));
    }

    private static List<CardModel> GetAvailableOptions(Player player)
    {
        var options = new List<CardModel>();

        if (PileType.Draw.GetPile(player).Cards.Count > 0)
        {
            options.Add(ModelDb.Card<KarenAwakePotionDrawPileOption>());
        }

        if (!PromisePileManager.IsVoidMode(player) && KarenCustomEnum.PromisePile.GetPile(player).Cards.Count > 0)
        {
            options.Add(ModelDb.Card<KarenAwakePotionPromisePileOption>());
        }

        if (PileType.Discard.GetPile(player).Cards.Count > 0)
        {
            options.Add(ModelDb.Card<KarenAwakePotionDiscardPileOption>());
        }

        return options;
    }

    private static async Task<int> SelectFromPileToHand(PlayerChoiceContext choiceContext, Player player, CardPile pile, LocString prompt)
    {
        var selectCount = System.Math.Min(GetHandSpace(player), pile.Cards.Count);
        if (selectCount <= 0)
        {
            return 0;
        }

        var cards = pile.Cards.OrderBy(card => card.Rarity).ThenBy(card => card.Id).ToList();
        var prefs = new CardSelectorPrefs(prompt, 0, selectCount);
        var selected = (await CardSelectCmd.FromSimpleGrid(choiceContext, cards, player, prefs)).ToList();
        if (selected.Count == 0)
        {
            return 0;
        }

        await CardPileCmd.Add(selected, PileType.Hand);
        return selected.Count;
    }

    private static int GetHandSpace(Player player)
        => CardPile.MaxCardsInHand - PileType.Hand.GetPile(player).Cards.Count;
}
