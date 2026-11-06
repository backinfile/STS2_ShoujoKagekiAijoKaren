using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>水族馆：回合开始抽牌，然后选择等量手牌放入约定牌堆。</summary>
public sealed class KarenAquariumPower : KarenBasePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount,
        Creature? applier, CardModel? cardSource)
    {
        if (power == this && amount > 0) NKarenAquariumVfx.Ensure(this, cast: true);
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        NKarenAquariumVfx.Remove(this);
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;
        Flash();
        var effect = NKarenAquariumVfx.Ensure(this);
        effect?.BeginTurn();
        try
        {
            var drawn = await CardPileCmd.Draw(choiceContext, Amount, player);
            effect?.ConfirmDraw(drawn);
            effect?.BeginSelection();
            var moved = await PromisePileCmd.AddFromHand(choiceContext, player, Amount, this);
            effect?.ConfirmMoves(moved);
        }
        finally { effect?.EndSelection(); }
    }
}
