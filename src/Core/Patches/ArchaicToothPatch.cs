using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using ShoujoKagekiAijoKaren.src.Models.Cards;
using ShoujoKagekiAijoKaren.src.Models.Characters;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

// BaseLib owns the mapping and native transformation. Keep Karen's fallback-only eligibility.
[HarmonyPatch(typeof(ArchaicTooth), "GetTranscendenceStarterCard")]
internal static class ArchaicToothPatch
{
    private static readonly System.Reflection.PropertyInfo Upgrades = AccessTools.Property(typeof(ArchaicTooth), "TranscendenceUpgrades");
    private static void Postfix(Player player, ref CardModel? __result)
    {
        if (__result is not KarenFall) return;
        var upgrades = (Dictionary<ModelId, CardModel>)Upgrades.GetValue(null)!;
        __result = player.Deck.Cards.FirstOrDefault(card => card is not KarenFall && upgrades.ContainsKey(card.Id))
            ?? (player.Character.Id.Entry == Karen.CHAR_ID ? __result : null);
    }
}
