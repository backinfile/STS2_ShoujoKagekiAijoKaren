using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using ShoujoKagekiAijoKaren.src.Models.Characters;

// Optional test-plugin entry: MCP console commands require a run, but this
// existing regression must execute in character selection before a run exists.
[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
internal static class MenuSmoke
{
    private static bool _requested;
    private static void Postfix(NCharacterSelectScreen __instance, CharacterModel __1)
    {
        string? result = System.Environment.GetEnvironmentVariable("KAREN_MENU_SMOKE_RESULT");
        if (_requested || __1 is not Karen || string.IsNullOrEmpty(result)) return;
        _requested = true;
        _ = Check(__instance, result);
    }
    private static async Task Check(NCharacterSelectScreen screen, string result)
    {
        string status;
        try
        {
            await screen.ToSignal(screen.GetTree(), SceneTree.SignalName.ProcessFrame);
            await ReviewFixesCmd.Run(null, "menu");
            status = ReviewFixesCmd.Status;
        }
        catch (Exception error) { status = "fail:menu:" + error.Message; }
        File.WriteAllText(result + ".tmp", status);
        File.Move(result + ".tmp", result, true);
    }
}
