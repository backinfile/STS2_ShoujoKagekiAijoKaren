using BaseLib.Abstracts;
using BaseLib.Utils.Attributes;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Models.Relics;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers.tmpStrength;

[CustomID("KAREN_LITTLE_STAR_TEMP_STRENGTH_POWER")]
public sealed class KarenLittleStarTempStrengthPower : TemporaryStrengthPower, ICustomPower
{
    public override AbstractModel OriginModel => ModelDb.Relic<KarenLittleStarRelic>();

    public string CustomPackedIconPath => "res://images/relics/karen_little_star_relic.png";
    public string CustomBigIconPath => "res://images/relics/karen_little_star_relic_big.png";
    public string CustomBigBetaIconPath => CustomBigIconPath;
}
