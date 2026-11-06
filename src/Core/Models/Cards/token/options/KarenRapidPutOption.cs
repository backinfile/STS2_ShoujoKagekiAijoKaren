using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.token.options
{
    internal class KarenRapidPutOption : KarenBaseCardModel
    {
        public KarenRapidPutOption() : base(-1, CardType.Skill, CardRarity.Token, TargetType.None)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

        public override async Task DoOption(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PromisePileCmd.AddFromHand(choiceContext, Owner, DynamicVars.Cards.IntValue, this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Cards.UpgradeValueBy(1);
        }

    }
}
