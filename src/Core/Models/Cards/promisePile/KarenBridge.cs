using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;

/// <summary>
/// 塔桥
/// </summary>
public sealed class KarenBridge : KarenBaseCardModel
{
    protected override HashSet<CardTag> CanonicalTags => [KarenCustomEnum.PromisePileRelated];

    protected override bool HasEnergyCostX => true;

    public KarenBridge() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var xValue = ResolveEnergyXValue();
        if (IsUpgraded) xValue += 1;
        if (xValue <= 0) return;

        //await CardPileCmd.AutoPlayFromDrawPile(choiceContext, base.Owner, num, CardPilePosition.Top, forceExhaust: false);
        await PromisePileCmd.AutoPlayFromPromisePile(choiceContext, base.Owner, xValue);
    }

}
