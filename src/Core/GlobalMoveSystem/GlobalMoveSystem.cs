using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;

namespace ShoujoKagekiAijoKaren.src.Core.GlobalMoveSystem;

/// <summary>
/// 全局卡牌移动监听系统。
/// 当任意卡牌在牌堆间移动时，触发 OnCardMoved 事件。
/// </summary>
public static class GlobalMoveSystem
{


    internal static async Task Trigger(CardModel card, PileType from, PileType to, AbstractModel? source)
    {
        MainFile.Logger.Info($"[GlobalMoveSystem] Card '{card.Title}' moved from {from} to {to} (source={source?.GetType().Name ?? "null"})");

        // 触发该卡牌的GlobalMove
        {
            if (card is KarenBaseCardModel karenCard)
            {
                await karenCard.OnGlobalMove(from, to, source);
            }
        }

    }
}
