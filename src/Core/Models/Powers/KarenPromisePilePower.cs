using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers.tmpStrength;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    Burn = 1 << 2,          // 从约定牌堆抽出的牌升级且打出时消耗
    PastAndFuture = 1 << 3, // 从约定牌堆抽牌时获得临时力量
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
    private decimal _pastAndFutureAmount = 0m;

    /// <summary>判断是否处于指定 Mode</summary>
    public bool IsInMode(PromisePileMode mode) => (_activeModes & mode) == mode;

    /// <summary>进入指定 Mode（支持组合）</summary>
    public void EnterMode(PromisePileMode mode)
    {
        _activeModes |= mode;
        SyncModeVfx();
        PlayAni();
    }

    /// <summary>退出指定 Mode</summary>
    public void ExitMode(PromisePileMode mode)
    {
        _activeModes &= ~mode;
        SyncModeVfx();
    }

    // ===== 便捷属性 =====
    public bool IsVoidMode => IsInMode(PromisePileMode.Void);
    public bool IsInfiniteReinforcement => IsInMode(PromisePileMode.InfiniteReinforcement);
    public bool IsBurnMode => IsInMode(PromisePileMode.Burn);
    public bool IsPastAndFutureMode => IsInMode(PromisePileMode.PastAndFuture);
    public decimal PastAndFutureAmount => _pastAndFutureAmount;

    public void AddPastAndFutureAmount(decimal amount)
    {
        _pastAndFutureAmount += amount;
    }

    public void ClearPastAndFutureAmount()
    {
        _pastAndFutureAmount = 0m;
    }

    // ===== Normal Mode Data =====
    private IReadOnlyList<string> _cardNames = Array.Empty<string>();

    /// <summary>当约定牌堆数量为0时不显示数字</summary>
    public override bool ShowFakeAmount => FakeAmount > 0 && !IsVoidMode && !IsInfiniteReinforcement;

    /// <summary>更新约定牌堆卡牌名列表（升级牌名带 +）</summary>
    public void UpdateCount()
    {
        if (Owner.Player is Player player)
        {
            var pile = PromisePileManager.GetPromisePile(player);
            var count = pile.Cards.Count;
            SetFakeAmount(count);
            _cardNames = pile.Cards.Select(c => c.Title).ToArray();
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
        if (IsInMode(PromisePileMode.Burn))
            modes.Add(Tips.PromisePilePowerModeBurn.GetFormattedText());
        if (IsInMode(PromisePileMode.PastAndFuture))
            modes.Add(LocManager.Instance.SmartFormat(Tips.PromisePilePowerModePastAndFuture, new Dictionary<string, object> { ["Amount"] = PastAndFutureAmount }));

        return string.Join("\n", modes);
    }

    private void SyncModeVfx()
    {
        if (Owner.Player is not Player player) return;

        if (IsBurnMode)
            KarenBurnVfxManager.Start(player);
        else
            KarenBurnVfxManager.Stop(player);

        if (IsPastAndFutureMode)
            KarenPastAndFutureRingVfxManager.Start(player);
        else
            KarenPastAndFutureRingVfxManager.Stop(player);
    }


    public static void AddBurnEffect(CardModel card)
    {
        CardCmd.Upgrade(card);
        if (!card.Keywords.Contains(CardKeyword.Exhaust))
        {
            card.AddKeyword(CardKeyword.Exhaust);
        }
        //card.ExhaustOnNextPlay = true;
    }

    public void PlayAni()
    {
        Owner?.InvokePowerModified(this, 1, false);
    }

    public override async Task OnCardRemovedFromPromisePile(CardModel card)
    {
        if (IsInMode(PromisePileMode.Burn))
        {
            AddBurnEffect(card);
        }
        if (IsInMode(PromisePileMode.PastAndFuture))
        {
            await PowerCmd.Apply<KarenPastAndFutureTempStrengthPower>(Owner, _pastAndFutureAmount, Owner, null);
        }
    }
}
