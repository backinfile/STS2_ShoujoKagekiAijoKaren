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
        ];
    }

    public override IEnumerable<RelicModel> GetUnlockedRelics(UnlockState unlockState)
    {
        return AllRelics.ToList();
    }
}
