using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using ShoujoKagekiAijoKaren.src.Models.Relics;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Models.RelicPools;

public sealed class KarenRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "karen";

    public override Color LabOutlineColor => StsColors.purple;

    protected override IEnumerable<RelicModel> GenerateAllRelics()
    {
        return
        [
            ModelDb.Relic<KarenHairpinRelic>(),
            ModelDb.Relic<KarenHairpin2Relic>(),
            ModelDb.Relic<KarenWaterBottleRelic>(),
            ModelDb.Relic<KarenCookieRelic>(),
            ModelDb.Relic<KarenStarHairclipRelic>(),
            ModelDb.Relic<KarenBaseballCatRelic>(),
            ModelDb.Relic<KarenMrWhiteRelic>(),
            ModelDb.Relic<KarenLittleStarRelic>(),
            ModelDb.Relic<KarenStarTiaraRelic>(),
        ];
    }

    public override IEnumerable<RelicModel> GetUnlockedRelics(UnlockState unlockState)
    {
        return AllRelics.ToList();
    }
}
