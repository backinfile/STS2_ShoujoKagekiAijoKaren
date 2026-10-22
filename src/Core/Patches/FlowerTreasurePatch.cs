using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using ShoujoKagekiAijoKaren.src.Models.Relics;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// Two candidates automatically use the native multiplayer chest layout and voting pipeline.
/// Its single player receives the chosen relic; the unchosen candidate is marked skipped.
/// </summary>
[HarmonyPatch(typeof(TreasureRoomRelicSynchronizer), nameof(TreasureRoomRelicSynchronizer.BeginRelicPicking))]
public static class FlowerTreasurePatch
{
    public static void Postfix(TreasureRoomRelicSynchronizer __instance,
        IPlayerCollection ____playerCollection, RelicGrabBag ____sharedGrabBag)
    {
        if (____playerCollection.Players.Count != 1 ||
            __instance.CurrentRelics is not List<RelicModel> { Count: 1 } relics) return;
        var player = ____playerCollection.Players[0];
        var flower = player.Relics.OfType<KarenFlowerRelic>().FirstOrDefault();
        if (flower == null || !KarenFlowerRelic.SupportsChoice(relics[0].Rarity)) return;

        var original = relics[0];
        var alternative = ____sharedGrabBag.PullFromFront(original.Rarity,
            relic => relic.Rarity == original.Rarity && relic.Id != original.Id, player.RunState);
        if (alternative == null || alternative.Rarity != original.Rarity) return;
        relics.Add(alternative);
        flower.Flash();
    }
}
