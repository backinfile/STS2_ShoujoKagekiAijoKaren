using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// 约定牌堆计数 Power
/// 显示玩家约定牌堆中的卡牌数量（Amount 始终为 1，使用 DisplayAmount 显示实际数值）
/// </summary>
public sealed class KarenPromisePilePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    // Single 类型确保只存在一个实例，Amount 始终为 1
    public override PowerStackType StackType => PowerStackType.Single;

    // 内部数据存储真实数值
    private class Data
    {
        public int RealCount;
    }

    protected override object InitInternalData() => new Data();

    /// <summary>覆盖显示数值，返回约定牌堆实际数量</summary>
    public override int DisplayAmount => GetInternalData<Data>().RealCount;

    /// <summary>设置真实数值并刷新 UI</summary>
    public void SetRealCount(int count)
    {
        GetInternalData<Data>().RealCount = count;
        InvokeDisplayAmountChanged();
    }
}
