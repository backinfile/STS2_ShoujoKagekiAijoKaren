using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
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

        public static CardModel GetRandomRewardableShineCard(Player player, Rng rng, CardModel? except = null)
        {
            var targetBucket = SelectByWeight(rng,
                (ShineRewardBucket.Common, 42),
                (ShineRewardBucket.Uncommon, 42),
                (ShineRewardBucket.Rare, 15),
                (ShineRewardBucket.ShineReproduce, 1));

            var rewardableCards = GetRewardableShineCards(player, except).ToList();

            if (rewardableCards.Count == 0)
            {
                MainFile.Logger.Error($"[ShineReward] No rewardable shine cards are available. Player={player.NetId}, Except={except?.Id.ToString() ?? "<null>"}");
                return ModelDb.Card<KarenShineReproduce>();
            }

            var candidates = rewardableCards.Where(card => IsInShineRewardBucket(card, targetBucket)).ToList();
            return GetCardFromBucketOrFallback(candidates, targetBucket, rewardableCards, player, rng, except);
        }

        private enum ShineRewardBucket
        {
            Common,
            Uncommon,
            Rare,
            ShineReproduce,
        }

        private static CardModel GetCardFromBucketOrFallback(List<CardModel> bucketCards, ShineRewardBucket bucket, List<CardModel> rewardableCards, Player player, Rng rng, CardModel? except)
        {
            if (bucketCards.Count > 0)
            {
                var selected = bucketCards[rng.NextInt(bucketCards.Count)];
                MainFile.Logger.Info($"[ShineReward] Selected shine card from weighted bucket. Player={player.NetId}, Bucket={bucket}, Card={selected.Id}, Rarity={selected.Rarity}");
                return selected;
            }

            var fallback = rewardableCards[rng.NextInt(rewardableCards.Count)];
            MainFile.Logger.Warn($"[ShineReward] Weighted bucket has no available cards; fallback selected from all rewardable shine cards. Player={player.NetId}, MissingBucket={bucket}, FallbackCard={fallback.Id}, FallbackRarity={fallback.Rarity}, Except={except?.Id.ToString() ?? "<null>"}");
            return fallback;
        }

        private static T SelectByWeight<T>(Rng rng, params (T Value, int Weight)[] items)
        {
            var totalWeight = items.Sum(item => item.Weight);
            var roll = rng.NextInt(totalWeight);
            var currentWeight = 0;

            foreach (var item in items)
            {
                currentWeight += item.Weight;
                if (roll < currentWeight)
                {
                    return item.Value;
                }
            }

            return items[^1].Value;
        }

        private static bool IsInShineRewardBucket(CardModel card, ShineRewardBucket bucket)
        {
            return bucket switch
            {
                ShineRewardBucket.Common => card.Rarity == CardRarity.Common && card is not KarenShineReproduce,
                ShineRewardBucket.Uncommon => card.Rarity == CardRarity.Uncommon && card is not KarenShineReproduce,
                ShineRewardBucket.Rare => card.Rarity == CardRarity.Rare && card is not KarenShineReproduce,
                ShineRewardBucket.ShineReproduce => card is KarenShineReproduce,
                _ => false,
            };
        }

        public static bool ShouldExcludeOtherPlayerRestrictedShineCards(Player player)
        {
            return player.Character?.Id?.Entry != Karen.CHAR_ID;
        }

    }
}
