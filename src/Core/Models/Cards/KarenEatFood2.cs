using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

/// <summary>
/// 三明治(EatFood2) - 0费技能，获得3临时力量，消耗
/// 由观察情况、Banana午餐等卡牌生成的token卡
/// </summary>
public sealed class KarenEatFood2 : KarenBaseCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    public KarenEatFood2() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(3m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得临时力量：+3本回合，-3下回合
        await PowerCmd.Apply<StrengthPower>(
            Owner.Creature,
            DynamicVars.Strength.BaseValue,
            Owner.Creature,
            this
        );

        // 下回合失去这些力量
        await PowerCmd.Apply<StrengthPower>(
            Owner.Creature,
            -DynamicVars.Strength.BaseValue,
            Owner.Creature,
            this
        );
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Strength.UpgradeValueBy(3m);
    }
}
