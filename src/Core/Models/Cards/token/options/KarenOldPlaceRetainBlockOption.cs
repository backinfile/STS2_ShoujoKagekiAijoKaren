using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.token.options
{
    // 保留格挡
    internal class KarenOldPlaceRetainBlockOption : KarenBaseCardModel
    {
        //public override int MaxUpgradeLevel => 0;

        public KarenOldPlaceRetainBlockOption() : base(-1, CardType.Skill, CardRarity.Token, TargetType.None)
        {
        }

        public override async Task DoOption(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<RetainBlock>(Owner.Creature, 1, Owner.Creature, this);
        }
    }
}
