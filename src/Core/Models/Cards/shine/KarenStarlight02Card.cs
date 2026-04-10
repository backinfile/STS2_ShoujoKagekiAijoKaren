using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

/// <summary>
/// 第二幕
/// 打出的 闪耀 牌会额外打出 !M! 次，然后耗尽 闪耀 值。
/// TODO 耗尽
/// </summary>
public sealed class KarenStarlight02Card() : KarenBaseCardModel(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<KarenStarlight02Power>(1m)
    ];

    public override IEnumerable<CardTag> Tags => [KarenCustomEnum.ShineRelated];


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<KarenStarlight02Power>(
            Owner.Creature,
            DynamicVars[nameof(KarenStarlight02Power)].BaseValue,
            Owner.Creature,
            this
        );
    }

    protected override void OnUpgrade()
    {
        DynamicVars[nameof(KarenStarlight02Power)].UpgradeValueBy(1m);
    }
}
