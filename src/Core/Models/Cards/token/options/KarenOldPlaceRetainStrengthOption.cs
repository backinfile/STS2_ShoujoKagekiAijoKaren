using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.token.options
{
    // 保留力量
    internal class KarenOldPlaceRetainStrengthOption : KarenBaseCardModel
    {
        //public override int MaxUpgradeLevel => 0;

        public KarenOldPlaceRetainStrengthOption() : base(-1, CardType.Skill, CardRarity.Token, TargetType.None)
        {
        }
        public override IEnumerable<CardTag> Tags => [KarenCustomEnum.RetainTmpStrength];

        protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Turns", 1)];
        protected override void OnUpgrade()
        {
            DynamicVars["Turns"].UpgradeValueBy(1);
        }
        public override async Task DoOption(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<KarenRetainTmpStrengthPower>(Owner.Creature, DynamicVars["Turns"].IntValue, Owner.Creature, this);
        }
    }
}
