using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Localization;

namespace ShoujoKagekiAijoKaren.src.Core.Utils;

/// <summary>
/// 统一存放所有本地化字符串，便于管理和复用
/// </summary>
public static class Tips
{
    // ==================== card_selection ====================
    public static readonly LocString SelectFromDrawToHand = new("card_selection", "KAREN_SELECT_FROM_DRAW_TO_HAND");

    // ==================== gameplay_ui ====================
    /// <summary>约定牌堆查看界面底部标题</summary>
    public static readonly LocString PromisePileInfo = new("gameplay_ui", "KAREN_PROMISE_PILE_INFO");

    /// <summary>从约定牌堆选择抽取的提示</summary>
    public static readonly LocString PromisePileSelectDraw = new("gameplay_ui", "KAREN_PROMISE_PILE_SELECT_DRAW");

    /// <summary>从弃牌堆选择放入约定牌堆的提示</summary>
    public static readonly LocString PromisePileSelectFromDiscard = new("gameplay_ui", "KAREN_PROMISE_PILE_SELECT_FROM_DISCARD");

    /// <summary>从抽牌堆选择放入约定牌堆的提示</summary>
    public static readonly LocString PromisePileSelectFromDraw = new("gameplay_ui", "KAREN_PROMISE_PILE_SELECT_FROM_DRAW");

    /// <summary>从手牌选择放入约定牌堆的提示</summary>
    public static readonly LocString PromisePileSelectFromHand = new("gameplay_ui", "KAREN_PROMISE_PILE_SELECT_FROM_HAND");

    /// <summary>闪耀标签前缀</summary>
    public static readonly LocString ShineLabel = new("gameplay_ui", "KAREN_SHINE_LABEL");

    /// <summary>闪耀标签后缀</summary>
    public static readonly LocString ShineSuffix = new("gameplay_ui", "KAREN_SHINE_SUFFIX");

    /// <summary>耗尽牌堆唯一计数标题</summary>
    public static readonly LocString DisposedPileUniqueCountTitle = new("gameplay_ui", "KAREN_DISPOSED_PILE_UNIQUE_COUNT.title");

    /// <summary>耗尽牌堆唯一计数描述第一部分</summary>
    public static readonly LocString DisposedPileUniqueCountDesc0 = new("gameplay_ui", "KAREN_DISPOSED_PILE_UNIQUE_COUNT.description.part0");

    /// <summary>耗尽牌堆唯一计数描述第二部分</summary>
    public static readonly LocString DisposedPileUniqueCountDesc1 = new("gameplay_ui", "KAREN_DISPOSED_PILE_UNIQUE_COUNT.description.part1");

    // ==================== powers ====================
    /// <summary>临时力量 Power 标题</summary>
    public static readonly LocString TempStrengthPowerTitle = new("powers", "KAREN_TEMP_STRENGTH_POWER.title");

    /// <summary>约定牌堆 Power - 当前强化标题</summary>
    public static readonly LocString PromisePilePowerCurrentModes = new("powers", "KAREN_PROMISE_PILE_POWER.currentModes");

    /// <summary>约定牌堆 Power - 牌堆内容标题</summary>
    public static readonly LocString PromisePilePowerPileContents = new("powers", "KAREN_PROMISE_PILE_POWER.pileContents");

    /// <summary>约定牌堆 Power - Void 模式描述</summary>
    public static readonly LocString PromisePilePowerModeVoid = new("powers", "KAREN_PROMISE_PILE_POWER.mode.void");

    /// <summary>约定牌堆 Power - 无限强化模式描述</summary>
    public static readonly LocString PromisePilePowerModeInfinite = new("powers", "KAREN_PROMISE_PILE_POWER.mode.infinite");

    /// <summary>约定牌堆 Power - 升级模式描述</summary>
    public static readonly LocString PromisePilePowerModeUpgrade = new("powers", "KAREN_PROMISE_PILE_POWER.mode.upgrade");

    /// <summary>约定牌堆 Power - 消耗模式描述</summary>
    public static readonly LocString PromisePilePowerModeExhaust = new("powers", "KAREN_PROMISE_PILE_POWER.mode.exhaust");
}
