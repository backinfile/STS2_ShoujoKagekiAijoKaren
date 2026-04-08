using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token.options;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// 唤醒
/// </summary>
public sealed class KarenWakeUp : KarenBaseCardModel
{
    public KarenWakeUp() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsUpgraded)
        {
            if (CombatState != null)
            {
                await CardPileCmdEx.SelectOption(choiceContext, cardPlay, Owner, CombatState, [
                    ModelDb.Card<KarenWakeUpDrawPileOption>(),
                    ModelDb.Card<KarenWakeUpDiscardPileOption>(),
                    ModelDb.Card<KarenWakeUpPromisePileOption>()
                    ]);
            }
        }
        else
        {
            await CardPileCmdEx.SelectFromDrawPileToHand(choiceContext, Owner, 2);
        }

    }
}
