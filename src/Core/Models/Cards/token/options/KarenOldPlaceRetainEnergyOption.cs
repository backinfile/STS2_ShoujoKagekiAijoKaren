using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShoujoKagekiAijoKaren.src.Core.Commands;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.token.options
{
    // 保留能量
    internal class KarenOldPlaceRetainEnergyOption : KarenBaseCardModel
    {
        //public override int MaxUpgradeLevel => 0;

        public KarenOldPlaceRetainEnergyOption() : base(-1, CardType.Skill, CardRarity.Token, TargetType.None)
        {
        }

        public override async Task DoOption(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<RetainPower>(Owner.Creature, 1, Owner.Creature, this);
        }
    }
}
