using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// 约定牌堆 Mode 枚举，支持 Flags 组合
/// </summary>
[Flags]
public enum PromisePileMode
{
    None = 0,
    Void = 1 << 0,                   // 虚空模式：交互重定向到抽牌堆
    InfiniteReinforcement = 1 << 1,  // 无限强化：约定牌堆始终为10张续演
    UpgradeOnDraw = 1 << 2,          // 抽出时升级：从约定牌堆抽出的牌自动升级
    ExhaustOnPlay = 1 << 3,          // 打出时消耗：从约定牌堆抽出的牌打出时消耗
}

/// <summary>
/// 约定牌堆计数 Power，显示约定牌堆中的卡牌数量及卡牌列表
/// 支持多种 Mode：Void、InfiniteReinforcement、UpgradeOnDraw、ExhaustOnPlay
/// Mode 可以同时激活
/// </summary>
public sealed class KarenPromisePilePower : FakeAmountPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    // ===== Mode 管理 =====
    private PromisePileMode _activeModes = PromisePileMode.None;

    /// <summary>判断是否处于指定 Mode</summary>
    public bool IsInMode(PromisePileMode mode) => (_activeModes & mode) == mode;

    /// <summary>进入指定 Mode（支持组合）</summary>
    public void EnterMode(PromisePileMode mode)
    {
        _activeModes |= mode;

        // Void 模式特殊处理：隐藏数值
        if ((mode & PromisePileMode.Void) != 0)
            SetCount(0);
    }

    /// <summary>退出指定 Mode</summary>
    public void ExitMode(PromisePileMode mode) => _activeModes &= ~mode;

    // ===== 便捷属性 =====
    public bool IsVoidMode => IsInMode(PromisePileMode.Void);
    public bool IsInfiniteReinforcement => IsInMode(PromisePileMode.InfiniteReinforcement);
    public bool IsUpgradeOnDraw => IsInMode(PromisePileMode.UpgradeOnDraw);
    public bool IsExhaustOnPlay => IsInMode(PromisePileMode.ExhaustOnPlay);

    // TODO 切换ICON
    //public override Texture2D Icon => IsVoidMode ? VoidIcon.Value : NormalIcon.Value;


    // ===== Normal Mode Data =====
    private IReadOnlyList<string> _cardNames = Array.Empty<string>();

    public void SetCount(int count) => SetFakeAmount(count);

    /// <summary>更新约定牌堆卡牌名列表（升级牌名带 +）</summary>
    public void UpdateCardNames()
    {
        if (Owner.Player is Player player)
        {
            _cardNames = PromisePileManager.GetPromisePile(player).Cards
                .Select(c => c.Title).ToArray();
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            // 显示当前激活的强化模式
            if (_activeModes != PromisePileMode.None)
            {
                yield return new HoverTip(
                    Tips.PromisePilePowerCurrentModes,
                    GetModeDescription()
                );
            }

            // Void 模式和 Infinite 模式下不显示卡牌列表
            if (IsVoidMode || IsInfiniteReinforcement)
                yield break;

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
                Tips.PromisePilePowerPileContents,
                sb.ToString()
            );
        }
    }

    /// <summary>获取当前激活模式的描述文本</summary>
    private string GetModeDescription()
    {
        var modes = new List<string>();

        if (IsInMode(PromisePileMode.Void))
            modes.Add(Tips.PromisePilePowerModeVoid.GetFormattedText());
        if (IsInMode(PromisePileMode.InfiniteReinforcement))
            modes.Add(Tips.PromisePilePowerModeInfinite.GetFormattedText());
        if (IsInMode(PromisePileMode.UpgradeOnDraw))
            modes.Add(Tips.PromisePilePowerModeUpgrade.GetFormattedText());
        if (IsInMode(PromisePileMode.ExhaustOnPlay))
            modes.Add(Tips.PromisePilePowerModeExhaust.GetFormattedText());

        return string.Join("\n", modes);
    }
}
