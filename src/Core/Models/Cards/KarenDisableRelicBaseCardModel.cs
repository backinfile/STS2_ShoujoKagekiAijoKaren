using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards
{
    public abstract class KarenDisableRelicBaseCardModel : KarenBaseCardModel
    {
        protected KarenDisableRelicBaseCardModel(int energyCost, CardType type, CardRarity rarity, TargetType targetType = TargetType.None) : base(energyCost, type, rarity, targetType)
        {
        }

        public DynamicVar DisableRelicVar => DynamicVars[ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem.DisableRelicVar.VarName];


        protected override bool IsPlayable => DisableRelicCmd.GetDisableableRelicCount(Owner) >= DisableRelicVar.IntValue;
    }
}
