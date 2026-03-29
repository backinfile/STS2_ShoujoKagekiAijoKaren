using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

/// <summary>
/// 重逢临时版 - 0费技能，获得2临时力量，打出后进入约定牌堆
/// </summary>
public sealed class KarenMeetAgainTmp : KarenBaseCardModel
{
    public KarenMeetAgainTmp() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override HashSet<CardTag> CanonicalTags => [KarenCustomEnum.PromisePileRelated];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<KarenMeetAgainTmpTempStrengthPower>(2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<KarenMeetAgainTmpTempStrengthPower>(
            Owner.Creature,
            DynamicVars[nameof(KarenMeetAgainTmpTempStrengthPower)].BaseValue,
            Owner.Creature,
            this
        );

        await PromisePileCmd.Add(this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[nameof(KarenMeetAgainTmpTempStrengthPower)].UpgradeValueBy(2m);
    }
}
