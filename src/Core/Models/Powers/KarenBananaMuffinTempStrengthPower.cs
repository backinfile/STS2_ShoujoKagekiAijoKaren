using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.strength;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// Banana松饼的临时力量效果（本回合）
/// </summary>
public class KarenBananaMuffinTempStrengthPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<KarenBananaMuffin>();

    protected override bool IsPositive => true;
}
