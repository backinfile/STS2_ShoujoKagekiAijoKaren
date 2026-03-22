using System;
using System.Collections.Generic;
using System.Text;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// 约定牌堆计数 Power，显示约定牌堆中的卡牌数量及卡牌列表
/// </summary>
public sealed class KarenPromisePilePower : FakeAmountPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    private IReadOnlyList<string> _cardNames = Array.Empty<string>();

    public void SetCount(int count) => SetFakeAmount(count);

    /// <summary>更新约定牌堆卡牌名列表（升级牌名带 +）</summary>
    public void SetCardNames(IReadOnlyList<string> names)
    {
        _cardNames = names;
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            if (_cardNames.Count == 0) yield break;

            var sb = new StringBuilder();
            int shown = Math.Min(_cardNames.Count, 10);
            for (int i = 0; i < shown; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(_cardNames[i]);
            }
            if (_cardNames.Count > 10)
                sb.Append("\n...");

            yield return new HoverTip(
                new LocString("powers", "KAREN_PROMISE_PILE_POWER.pileContents"),
                sb.ToString()
            );
        }
    }
}
