using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// 星星串起了我们的友谊 - 击杀敌人时获得额外卡牌奖励的标记 Power
/// </summary>
public sealed class KarenStarFriendPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 作为一个标记性 Power，不需要实现特殊 Hook
    // 实际奖励效果由 KarenStarFriend 卡牌在击杀时直接处理
}
