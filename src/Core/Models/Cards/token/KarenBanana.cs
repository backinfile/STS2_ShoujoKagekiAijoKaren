using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Models.CardPools;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;

/// <summary>
/// Banana - 2费 token 能力，获得力量。
/// </summary>
public sealed class KarenBanana : KarenBaseCardModel
{
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<KarenCardPool>();

    public KarenBanana() : base(2, CardType.Power, CardRarity.Token, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(8m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var effect = NKarenBananaVfx.Play(Owner, NKarenBananaVfx.CastKind.Dinner);
        if (effect != null) await NKarenBananaVfx.WaitForCue(NKarenBananaVfx.CastKind.Dinner);
        if (GodotObject.IsInstanceValid(effect) && !effect!.IsQueuedForDeletion()) effect.WatchStrength();
        await PowerCmd.Apply<StrengthPower>(choiceContext, 
            Owner.Creature,
            DynamicVars.Strength.BaseValue,
            Owner.Creature,
            this
        );
        if (GodotObject.IsInstanceValid(effect) && !effect!.IsQueuedForDeletion()) effect.CheckStrength();
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Strength.UpgradeValueBy(2m);
    }
}
