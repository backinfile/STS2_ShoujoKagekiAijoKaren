using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.token.options
{
    internal class KarenWakeUpPromisePile : KarenBaseCardModel
    {
        public KarenWakeUpPromisePile() : base(-1, CardType.Skill, CardRarity.Token, TargetType.None)
        {
        }

        public override async Task DoOption(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PromisePileCmd.SelectedToHand(choiceContext, Owner, 1);
        }
    }
}
