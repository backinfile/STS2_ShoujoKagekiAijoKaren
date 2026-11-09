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
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <summary>
    /// 被锁定的原始遗物
    /// </summary>
    public RelicModel? LockedRelic { get; set; }

}
