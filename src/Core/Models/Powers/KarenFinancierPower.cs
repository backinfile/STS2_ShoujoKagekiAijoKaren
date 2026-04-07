using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// Power类 - Banana费南雪的保留力量效果
/// </summary>
public class KarenFinancierPower : PowerModel
{
    private int _turnCounter = 0;

    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerType Type => PowerType.Buff;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player)
        {
            _turnCounter++;
            if (_turnCounter % 2 == 0)
            {
                // 每两回合保留力量1回合
                Flash();
                await PowerCmd.Apply<KarenRetainTmpStrengthPower>(player.Creature, Amount, player.Creature, null);
            }
        }
    }
}
