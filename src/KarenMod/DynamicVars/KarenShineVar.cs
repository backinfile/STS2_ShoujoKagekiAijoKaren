using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace ShoujoKagekiAijoKaren.src.KarenMod.DynamicVars;

/// <summary>
/// KarenShine 动态变量 - 用于"闪耀"关键字
/// 每次打出后减少，到0时卡牌被移除
/// </summary>
public class KarenShineVar : DynamicVar
{
    public const string DefaultName = "KarenShine";

    public KarenShineVar(decimal initialValue)
        : base(DefaultName, initialValue)
    {
    }

    /// <summary>
    /// KarenShine 值不受附魔影响，直接显示基础值
    /// </summary>
    public override void UpdateCardPreview(
        CardModel card,
        CardPreviewMode previewMode,
        Creature target,
        bool runGlobalHooks)
    {
        base.EnchantedValue = base.BaseValue;
        base.PreviewValue = base.BaseValue;
    }
}
