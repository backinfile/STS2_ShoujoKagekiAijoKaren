using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.strength;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System.Collections.Generic;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers.tmpStrength;

/// <summary>
/// Banana松饼的临时力量效果（本回合）
/// </summary>
public class KarenTempStrengthPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<KarenSandwitch>();

    protected override bool IsPositive => true;



    // ============  disable extra hover =============

    public override LocString Title => Tips.TempStrengthPowerTitle;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [];
}
