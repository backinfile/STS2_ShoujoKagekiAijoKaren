using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.strength;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers.tmpStrength;

/// <summary>
/// 一起吃饭吧的临时力量效果（本回合）
/// </summary>
public class KarenEatTogetherTempStrengthPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<KarenEatTogether>();

    protected override bool IsPositive => true;
}
