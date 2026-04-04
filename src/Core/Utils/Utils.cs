using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public static List<CardModel> CreateTokens(Player player, CombatState combatState, List<CardModel> canonicalCards, bool upgrade = false)
        {
            var result = new List<CardModel>();

            foreach (var canonicalCard in canonicalCards)
            {
                var card = combatState.CreateCard(canonicalCard, player);
                if (upgrade) CardCmd.Upgrade(card);
                result.Add(card);
            }
            return result;
        }
    }

}
