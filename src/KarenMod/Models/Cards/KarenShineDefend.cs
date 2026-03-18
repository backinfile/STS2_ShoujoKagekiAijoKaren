using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.KarenMod.DynamicVars;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

/// <summary>
/// 闪耀防御 - 1费获得8格挡，Shine 5
/// 打出5次后从卡组移除
/// </summary>
public sealed class KarenShineDefend : CardModel
{
    public KarenShineDefend() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(8m, ValueProp.Move),
        new KarenShineVar(5m)
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            var tips = base.ExtraHoverTips.ToList();
            var shineTitle = new LocString("cards", "KAREN_SHINE_KEYWORD.title");
            var shineDesc = new LocString("cards", "KAREN_SHINE_KEYWORD.description");
            tips.Add(new HoverTip(shineTitle, shineDesc));
            return tips;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
