using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using ShoujoKagekiAijoKaren.src.Core.Patches;
using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace ShoujoKagekiAijoKaren.src.Core.Commands;

/// <summary>
/// CardSelectCmd 的扩展方法，支持自定义标题
/// </summary>
public static class CardSelectCmdEx
{
    /// <summary>
    /// 从 NChooseACardSelectionScreen 选择一张卡（支持自定义标题）
    /// </summary>
    /// <param name="context">玩家选择上下文</param>
    /// <param name="cards">卡牌列表（最多3张）</param>
    /// <param name="player">玩家</param>
    /// <param name="canSkip">是否可以跳过</param>
    /// <param name="title">自定义标题（null则使用默认"CHOOSE_CARD_HEADER"）</param>
    /// <returns>选中的卡牌，未选择或取消则为 null</returns>
    public static async Task<CardModel?> FromChooseACardScreen(
        PlayerChoiceContext context,
        IReadOnlyList<CardModel> cards,
        Player player,
        bool canSkip = false,
        LocString? title = null)
    {
        if (cards.Count > 3)
        {
            throw new ArgumentException("Only works with less than 3 cards", nameof(cards));
        }

        uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(player);
        await context.SignalPlayerChoiceBegun(PlayerChoiceOptions.None);

        CardModel? result;

        if (ShouldSelectLocalCard(player))
        {
            NPlayerHand.Instance?.CancelAllCardPlay();

            if (CardSelectCmd.Selector != null)
            {
                result = (await CardSelectCmd.Selector.GetSelectedCards(cards, 0, 1)).FirstOrDefault();
            }
            else
            {
                // 使用 Patch 后的原版屏幕，设置自定义标题
                NChooseACardSelectionScreenPatch.NextCustomTitle = title;
                var screen = NChooseACardSelectionScreen.ShowScreen(cards, canSkip);

                if (LocalContext.IsMe(player))
                {
                    foreach (CardModel card in cards)
                    {
                        SaveManager.Instance.MarkCardAsSeen(card);
                    }
                }

                result = (await screen!.CardsSelected()).FirstOrDefault();
            }

            int index = cards.IndexOf(result);
            PlayerChoiceResult result2 = PlayerChoiceResult.FromIndex(index);
            RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(player, choiceId, result2);
        }
        else
        {
            int num = (await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(player, choiceId)).AsIndex();
            result = ((num < 0) ? null : cards[num]);
        }

        await context.SignalPlayerChoiceEnded();
        LogChoice(player, result);
        return result;
    }

    private static bool ShouldSelectLocalCard(Player player)
    {
        if (LocalContext.IsMe(player))
        {
            return RunManager.Instance.NetService.Type != NetGameType.Replay;
        }
        return false;
    }

    private static void LogChoice(Player player, CardModel? card)
    {
        string value = card?.Id.Entry ?? "null";
        Log.Info($"Player {player.NetId} chose card [{value}]");
    }
}
