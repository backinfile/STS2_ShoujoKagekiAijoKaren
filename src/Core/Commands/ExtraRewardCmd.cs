
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
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
        public static Task AddShineCardReward(Player player, CardModel? except = null)
            => ShoujoKageki.Core.V1.ShineRewards.Add(player, except);

        /// <summary>
        /// 发放遗物奖励
        /// </summary>
        public static async Task AddRelicReward(Player player)
        {
            if (player.RunState.CurrentRoom is CombatRoom combatRoom)
            {
                combatRoom.AddExtraReward(player, new RelicReward(player));
                MainFile.Logger.Info($"Added relic reward for player: {player.NetId}");
                await PowerCmd.Apply<KarenPassionPower>(new ThrowingPlayerChoiceContext(), player.Creature, 1m, player.Creature, null);
            }
            else
            {
                await RewardsCmd.OfferCustom(player, [new RelicReward(player)]);
            }
        }
    }
}
