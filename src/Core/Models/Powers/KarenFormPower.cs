using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// Power类 - 觉醒形态：每个回合开始时，抽满手牌
/// </summary>
public class KarenFormPower : PowerModel
{
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerType Type => PowerType.Buff;

    public override decimal ModifyHandDrawLate(Player player, decimal count)
    {
        return CardPile.maxCardsInHand;
    }

    //public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    //{
    //    if (player == base.Owner.Player)
    //    {
    //        int max = CardPile.maxCardsInHand;
    //        var hand = PileType.Hand.GetPile(player);
    //        int drawnCnt = 0;
    //        while (hand.Cards.Count < max)
    //        {
    //            var drawn = await CardPileCmd.Draw(choiceContext, 1, base.Owner.Player);
    //            if (!drawn.Any()) break; // 没牌可抽了
    //            if (drawnCnt++ > 100) break; // 保险措施，防止死循环
    //        }
    //    }
    //}
}
