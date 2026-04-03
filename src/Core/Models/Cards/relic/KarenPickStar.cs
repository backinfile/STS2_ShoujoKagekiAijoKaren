using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.relic;

/// <summary>
/// 摘星 - 禁用X个遗物，然后获得2能量
/// </summary>
public sealed class KarenPickStar : KarenBaseCardModel
{
    public KarenPickStar() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var xValue = ResolveEnergyXValue();

        // TODO: 禁用指定数量的遗物
        // var relics = Owner.Relics.ToList();
        // var relicsToDisable = Math.Min(xValue, relics.Count);
        // for (int i = 0; i < relicsToDisable; i++)
        // {
        //     relics[i].SetDisabled(true);
        // }

        // 获得能量
        await PlayerCmd.GainEnergy((int)DynamicVars.Energy.BaseValue, Owner);
        // TODO: 能量获得特效
        // await VfxCmd.EnergyGain(choiceContext, Owner, (int)DynamicVars.Energy.BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1);
    }
}
