using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// 激情
/// </summary>
public sealed class KarenPassionPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount,
        Creature? applier, CardModel? cardSource)
    {
        // Native notification covers first application and stacking, without replaying on power loss.
        if (power == this && amount >= 1 && Amount > 0)
            NKarenPassionVfx.Play(this, (int)amount);
        return Task.CompletedTask;
    }
}
