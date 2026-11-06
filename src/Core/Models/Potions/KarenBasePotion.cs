using MegaCrit.Sts2.Core.Models;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Potions;

public abstract class KarenBasePotion : PotionModel
{
    public string ResourceKey => Id.Entry.ToLowerInvariant();

    public string KarenImagePath => $"res://images/potions/{ResourceKey}.png";

    public string KarenOutlinePath => $"res://images/potions/{ResourceKey}_outline.png";
}
