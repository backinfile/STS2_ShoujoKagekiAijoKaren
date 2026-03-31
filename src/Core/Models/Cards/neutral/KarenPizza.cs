using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// 披萨 - 获得力量，所有敌人失去力量
/// </summary>
public sealed class KarenPizza : KarenBaseCardModel
{
    public KarenPizza() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies) { }

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new StrengthPowerVar(2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 玩家获得力量
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars.StrengthPower.BaseValue, Owner.Creature, this);

        // 所有敌人失去力量
        foreach (var enemy in CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<StrengthPower>(enemy, -DynamicVars.StrengthPower.BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.StrengthPower.UpgradeValueBy(1m);
    }
}
