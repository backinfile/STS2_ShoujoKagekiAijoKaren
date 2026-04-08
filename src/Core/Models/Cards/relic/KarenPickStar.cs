using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using MegaCrit.Sts2.Core.Entities.Cards;
using ShoujoKagekiAijoKaren.src.Core;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.relic;

/// <summary>
/// 摘星 - 禁用X个遗物，然后获得2能量
/// </summary>
public sealed class KarenPickStar : KarenDisableRelicBaseCardModel
{
    public KarenPickStar() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2), new DisableRelicVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DisableRelicCmd.DisableRelic(Owner, DisableRelicVar.IntValue);
        // 获得能量
        await PlayerCmd.GainEnergy((int)DynamicVars.Energy.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DisableRelicVar.UpgradeValueBy(-1);
    }
}
