using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// 可显示自定义数值的 Power 基类。
/// 继承此类后，可设置 FakeAmount 来控制 UI 显示的数字，而不受 StackType 限制。
/// 同时继承 KarenBasePower，支持约定牌堆相关扳机。
/// </summary>
public abstract class FakeAmountPower : KarenBasePower
{
    private int _fakeAmount;

    /// <summary>自定义显示数值，独立于 Amount 属性</summary>
    public virtual int FakeAmount => _fakeAmount;

    /// <summary>设置自定义显示数值并触发 UI 刷新</summary>
    protected void SetFakeAmount(int value)
    {
        _fakeAmount = value;
        InvokeDisplayAmountChanged();
    }
}
