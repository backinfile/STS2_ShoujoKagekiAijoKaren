using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// Position 0 - 每回合前X次造成未被格挡的攻击伤害时，获得等量格挡
/// </summary>
public sealed class KarenPosition0 : KarenBaseCardModel
{
    public KarenPosition0() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override bool HasEnergyCostX => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var xValue = ResolveEnergyXValue();
        await PowerCmd.Apply<KarenPosition0Power>(Owner.Creature, xValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
