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
/// 星罪 - 禁用X个遗物，然后抽2张牌
/// </summary>
public sealed class KarenStarCrime : KarenBaseCardModel
{
    public KarenStarCrime() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var xValue = ResolveEnergyXValue();

        // 禁用指定数量的遗物
        var relics = Owner.Relics.ToList();
        var relicsToDisable = Math.Min(xValue, relics.Count);

        for (int i = 0; i < relicsToDisable; i++)
        {
            relics[i].SetDisabled(true);
        }

        // 抽牌
        await CardPileCmd.Draw(choiceContext, (int)DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
