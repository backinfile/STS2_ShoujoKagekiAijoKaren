using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;

/// <summary>
/// 神圣星辰 - 2费攻击，造成14(升18)点伤害。被放入约定牌堆时，本场战斗中耗能减少1。
/// </summary>
public sealed class KarenHolyStar : KarenBaseCardModel
{
    protected override HashSet<CardTag> CanonicalTags => [KarenCustomEnum.PromisePileRelated];

    public KarenHolyStar() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(18m, ValueProp.Move),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx(VfxCmd.slashPath)
            .Execute(choiceContext);
    }

    /// <summary>
    /// 当被放入约定牌堆时，减少费用
    /// </summary>
    public override async Task OnAddedToPromisePile()
    {
        EnergyCost.AddThisCombat(-1);
        MainFile.Logger.Info($"[KarenHolyStar] Reduced energy cost by 1 for this combat. Current cost: {EnergyCost.GetResolved()}");
    }

    protected override void OnUpgrade()
        => DynamicVars.Damage.UpgradeValueBy(6m);
}
