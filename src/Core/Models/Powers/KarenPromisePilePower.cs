using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// 约定牌堆计数 Power，显示约定牌堆中的卡牌数量
/// </summary>
public sealed class KarenPromisePilePower : FakeAmountPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public void SetCount(int count) => SetFakeAmount(count);
}
