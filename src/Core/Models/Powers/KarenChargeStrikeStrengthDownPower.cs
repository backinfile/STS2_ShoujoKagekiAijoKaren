using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Models.Cards;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// 蓄力打击的临时力量下降效果（本回合）
/// </summary>
public class KarenChargeStrikeStrengthDownPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<KarenChargeStrike>();

    protected override bool IsPositive => false;
}
