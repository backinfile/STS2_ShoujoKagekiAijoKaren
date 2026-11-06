using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.ancient;

/// <summary>
/// 获得格挡，并选择手牌寄存在约定中。
/// </summary>
public sealed class KarenWhy : KarenBaseCardModel
{
    public KarenWhy() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self) { }

    protected override HashSet<CardTag> CanonicalTags => [];

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(9m, ValueProp.Move), new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        var cue = NKarenWhyVfx.Begin(Owner);
        try
        {
            await PromisePileCmd.AddFromHand(choiceContext, Owner, (int)DynamicVars.Cards.BaseValue, this, true);
        }
        finally { cue?.End(); }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
