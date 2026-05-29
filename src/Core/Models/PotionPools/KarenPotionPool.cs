using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using ShoujoKagekiAijoKaren.src.Core.Models.Potions;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.Models.PotionPools;

public sealed class KarenPotionPool : PotionPoolModel
{
    public override string EnergyColorName => "karen";

    public override Color LabOutlineColor => new("FB5458");

    protected override IEnumerable<PotionModel> GenerateAllPotions()
    {
        return
        [
            ModelDb.Potion<KarenShinePotion>(),
        ];
    }

    public override IEnumerable<PotionModel> GetUnlockedPotions(UnlockState unlockState)
    {
        return AllPotions.ToList();
    }
}
