using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;

namespace ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem;

/// <summary>
/// 锁定遗物 - 用于替代被禁用的遗物
/// 战斗结束后会自动恢复为原始遗物
/// </summary>
public sealed class KarenLockRelic : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    /// <summary>
    /// 被锁定的原始遗物
    /// </summary>
    public RelicModel? LockedRelic { get; set; }


    //public override LocString Title => new LocString("relics", "KAREN_LOCK_RELIC.title");


    //public override LocString Flavor => new LocString("relics", "KAREN_LOCK_RELIC.flavor");

    /// <summary>
    /// 提供额外的悬浮提示，显示被锁定的遗物信息（只显示描述，不显示标题）
    /// </summary>
    //protected override IEnumerable<IHoverTip> ExtraHoverTips
    //{
    //    get
    //    {
    //        if (LockedRelic != null)
    //        {
    //            string lockedRelicName = LockedRelic.Title.GetFormattedText();
    //            // 只创建描述，不设置标题
    //            var desc = new LocString("relics", "KAREN_LOCK_RELIC.description");
    //            desc.Add("relicName", lockedRelicName);
    //            return [new HoverTip(desc)];
    //        }
    //        return base.ExtraHoverTips;
    //    }
    //}
}
