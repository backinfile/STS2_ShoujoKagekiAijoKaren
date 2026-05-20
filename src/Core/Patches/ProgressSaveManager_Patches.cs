using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves.Managers;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using System;

namespace ShoujoKagekiAijoKaren.src.KarenMod.Patches;

[HarmonyPatch]
internal class ProgressSaveManager_Patches
{
    private static bool IsKaren(Player localPlayer)
    {
        return localPlayer.Character?.Id.Entry == Karen.CHAR_ID;
    }

    private static string GetCharacterName(Player localPlayer)
    {
        return localPlayer.Character?.GetType().Name ?? "<null>";
    }

    // -------------------------------
    // Patch CheckFifteenElitesDefeatedEpoch
    // -------------------------------
    [HarmonyPatch(typeof(ProgressSaveManager), "CheckFifteenElitesDefeatedEpoch")]
    [HarmonyPatch(new[] { typeof(Player) })]
    private static class ElitesPatch
    {
        private static bool Prefix(ProgressSaveManager __instance, Player localPlayer)
        {
            Console.WriteLine("[Prefix] CheckFifteenElitesDefeatedEpoch started for " +
                              GetCharacterName(localPlayer));
            return !IsKaren(localPlayer); // skip original for Karen
        }

        private static void Postfix(ProgressSaveManager __instance, Player localPlayer)
        {
            Console.WriteLine("[Postfix] CheckFifteenElitesDefeatedEpoch finished for " +
                              GetCharacterName(localPlayer));
        }
    }

    // -------------------------------
    // Patch CheckFifteenBossesDefeatedEpoch
    // -------------------------------
    [HarmonyPatch(typeof(ProgressSaveManager), "CheckFifteenBossesDefeatedEpoch")]
    [HarmonyPatch(new[] { typeof(Player) })]
    private static class BossesPatch
    {
        private static bool Prefix(ProgressSaveManager __instance, Player localPlayer)
        {
            Console.WriteLine("[Prefix] CheckFifteenBossesDefeatedEpoch started for " +
                              GetCharacterName(localPlayer));
            return !IsKaren(localPlayer); // skip original for Karen
        }

        private static void Postfix(ProgressSaveManager __instance, Player localPlayer)
        {
            Console.WriteLine("[Postfix] CheckFifteenBossesDefeatedEpoch finished for " +
                              GetCharacterName(localPlayer));
        }
    }

    [HarmonyPatch(typeof(ProgressSaveManager))]
    [HarmonyPatch("ObtainCharUnlockEpoch")]
    [HarmonyPatch([typeof(Player), typeof(int)])]
    private static class ObtainEpochPatch
    {
        private static bool Prefix(ProgressSaveManager __instance, Player localPlayer, int act)
        {
            Console.WriteLine(
                $"[Prefix] ObtainCharUnlockEpoch started for {GetCharacterName(localPlayer)}, Act {act + 1}");

            // Skip method for Karen or handle custom logic
            return !IsKaren(localPlayer);
        }

        private static void Postfix(ProgressSaveManager __instance, Player localPlayer, int act)
        {
            Console.WriteLine(
                $"[Postfix] ObtainCharUnlockEpoch finished for {GetCharacterName(localPlayer)}, Act {act + 1}");
        }
    }
}
