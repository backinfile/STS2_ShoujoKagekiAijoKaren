using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ShoujoKagekiAijoKaren.src.Core.GlobalMoveSystem;

/// <summary>
/// 全局卡牌移动监听系统。
/// 当任意卡牌在牌堆间移动时，触发 OnCardMoved 事件。
/// </summary>
public static class GlobalMoveSystem
{
 

    internal static void Invoke(CardModel card, PileType from, PileType to, AbstractModel? source)
    {
        MainFile.Logger.Info($"[GlobalMoveSystem] Card '{card.Title}' moved from {from} to {to} (source={source?.GetType().Name ?? "null"})");
    }
}
