using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
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

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner.Player is { } player) KarenGeerVfxManager.Start(player);
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        if (oldOwner.Player is { } player) KarenGeerVfxManager.Stop(player);
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        for (int i = 0; i < Amount; i++)
        {
            if (PromisePileManager.GetCount(player) == 0) break;
            Flash();
            KarenGeerVfxManager.BeginSelection(player);
            try
            {
                // Give the mechanical engagement a short lead before the native selection grid.
                await Cmd.Wait(0.28f);
                await PromisePileCmd.SelectAndAutoPlayFromPromisePile(choiceContext, player,
                    selected => KarenGeerVfxManager.CommitSelection(player, selected));
            }
            finally
            {
                KarenGeerVfxManager.EndSelection(player);
            }
        }
    }
}
