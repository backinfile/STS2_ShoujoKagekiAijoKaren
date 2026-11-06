using ShoujoKagekiAijoKaren.src.Core.Vfx;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// 以觉悟的名义 - 1费攻击，造成6点伤害，获得与未被格挡伤害相等的格挡
/// </summary>
[ShoujoKageki.Core.V1.ShineCard(9)]
public sealed class KarenConsciousness : KarenBaseCardModel
{
    public KarenConsciousness() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move)
    ];
    public override bool GainsBlock => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        using var show = new KarenAttackCue(Owner.Creature, cardPlay.Target, KarenAttackStyle.Consciousness, potency: (float)DynamicVars.Damage.BaseValue);
        var result = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromPlayedCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithAttackerFx(show.Start)
            .WithHitVfxNode(show.Hit)
            .Execute(choiceContext);

        var blockAmount = result.Results.SelectMany(r => r).Sum((DamageResult r) => r.TotalDamage + r.OverkillDamage);
        await CreatureCmd.GainBlock(Owner.Creature, blockAmount, ValueProp.Move, cardPlay);
        if (blockAmount > 0) show.ConfirmBlock();
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
