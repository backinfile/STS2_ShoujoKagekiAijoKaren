using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers.tmpStrength;

/// <summary>
/// 小零食的临时力量效果。
/// </summary>
public sealed class KarenSmallSnackTempStrengthPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<KarenSmallSnack>();

    protected override bool IsPositive => true;
}
