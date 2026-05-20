using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Powers;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// 门票 Power：每回合开始时，将一张"约定之塔"加入手牌
/// </summary>
public sealed class KarenTicketPower : KarenBasePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player == Owner.Player)
        {
            Flash();
            var cards = new List<CardModel>();
            for (int i = 0; i < Amount; i++)
            {
                cards.Add(CombatState.CreateCard<KarenTowerOfPromise>(player));
            }
            await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, true);
        }
    }
}
