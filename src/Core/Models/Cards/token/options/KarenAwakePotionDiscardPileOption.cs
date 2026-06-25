using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.token.options;

internal sealed class KarenAwakePotionDiscardPileOption : KarenBaseCardModel
{
    public KarenAwakePotionDiscardPileOption() : base(-1, CardType.Skill, CardRarity.Token, TargetType.None)
    {
    }

    public override async Task DoOption(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await AwakePotionAction.SelectFromDiscardPileToHand(choiceContext, Owner);
    }
}
