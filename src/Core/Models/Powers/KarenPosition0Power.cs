using MegaCrit.Sts2.Core.Commands;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.Patches;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// Power类 - Position 0：每回合前X次造成未被格挡的攻击伤害时，获得等量格挡
/// </summary>
public class KarenPosition0Power : PowerModel
{
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerType Type => PowerType.Buff;

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        // Both first application and stacking acknowledge the actual successful change.
        if (power == this && amount > 0) NKarenPositionZeroVfx.Ensure(this, claim: true);
        return Task.CompletedTask;
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        // Restored combat powers rebuild only their quiet stage, after creature views exist.
        if (room is CombatRoom) Callable.From(() => NKarenPositionZeroVfx.Ensure(this)).CallDeferred();
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player) NKarenPositionZeroVfx.Ensure(this);
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        NKarenPositionZeroVfx.Remove(this);
        return Task.CompletedTask;
    }

    public override async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer?.Player is Player player && player == Owner.Player && props == ValueProp.Move && cardSource != null)
        {
            int count = AttackCounter.GetAttackCount(player);
            if (count <= Amount)
            {
                int before = dealer.Block;
                await CreatureCmd.GainBlock(dealer, amount, ValueProp.Unpowered, null);
                if (dealer.Block > before)
                    NKarenPositionZeroVfx.Ensure(this)?.ConfirmBlock(target, dealer.Block - before);
                Flash();
            }
        }
    }
}
