using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace ShoujoKagekiAijoKaren.src.Models.Relics;

/// <summary>Shared resource paths and localized mechanic tips for Karen relics.</summary>
public abstract class KarenBaseRelic : RelicModel
{
    public override string PackedIconPath => $"res://images/relics/{IconBaseName}.png";
    protected override string PackedIconOutlinePath => $"res://images/relics/{IconBaseName}_outline.png";
    protected override string BigIconPath => $"res://images/relics/{IconBaseName}_big.png";

    protected static HoverTip MechanicTip(string key) => new(
        new LocString("card_keywords", key + ".title"),
        new LocString("card_keywords", key + ".description"));
}
