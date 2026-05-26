using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// 命运的齿轮：回合开始抽牌后，选择约定牌堆中的一张牌并打出。
/// </summary>
public sealed class KarenGeerPower : KarenBasePower
{
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerType Type => PowerType.Buff;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        for (int i = 0; i < Amount; i++)
        {
            Flash();
            await PromisePileCmd.SelectAndAutoPlayFromPromisePile(choiceContext, player);
        }
    }
}
