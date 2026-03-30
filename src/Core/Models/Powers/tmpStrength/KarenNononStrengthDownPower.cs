using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Models.Cards;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers.tmpStrength;

/// <summary>
/// NONNON哒哟的临时力量下降效果（本回合）
/// </summary>
public class KarenNononStrengthDownPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<KarenNonon>();

    protected override bool IsPositive => false;
}
