using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sts2Mod.chaosed0.sts2examplemod.src;

public class SuperStrike : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(999m, ValueProp.Move)];
    public SuperStrike() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true)
    {
    }


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(VfxCmd.bluntPath, tmpSfx: TmpSfx.heavyAttack)
            .Execute(choiceContext);
    }
}