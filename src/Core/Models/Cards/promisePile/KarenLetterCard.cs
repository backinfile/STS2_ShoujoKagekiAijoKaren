using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;

/// <summary>
/// 信（Letter）- 能力牌
/// 打出后获得信Power：每当有卡牌被放入约定牌堆，获得 X 点格挡
/// </summary>
public sealed class KarenLetterCard() : KarenBaseCardModel(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<KarenLetterPower>(2m)
    };


    public override IEnumerable<CardTag> Tags => [KarenCustomEnum.PromisePileRelated];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 给自己添加信Power
        await PowerCmd.Apply<KarenLetterPower>(
            Owner.Creature,
            DynamicVars[nameof(KarenLetterPower)].BaseValue,
            Owner.Creature,
            this
        );
    }

    protected override void OnUpgrade()
    {
        DynamicVars[nameof(KarenLetterPower)].UpgradeValueBy(1m);
    }
}
