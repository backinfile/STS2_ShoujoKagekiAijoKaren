using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem
{
    public class PromisePileHooks
    {
        /// <summary>
        /// 扳机-当约定牌堆变空时触发
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static async Task TriggerPromisePileEmpty(Player player)
        {
            // 无限能力下不触发这个扳机
            if (PromisePileManager.IsInMode(player, PromisePileMode.InfiniteReinforcement))
            {
                return;
            }

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


        /// <summary>
        /// 触发玩家身上所有 KarenBasePower 的 OnCardAddedToPromisePile 扳机
        /// </summary>
        public static async Task TriggerOnCardAdded(Player player, CardModel card)
        {
            if (player?.Creature == null) return;
            foreach (var power in player.Creature.Powers.OfType<KarenBasePower>())
            {
                await power.OnCardAddedToPromisePile(card);
            }
            if (card is KarenBaseCardModel karenCard)
            {
                await karenCard.OnAddedToPromisePile();
            }
        }

        /// <summary>
        /// 触发玩家身上所有的 OnCardRemovedFromPromisePile 扳机
        /// </summary>
        public static async Task TriggerOnCardRemoved(Player player, CardModel card)
        {
            if (player?.Creature == null) return;

            // 触发burn模式
            if (PromisePileManager.IsInMode(player, PromisePileMode.Burn))
            {
                KarenPromisePilePower.AddBurnEffect(card);
            }


            foreach (var power in player.Creature.Powers.OfType<KarenBasePower>())
            {
                await power.OnCardRemovedFromPromisePile(card);
            }
            if (card is KarenBaseCardModel karenCard)
            {
                await karenCard.OnRemovedFromPromisePile();
            }
        }

       
    }
}
