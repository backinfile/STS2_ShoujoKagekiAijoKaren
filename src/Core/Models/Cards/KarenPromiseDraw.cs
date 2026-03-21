using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

/// <summary>
/// 约定兑现 - 0费造成3点伤害，从约定牌堆取出2（升级后4）张牌到手牌。
/// </summary>
public sealed class KarenPromiseDraw : CardModel
{
    public KarenPromiseDraw() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(3m, ValueProp.Move),
        new CardsVar(2)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        int count = (int)DynamicVars.Cards.BaseValue;
        await PromisePileCmd.Draw(choiceContext, Owner, count);
    }

    protected override void OnUpgrade()
        => DynamicVars.Cards.UpgradeValueBy(2m);
}
