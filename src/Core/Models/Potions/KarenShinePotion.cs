using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Potions;

public sealed class KarenShinePotion : KarenBasePotion
{
    private static readonly LocString SelectionTitle = new("potions", "KAREN_SHINE_POTION.selectionScreenPrompt");

    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var shineOptions = CardFactory.GetDistinctForCombat(
                Owner,
                Owner.Character.CardPool
                    .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
                    .Where(IsEligibleShineCard),
                3,
                Owner.RunState.Rng.CombatCardGeneration)
            .ToList();

        if (shineOptions.Count == 0)
        {
            return;
        }

        var selected = await SelectCard(choiceContext, shineOptions);
        if (selected == null)
        {
            return;
        }

        selected.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, Owner);

        var deckCopy = selected.CloneSafeForDeck();
        selected.DeckVersion = deckCopy;
        var deckResult = await CardPileCmd.Add(deckCopy, PileType.Deck);
        CardCmd.PreviewCardPileAdd(deckResult, 1.2f, CardPreviewStyle.MessyLayout);
    }

    private async Task<CardModel?> SelectCard(PlayerChoiceContext choiceContext, List<CardModel> cards)
    {
        if (cards.Count <= 3)
        {
            return await CardSelectCmdEx.FromChooseACardScreen(choiceContext, cards, Owner, canSkip: false, SelectionTitle);
        }

        var prefs = new CardSelectorPrefs(SelectionTitle, 1);
        return (await CardSelectCmd.FromSimpleGrid(choiceContext, cards, Owner, prefs)).FirstOrDefault();
    }

    private static bool IsEligibleShineCard(CardModel card)
    {
        return card is KarenBaseCardModel
            && card.IsShineCard()
            && card.GetShineMaxValue() > 0
            && card.CanBeGeneratedInCombat;
    }
}
