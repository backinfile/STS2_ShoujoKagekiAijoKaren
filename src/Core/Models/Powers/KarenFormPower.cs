using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// Power类 - 觉醒形态：每个回合开始时，抽满手牌
/// </summary>
public class KarenFormPower : PowerModel
{
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerType PowerType => PowerType.Buff;

    public override async Task OnSideTurnStart(PlayerChoiceContext choiceContext, bool isPlayerTurn)
    {
        if (isPlayerTurn)
        {
            var maxHandSize = Owner.HandSize;
            var currentHandCount = Owner.CardPiles.HandPile.Count;
            var cardsToDraw = maxHandSize - currentHandCount;

            if (cardsToDraw > 0)
            {
                await CardPileCmd.Draw(choiceContext, cardsToDraw, Owner);
            }
        }
    }
}
