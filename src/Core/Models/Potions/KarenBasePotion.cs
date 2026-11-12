using MegaCrit.Sts2.Core.Models;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Potions;

public abstract class KarenBasePotion : BaseLib.Abstracts.CustomPotionModel
{
    protected KarenBasePotion() : base(autoAdd: false) { }
    public override string CustomPackedImagePath => KarenImagePath;
    public override string CustomPackedOutlinePath => KarenOutlinePath;
    public string ResourceKey => Id.Entry.ToLowerInvariant();

    public string KarenImagePath => $"res://images/potions/{ResourceKey}.png";

    public string KarenOutlinePath => $"res://images/potions/{ResourceKey}_outline.png";
}
