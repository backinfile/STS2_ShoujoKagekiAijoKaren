using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers.tmpStrength;

/// <summary>
/// 过去与未来的临时力量效果
/// </summary>
public sealed class KarenPastAndFutureTempStrengthPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<KarenPastAndFuture>();

    protected override bool IsPositive => true;
}
