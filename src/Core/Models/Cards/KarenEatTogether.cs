using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

/// <summary>
/// 一起吃饭吧 - 1费技能，获得3临时力量，下回合失去，然后获得1层保留力量
/// </summary>
public sealed class KarenEatTogether : KarenBaseCardModel
{
    public KarenEatTogether() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<KarenEatTogetherTempStrengthPower>(3m),
        new PowerVar<StrengthPower>(1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得临时力量（本回合+3，下回合-3）
        await PowerCmd.Apply<KarenEatTogetherTempStrengthPower>(
            Owner.Creature,
            DynamicVars[nameof(KarenEatTogetherTempStrengthPower)].BaseValue,
            Owner.Creature,
            this
        );

        // 获得保留力量（回合结束时不会消失）
        await PowerCmd.Apply<StrengthPower>(
            Owner.Creature,
            DynamicVars.Strength.BaseValue,
            Owner.Creature,
            this
        );
    }

    protected override void OnUpgrade()
    {
        DynamicVars[nameof(KarenEatTogetherTempStrengthPower)].UpgradeValueBy(1m);
    }
}
