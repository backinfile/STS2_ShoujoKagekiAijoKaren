using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace ShoujoKagekiAijoKaren.src.Core;

public static class KarenCustomEnum
{
    /// <summary>约定牌堆相关卡牌的标记 Tag</summary>
    [CustomEnum] public static CardTag PromisePileRelated;

    /// <summary>约定牌堆虚拟 PileType，用于 GlobalMoveSystem 事件的 from/to 标识</summary>
    [CustomEnum] public static PileType PromisePile;
}
