using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

/// <summary>
/// 第一幕
/// 打出闪耀牌后抽X
/// </summary>
public sealed class KarenStarlightCard() : KarenBaseCardModel(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<KarenStarlightPower>(1m)
    ];

    public override IEnumerable<CardTag> Tags => [KarenCustomEnum.ShineRelated];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 给自己添加星光Power
        await PowerCmd.Apply<KarenStarlightPower>(
            Owner.Creature,
            DynamicVars[nameof(KarenStarlightPower)].BaseValue,
            Owner.Creature,
            this
        );
    }

    protected override void OnUpgrade()
    {
        DynamicVars[nameof(KarenStarlightPower)].UpgradeValueBy(1m);
    }
}
