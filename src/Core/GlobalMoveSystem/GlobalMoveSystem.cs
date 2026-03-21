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
    /// <summary>
    /// 卡牌移动事件。参数：(card, fromPile, toPile, source)
    /// fromPile = PileType.None 表示卡牌进入战斗（首次加入）
    /// toPile   = PileType.None 表示卡牌被从战斗移除
    /// </summary>
    public static event Action<CardModel, PileType, PileType, AbstractModel?>? OnCardMoved;

    internal static void Invoke(CardModel card, PileType from, PileType to, AbstractModel? source)
    {
        OnCardMoved?.Invoke(card, from, to, source);
    }
}
