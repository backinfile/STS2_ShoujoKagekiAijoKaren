using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token.options;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Commands
{
    public static class CardPileCmdEx
    {
        /// <summary>
        /// 从抽牌堆选择卡牌放入手牌
        /// </summary>
        /// <param name="ctx">玩家选择上下文</param>
        /// <param name="player">玩家</param>
        /// <param name="count">选择数量</param>
        /// <returns></returns>
        public static async Task SelectFromDrawPileToHand(PlayerChoiceContext ctx, Player player, int count)
        {
            var pile = PileType.Draw.GetPile(player);
            var selectFrom = (from c in pile.Cards orderby c.Rarity, c.Id select c).ToList();
            IEnumerable<CardModel> selected = await CardSelectCmd.FromSimpleGrid(ctx, selectFrom, player, new CardSelectorPrefs(Tips.SelectFromDrawToHand, count));
            await CardPileCmd.Add(selected, PileType.Hand);
        }

        /// <summary>
        /// 从弃牌堆选择卡牌放入手牌
        /// </summary>
        /// <param name="ctx">玩家选择上下文</param>
        /// <param name="player">玩家</param>
        /// <param name="count">选择数量</param>
        /// <returns></returns>
        public static async Task SelectFromDiscardPileToHand(PlayerChoiceContext ctx, Player player, int count)
        {
            var pile = PileType.Discard.GetPile(player);
            var selectFrom = (from c in pile.Cards orderby c.Rarity, c.Id select c).ToList();
            IEnumerable<CardModel> selected = await CardSelectCmd.FromSimpleGrid(ctx, selectFrom, player, new CardSelectorPrefs(Tips.SelectFromDiscardToHand, count));
            await CardPileCmd.Add(selected, PileType.Hand);
        }

        /// <summary>
        /// 从多个选项卡牌中选择一项执行对应DoOption方法
        /// </summary>
        public static async Task SelectOption(PlayerChoiceContext ctx, CardPlay cardPlay, Player player, CombatState combatState, List<CardModel> options, bool upgrade = false)
        {
            var cards = CardUtils.CreateTokens(player, combatState, options, upgrade);

            var cardModel = await CardSelectCmd.FromChooseACardScreen(ctx, cards, player, canSkip: false);
            MainFile.Logger.Info($"KarenWakeUp selected card: {cardModel?.Title}");

            if (cardModel is KarenBaseCardModel karenCard)
            {
                await karenCard.DoOption(ctx, cardPlay);
            }

            // 只移除实际在战斗牌堆中的临时代币卡牌
            // 注意：某些选项（如约定牌堆选项）可能已将代币卡牌移动到其他位置
            var cardsInCombat = cards.Where(c => c.Pile != null).ToList();
            if (cardsInCombat.Count > 0)
            {
                await CardPileCmd.RemoveFromCombat(cardsInCombat);
            }
        }
    }
}
