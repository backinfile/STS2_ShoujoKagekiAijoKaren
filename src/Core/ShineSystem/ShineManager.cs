using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using ShoujoKagekiAijoKaren.src.Models.CardPools;
using ShoujoKagekiAijoKaren.src.Models.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.ShineSystem
{
    public class ShineManager
    {
        public static IEnumerable<CardModel> GetAllShineCards()
        {
            return ModelDb.CardPool<KarenCardPool>().AllCards.Where(c => c.IsShineCard());
        }

        public static IEnumerable<CardModel> GetAllShineCards(Player player, bool excludeOtherPlayerRestricted = false)
        {
            return ModelDb.CardPool<KarenCardPool>()
                .GetUnlockedCards(UnlockState.all, player.RunState.CardMultiplayerConstraint)
                .Where(c => c.IsShineCard() && c.Rarity != CardRarity.Basic)
                .Where(c => !excludeOtherPlayerRestricted || c is not KarenBaseCardModel karenCard || karenCard.ShineCardForOtherPlayer);
        }

        public static IEnumerable<CardModel> GetRewardableShineCards(Player player, CardModel? except = null)
        {
            bool excludeOtherPlayerRestricted = ShouldExcludeOtherPlayerRestrictedShineCards(player);

            return GetAllShineCards(player, excludeOtherPlayerRestricted)
                .Append(ModelDb.Card<KarenShineReproduce>())
                .Where(card => card.Rarity != CardRarity.Basic)
                .Where(card => card.Id != except?.Id)
                .Where(card => !excludeOtherPlayerRestricted || card is not KarenBaseCardModel karenCard || karenCard.ShineCardForOtherPlayer);
        }

        public static bool ShouldExcludeOtherPlayerRestrictedShineCards(Player player)
        {
            return player.Character?.Id?.Entry != Karen.CHAR_ID;
        }

    }
}
