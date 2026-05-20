using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.token.options
{
    // 保留格挡
    internal class KarenOldPlaceRetainBlockOption : KarenBaseCardModel
    {


        public KarenOldPlaceRetainBlockOption() : base(-1, CardType.Skill, CardRarity.Token, TargetType.None)
        {
        }
        protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Turns", 1)];
        protected override void OnUpgrade()
        {
            DynamicVars["Turns"].UpgradeValueBy(1);
        }

        public override async Task DoOption(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<BlurPower>(Owner.Creature, DynamicVars["Turns"].IntValue, Owner.Creature, this);
        }

    }
}
