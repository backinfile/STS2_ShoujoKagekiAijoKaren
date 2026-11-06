using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
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
            if (PromisePileManager.GetCount(player) == 0) break;
            Flash();
            KarenGeerVfxManager.PlayTrigger(player);
            // Fixed native timing keeps selection independent of local rendering.
            await Cmd.CustomScaledWait(0.65f, 0.65f);
            await PromisePileCmd.SelectAndAutoPlayFromPromisePile(choiceContext, player);
        }
    }
}
