using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using ShoujoKagekiAijoKaren.src.Core.Audio;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

[HarmonyPatch]
internal static class CombatBgmReplacementRunExitPatch
{
    [HarmonyPatch(typeof(NGame), nameof(NGame.ReturnToMainMenu))]
    [HarmonyPrefix]
    private static void BeforeReturnToMainMenu()
    {
        CombatBgmReplacementManager.StopForRunExit();
    }

    [HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
    [HarmonyPostfix]
    private static void AfterMainMenuReady()
    {
        CombatBgmReplacementManager.StopForRunExit();
    }
}
