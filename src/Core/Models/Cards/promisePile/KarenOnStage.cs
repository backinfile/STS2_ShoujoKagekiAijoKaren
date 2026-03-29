using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;

/// <summary>
/// 我将再次变为我自己 - 1(升0)费技能，从约定牌堆抽4张牌。获得2点能量。消耗。
/// </summary>
public sealed class KarenOnStage : KarenBaseCardModel
{
    protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { KarenCustomEnum.PromisePileRelated };

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public KarenOnStage() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(4),
        new EnergyVar(2),
    ];


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PromisePileCmd.Draw(choiceContext, Owner, ((int)DynamicVars.Cards.BaseValue));
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        // 升级后费用变为0
        EnergyCost.UpgradeBy(-1);
    }
}
