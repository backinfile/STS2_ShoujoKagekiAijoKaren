using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace ShoujoKagekiAijoKaren.src.Core;

public static class KarenCustomEnum
{
    /// <summary>约定牌堆相关卡牌的标记 Tag</summary>
    [CustomEnum] public static CardTag PromisePileRelated;

    /// <summary>闪耀相关卡牌的标记 Tag</summary>
    [CustomEnum] public static CardTag ShineRelated;

    /// <summary>闪耀牌奖励</summary>
    [CustomEnum] public static CardTag ShineCardReward;

    /// <summary>临时力量</summary>
    [CustomEnum] public static CardTag TmpStrength;

    /// <summary>保留力量</summary>
    [CustomEnum] public static CardTag RetainTmpStrength;

    /// <summary>约定牌堆虚拟 PileType，用于 GlobalMoveSystem 事件的 from/to 标识</summary>
    [CustomEnum] public static PileType PromisePile;

    /// <summary>闪耀耗尽牌堆虚拟 PileType，用于拦截卡牌打出后的流向</summary>
    [CustomEnum] public static PileType ShineDepletePile;
}
