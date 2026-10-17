using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

/// <summary>
/// 用你的闪耀贯穿我吧 - 1费 3×4=12伤（多段），Shine 6
/// 升级：5段（3×5=15伤）
/// </summary>
public sealed class KarenShineStrikeBarrage : KarenBaseCardModel
{
    public KarenShineStrikeBarrage() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        this.AddShineMax(6);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(3m, ValueProp.Move),
        new RepeatVar(4)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        NKarenShineBarrageVfx? preparation = null;
        NKarenShineBarrageVfx? lastHit = null;
        int hitIndex = 0;
        try
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(DynamicVars.Repeat.IntValue)
                .FromPlayedCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithAttackerFx(() => preparation = NKarenShineBarrageVfx.Create(Owner.Creature, cardPlay.Target))
                .WithAttackerAnim("Attack", .15f)
                .OnlyPlayAnimOnce()
                .WithWaitBeforeHit(NKarenShineBarrageVfx.FastHitInterval, NKarenShineBarrageVfx.HitInterval)
                .WithHitVfxNode(target => lastHit = NKarenShineBarrageVfx.Hit(Owner.Creature, target, hitIndex++))
                .Execute(choiceContext);
        }
        finally
        {
            NKarenShineBarrageVfx.Complete(preparation, lastHit);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Repeat.UpgradeValueBy(1m);
    }
}
