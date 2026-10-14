using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;

/// <summary>
/// 舞台 - 0费Token技能牌，获得2/4能量。消耗。
/// </summary>
public sealed class KarenWeKownToken : KarenBaseCardModel
{
    public KarenWeKownToken() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var effect = NKarenBenefitVfx.Play(Owner, NKarenBenefitVfx.BenefitKind.Radiance, this);
        if (effect != null) await NKarenBenefitVfx.WaitForCue(NKarenBenefitVfx.BenefitKind.Radiance);
        effect?.WatchEnergy();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        effect?.CheckEnergy();
        if (effect != null) await effect.WaitForEnergyDeparture();
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(2m);
    }
}
