using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.Patches;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// Power类 - Position 0：每回合前X次造成未被格挡的攻击伤害时，获得等量格挡
/// </summary>
public class KarenPosition0Power : PowerModel
{
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerType Type => PowerType.Buff;

    public override async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        //MainFile.Logger.Info($"dealer.Name = {dealer?.Name} target.Name = {target?.Name}");
        if (dealer?.Player is Player player && player == Owner.Player && props == ValueProp.Move)
        {
            int count = AttackCounter.GetAttackCount(player);
            MainFile.Logger.Info($"KarenPosition0Power: Player has made {count} attacks this turn.");
            if (count <= Amount)
            {
                await CreatureCmd.GainBlock(dealer, amount, ValueProp.Unpowered, null);
                Flash();
            }
        }
    }
}
