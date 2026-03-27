using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;

/// <summary>
/// 我们的约定 - 1费技能，抽3(升4)张牌。将2张手牌放入约定牌堆。
/// </summary>
public sealed class KarenOurPromise : KarenBaseCardModel
{
    protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { KarenCustomEnum.PromisePileRelated };

    public KarenOurPromise() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(3)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 抽牌
        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);

        // 选择2张手牌放入约定牌堆
        await PromisePileCmd.AddFromHand(choiceContext, base.Owner, 2, this);
    }

    protected override void OnUpgrade()
        => DynamicVars.Cards.UpgradeValueBy(1m);
}
