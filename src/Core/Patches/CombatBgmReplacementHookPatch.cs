using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ShoujoKagekiAijoKaren.src.Core.Audio;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

[HarmonyPatch]
internal static class CombatBgmReplacementHookPatch
{
    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
    [HarmonyPostfix]
    private static void AfterCombatEnd(IRunState runState, CombatState? combatState, CombatRoom room)
    {
        CombatBgmReplacementManager.StopForCombatEnd();
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterRoomEntered))]
    [HarmonyPostfix]
    private static void AfterRoomEntered(AbstractRoom room)
    {
        CombatBgmReplacementManager.RestoreGameMusicIfNeeded();
    }
}
