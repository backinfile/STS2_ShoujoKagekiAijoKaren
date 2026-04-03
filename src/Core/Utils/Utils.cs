using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace ShoujoKagekiAijoKaren.src.Core.Utils
{
    public static class PlayerUtils
    {

        public static bool IsHandFull(Player player)
        {
            return player.PlayerCombatState?.Hand.Cards.Count >= CardPile.maxCardsInHand;
        }

    }

    public static class CardUtils
    {
       
    }
}
