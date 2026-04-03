using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.relic;

/// <summary>
/// "宽恕" - 禁用X个遗物，然后对所有敌人造成伤害
/// </summary>
public sealed class KarenForgive : KarenBaseCardModel
{
    public KarenForgive() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var xValue = ResolveEnergyXValue();

        // TODO: 禁用指定数量的遗物
        // var relics = Owner.Relics.ToList();
        // var relicsToDisable = Math.Min(xValue, relics.Count);
        // for (int i = 0; i < relicsToDisable; i++)
        // {
        //     relics[i].SetDisabled(true);
        // }

        // 对所有敌人造成伤害
        foreach (var enemy in CombatState.HittableEnemies)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(enemy)
                .WithHitFx(VfxCmd.slashPath)
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
    }
}
