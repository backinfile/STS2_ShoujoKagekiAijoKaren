using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
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
        /// <param name="beforeMove">选择完成后的同步表现回调，不增加选牌等待。</param>
        /// <returns></returns>
        public static Task SelectFromDrawPileToHand(PlayerChoiceContext ctx, Player player, int count,
            Action<IEnumerable<CardModel>>? beforeMove = null)
            => SelectToHand(ctx, player, PileType.Draw, Tips.SelectFromDrawToHand, count, beforeMove);

        public static Task SelectFromDiscardPileToHand(PlayerChoiceContext ctx, Player player, int count,
            Action<IEnumerable<CardModel>>? beforeMove = null)
            => SelectToHand(ctx, player, PileType.Discard, Tips.SelectFromDiscardToHand, count, beforeMove);

        private static async Task SelectToHand(PlayerChoiceContext ctx, Player player, PileType type,
            LocString prompt, int count, Action<IEnumerable<CardModel>>? beforeMove)
        {
            var candidates = type.GetPile(player).Cards.OrderBy(card => card.Rarity).ThenBy(card => card.Id).ToArray();
            var selected = (await ShoujoKageki.Core.V1.CardPicking.Pick(ctx, player, candidates, new ShoujoKageki.Core.V1.CardPickRequest
            {
                Prompt = prompt, Minimum = Math.Max(0, count), Maximum = Math.Max(0, count), PreferHand = false
            })).ToArray();
            beforeMove?.Invoke(selected);
            await CardPileCmd.Add(selected, PileType.Hand);
        }

        /// <summary>
        /// 从多个选项卡牌中选择一项执行对应DoOption方法
        /// </summary>
        public static async Task SelectOption(PlayerChoiceContext ctx, CardPlay cardPlay, Player player, ICombatState combatState, List<CardModel> options, bool upgrade = false)
        {
            var cards = CardUtils.CreateTokens(player, combatState, options, upgrade);


            var selected = await ShoujoKageki.Core.V1.CardOptions.Choose(ctx, cards, player,
                canSkip: false, title: Tips.KAREN_SELECT_OPTION);
            if (selected is KarenBaseCardModel option) await option.DoOption(ctx, cardPlay);
        }
    }
}
