using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.relic;

/// <summary>
/// 嫉妒 - 造成伤害，闪耀耗尽时获得随机遗物
/// </summary>
public sealed class KarenJealousy : KarenBaseCardModel
{
    public KarenJealousy() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) {

        this.AddShineMax(9);
    }

    public override bool CanBeGeneratedInCombat => false;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(15, ValueProp.Move),
        new DynamicVar("Relics", 1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        NKarenJealousyVfx? sweep = null;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromPlayedCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithAttackerFx(() => sweep = NKarenJealousyVfx.Create(Owner.Creature, cardPlay.Target))
            .WithAttackerAnim("Attack", 0f)
            .WithWaitBeforeHit(NKarenJealousyVfx.FastHitTime, NKarenJealousyVfx.HitTime)
            .WithHitFx(tmpSfx: "slash_attack.mp3")
            .WithHitVfxNode(target => NKarenJealousyVfx.CreateImpact(target, sweep))
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
        DynamicVars["Relics"].UpgradeValueBy(1m);
    }

    public override async Task OnShineExhausted(PlayerChoiceContext ctx, bool inCombat, ICombatState combatState)
    {
        for (var i = 0; i < DynamicVars["Relics"].IntValue; i++)
            await ExtraRewardCmd.AddRelicReward(Owner);
        if (inCombat) NKarenJealousyVfx.ConfirmReward(Owner.Creature, DynamicVars["Relics"].IntValue);
    }
}
