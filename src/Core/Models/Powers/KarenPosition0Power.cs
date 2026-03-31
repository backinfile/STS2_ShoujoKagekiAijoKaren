using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// Power类 - Position 0：每回合前X次造成未被格挡的攻击伤害时，获得等量格挡
/// </summary>
public class KarenPosition0Power : PowerModel
{
    private int _remainingHits;

    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerType PowerType => PowerType.Buff;

    public override void OnInitialApplication()
    {
        _remainingHits = (int)Amount;
    }

    public override async Task OnSideTurnStart(PlayerChoiceContext choiceContext, bool isPlayerTurn)
    {
        if (isPlayerTurn)
        {
            _remainingHits = (int)Amount;
        }
    }

    public override async Task OnAfterDamageGiven(PlayerChoiceContext choiceContext, Creature target, decimal damage, bool wasBlocked, bool wasUnblocked)
    {
        if (_remainingHits > 0 && wasUnblocked && target.IsEnemy)
        {
            await BlockCmd.Gain(damage, Owner, Applier).Execute(choiceContext);
            _remainingHits--;
        }
    }
}
