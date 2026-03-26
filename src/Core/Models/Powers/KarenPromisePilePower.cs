using System;
using System.Collections.Generic;
using System.Text;
using Godot;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// 约定牌堆计数 Power，显示约定牌堆中的卡牌数量及卡牌列表
/// 支持虚空模式：交互重定向到抽牌堆
/// </summary>
public sealed class KarenPromisePilePower : FakeAmountPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    // ===== Void Mode =====
    private bool _isVoidMode;

    /// <summary>是否处于虚空模式（交互重定向到抽牌堆）</summary>
    public bool IsVoidMode => _isVoidMode;

    /// <summary>切换为虚空模式（永久）</summary>
    public void EnterVoidMode()
    {
        _isVoidMode = true;
        // 隐藏数值显示
        SetCount(0);
    }

    // ===== Icon Switching =====
    private static readonly Lazy<Texture2D> NormalIcon = new(() =>
        GD.Load<Texture2D>("res://ShoujoKagekiAijoKaren/images/powers/karen_promise_pile_power.png"));

    private static readonly Lazy<Texture2D> VoidIcon = new(() =>
        GD.Load<Texture2D>("res://ShoujoKagekiAijoKaren/images/powers/karen_promise_pile_power_void.png"));

    public override Texture2D Icon => _isVoidMode ? VoidIcon.Value : NormalIcon.Value;

    // ===== Title & Description Override =====
    public override LocString Title => _isVoidMode
        ? new LocString("powers", "KAREN_PROMISE_PILE_POWER.voidTitle")
        : base.Title;

    public override LocString Description => _isVoidMode
        ? new LocString("powers", "KAREN_PROMISE_PILE_POWER.voidDescription")
        : base.Description;

    // ===== Normal Mode Data =====
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
            // Void 模式下不显示卡牌列表
            if (_isVoidMode) yield break;

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
