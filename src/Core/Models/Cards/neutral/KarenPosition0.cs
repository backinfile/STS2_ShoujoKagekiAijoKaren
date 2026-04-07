using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// Position 0
/// </summary>
public sealed class KarenPosition0 : KarenBaseCardModel
{
    public KarenPosition0() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }


    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<KarenPosition0Power>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<KarenPosition0Power>(Owner.Creature, DynamicVars[nameof(KarenPosition0Power)].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
