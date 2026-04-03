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

    /// <summary>本场战斗中通过移动获得的额外伤害（用于降级时恢复）</summary>
    private decimal _extraDamageFromMoves;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new DynamicVar("Increase", 2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(VfxCmd.slashPath)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        // 降级时将额外伤害加回基础值
        DynamicVars.Damage.BaseValue += _extraDamageFromMoves;
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        // 只响应自己的移动
        if (card != this) return;

        // 排除无效移动
        if (oldPileType == PileType.None || oldPileType == PileType.Deck) return;

        var newPile = card.Pile?.Type ?? PileType.None;
        if (newPile == PileType.None || newPile == PileType.Deck || newPile == oldPileType) return;

        // 增加伤害
        decimal increase = DynamicVars["Increase"].BaseValue;
        BuffDamage(increase);
        MainFile.Logger.Info($"[KarenSpin] 牌堆移动: {oldPileType} -> {newPile}，伤害+{increase}，当前总伤害: {DynamicVars.Damage.BaseValue}");
    }

    /// <summary>增加本张卡牌的伤害值</summary>
    private void BuffDamage(decimal extraDamage)
    {
        DynamicVars.Damage.BaseValue += extraDamage;
        _extraDamageFromMoves += extraDamage;
    }
}
