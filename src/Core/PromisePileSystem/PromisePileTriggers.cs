using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem
{
    public class PromisePileTriggers
    {
        /// <summary>
        /// 扳机-当约定牌堆变空时触发
        /// TODO 看看这个全不全
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static async Task TriggerPromisePileEmpty(Player player)
        {
            // 弃置牌之后约定牌堆空了，触发扳机
            foreach (var power in player.Creature.Powers)
            {
                if (power is KarenBasePower karenPower)
                {
                    await karenPower.OnPromisePileEmpty();
                }
            }
            MainFile.Logger.Info($"[PromisePile] Promise pile is now empty, triggered OnPromisePileEmpty");
        }


        /// <summary>
        /// 扳机 - 回合结束时如果在约定牌堆
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static async Task TriggerPromisePileTurnEnd(Player player)
        {
            MainFile.Logger.Info("[PromisePile] Handling turn end trigger for player's promise pile...");
            if (!PromisePileManager.IsVoidMode(player)) // （非 Void 模式）
            {
                var pile = PromisePileManager.GetPromisePile(player);
                foreach (var card in pile.Cards.ToList())
                {
                    if (card is KarenBaseCardModel karenCard)
                    {
                        await karenCard.OnTurnEndInPromisePile();
                    }
                }
                return;
            }
            else // 空虚模式，处理抽牌堆中的牌
            {
                var pile = PileType.Draw.GetPile(player);
                foreach (var card in pile.Cards.ToList())
                {
                    if (card is KarenBaseCardModel karenCard)
                    {
                        await karenCard.OnTurnEndInPromisePile();
                    }
                }
            }
        }
    }
}
