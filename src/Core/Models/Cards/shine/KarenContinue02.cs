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
/// 永无结束的命运舞台 - 1费12伤，Shine 9，可无限升级
/// 每次升级伤害递增（+3, +4, +5, ...）
/// </summary>
public sealed class KarenContinue02 : KarenBaseCardModel
{
    private int _upgradeCount = 0;

    public KarenContinue02() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        this.AddShineMax(9);
    }

    public override int MaxUpgradeLevel => int.MaxValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(12m, ValueProp.Move)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m + _upgradeCount);
        _upgradeCount++;
    }
}
