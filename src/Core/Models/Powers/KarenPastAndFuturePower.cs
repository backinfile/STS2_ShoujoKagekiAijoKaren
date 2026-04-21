using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Powers;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers.tmpStrength;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// 过去与未来 Power：从约定牌堆抽牌时获得临时力量
/// </summary>
public sealed class KarenPastAndFuturePower : KarenBasePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task OnCardRemovedFromPromisePile(CardModel card)
    {
        if (Owner.Player is Player player)
        {
            Flash();
            await PowerCmd.Apply<KarenPastAndFutureTempStrengthPower>(
                player.Creature, Amount, player.Creature, null);
        }
    }
}
