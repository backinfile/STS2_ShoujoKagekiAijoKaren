using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

/// <summary>
/// 背负着我们犯下的罪过 - 2费，造成 12+4×N 伤（N=闪耀牌堆中的卡数）
/// 升级：15+5×N
/// </summary>
public sealed class KarenCarryingGuilt : KarenBaseCardModel
{
    public KarenCarryingGuilt() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        // 此卡无闪耀值（原版亦无 Shine）
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(12m, ValueProp.Move),
        new ExtraDamageVar(4m)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        int shinePileCount = ShinePileManager.GetShinePileCount(Owner);
        decimal totalDamage = DynamicVars.Damage.BaseValue + DynamicVars.ExtraDamage.BaseValue * shinePileCount;

        await DamageCmd.Attack(totalDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars.ExtraDamage.UpgradeValueBy(1m);
    }
}
