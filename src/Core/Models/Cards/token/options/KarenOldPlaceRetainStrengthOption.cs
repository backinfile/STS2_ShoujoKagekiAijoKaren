using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;

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

        public override async Task DoOption(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<KarenRetainTmpStrengthPower>(Owner.Creature, 1, Owner.Creature, this);
        }
    }
}
