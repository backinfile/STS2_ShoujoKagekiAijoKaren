using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using ShoujoKagekiAijoKaren.src.Models.Characters;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

[HarmonyPatch(typeof(NCharacterSelectScreen))]
public static class NCharacterSelectKarenVideoPatch
{
    [HarmonyPatch(nameof(NCharacterSelectScreen.SelectCharacter))]
    [HarmonyPostfix]
    private static void SelectCharacterPostfix(NCharacterSelectScreen __instance, CharacterModel characterModel)
    {
        SetInfoPanelBackgroundVisible(__instance, characterModel is not Karen);
        KarenCharSelectVideoController.HandleCharacterSelected(__instance, characterModel);
    }

    [HarmonyPatch("OnSubmenuOpened")]
    [HarmonyPostfix]
    private static void OnSubmenuOpenedPostfix()
    {
        KarenCharSelectVideoController.ResetCooldown();
        KarenCharSelectVideoController.Stop(immediatelyRestoreMusic: true);
    }

    [HarmonyPatch("OnSubmenuClosed")]
    [HarmonyPrefix]
    private static void OnSubmenuClosedPrefix()
    {
        KarenCharSelectVideoController.Stop(immediatelyRestoreMusic: true);
    }

    [HarmonyPatch("OnEmbarkPressed")]
    [HarmonyPrefix]
    private static void OnEmbarkPressedPrefix()
    {
        KarenCharSelectVideoController.Stop(immediatelyRestoreMusic: true);
    }

    [HarmonyPatch(nameof(NCharacterSelectScreen.BeginRun))]
    [HarmonyPrefix]
    private static void BeginRunPrefix()
    {
        KarenCharSelectVideoController.Stop(immediatelyRestoreMusic: true);
    }

    private static void SetInfoPanelBackgroundVisible(NCharacterSelectScreen screen, bool visible)
    {
        if (screen.GetNodeOrNull<CanvasItem>("InfoPanel/NinePatchRect") is { } background)
            background.Visible = visible;
    }
}
