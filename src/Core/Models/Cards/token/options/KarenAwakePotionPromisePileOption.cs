using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.token.options;

internal sealed class KarenAwakePotionPromisePileOption : KarenBaseCardModel
{
    protected override HashSet<CardTag> CanonicalTags => [KarenCustomEnum.PromisePileRelated];

    public KarenAwakePotionPromisePileOption() : base(-1, CardType.Skill, CardRarity.Token, TargetType.None)
    {
    }

    public override async Task DoOption(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await AwakePotionAction.SelectFromPromisePileToHand(choiceContext, Owner);
    }
}
