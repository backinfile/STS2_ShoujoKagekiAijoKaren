using MegaCrit.Sts2.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.basic;

public sealed class KarenStrike() : KarenBaseCardModel(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromPlayedCard(this, cardPlay)
            .Targeting(cardPlay.Target);
        if (NKarenBasicAttackVfx.Enabled)
            attack.WithHitVfxNode(target => NKarenStrikeVfx.Create(Owner.Creature, target));
        else
            attack.WithHitFx(VfxCmd.slashPath);
        await attack.Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
