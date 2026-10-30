using ShoujoKagekiAijoKaren.src.Core.Vfx;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// 旋转 - 造成8点伤害，此牌在牌堆之间移动时，在本场战斗中伤害增加2点
/// </summary>
public sealed class KarenSpin : KarenBaseCardModel
{
    public KarenSpin() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    private const string IncreaseVarName = "Increase";

    /// <summary>本场战斗中通过移动获得的额外伤害（用于降级时恢复）</summary>
    private decimal _extraDamageFromMoves;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new DynamicVar(IncreaseVarName, 2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        using var show = new KarenAttackCue(Owner.Creature, cardPlay.Target, KarenAttackStyle.Spin, count: 1, potency: (float)DynamicVars.Damage.BaseValue);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromPlayedCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithAttackerFx(show.Start)
            .WithHitVfxNode(show.Hit)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[IncreaseVarName].UpgradeValueBy(1m);
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        // 降级时将额外伤害加回基础值
        DynamicVars.Damage.BaseValue += _extraDamageFromMoves;
    }

    public override async Task OnGlobalMove(PileType from, PileType to, AbstractModel? source)
    {
        // 增加伤害
        decimal increase = DynamicVars[IncreaseVarName].BaseValue;
        BuffDamage(increase);
    }

    /// <summary>增加本张卡牌的伤害值</summary>
    private void BuffDamage(decimal extraDamage)
    {
        DynamicVars.Damage.BaseValue += extraDamage;
        _extraDamageFromMoves += extraDamage;
    }
}
