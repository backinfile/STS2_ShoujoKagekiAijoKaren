using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;

/// <summary>
/// 棒球 - 3费稀有攻击牌，造成24/32点伤害。
/// 保留手牌、能量、格挡和力量2回合。
/// </summary>
public sealed class KarenBaseball : KarenBaseCardModel
{
    public KarenBaseball() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override HashSet<CardTag> CanonicalTags => [KarenCustomEnum.RetainTmpStrength];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(24m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(VfxCmd.slashPath)
            .Execute(choiceContext);

        // 同时应用4种保留效果，各2回合
        await PowerCmd.Apply<RetainHandPower>(Owner.Creature, 2, Owner.Creature, this);
        await PowerCmd.Apply<KarenRetainEnergyPower>(Owner.Creature, 2, Owner.Creature, this);
        await PowerCmd.Apply<BlurPower>(Owner.Creature, 2, Owner.Creature, this);
        await PowerCmd.Apply<KarenRetainTmpStrengthPower>(Owner.Creature, 2, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(8m);
    }
}
