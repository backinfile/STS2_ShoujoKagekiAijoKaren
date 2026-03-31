using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.Commands
{
    internal class ExtraRewardCmd
    {

        /// <summary>
        /// 发放闪耀牌奖励
        /// </summary>
        public static async Task AddShineCardReward(Player player, CardModel? except = null)
        {
            var shineCard = ShineManager.GetAllShineCards().Where(card => except == null || except.Id != card.Id).TakeRandom(1, player.PlayerRng.Rewards).First();
            var clone = player.RunState.CreateCard(shineCard, player);
            if (player.RunState.CurrentRoom is CombatRoom combatRoom)
            {
                // 随机取一张闪耀牌作为奖励，排除自己
                combatRoom.AddExtraReward(player, new SpecialCardReward(clone, player));
                MainFile.Logger.Info($"Added shine card reward: {shineCard?.Title} for player: {player.NetId}");
                // 标记Power
                await PowerCmd.Apply<KarenShineCardRewardPower>(player.Creature, 1m, player.Creature, null);
            }
            else
            {
                await RewardsCmd.OfferCustom(player, [new SpecialCardReward(clone, player)]);
            }
        }

        public static async Task AddRelicReward(Player player)
        {
            // TODO
        }
    }
}
