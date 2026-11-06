using System;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.Models.Characters;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>Start a visual cue before room hooks without changing their Task or native timing.</summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterRoomEntered))]
internal static class KarenCombatEntrancePatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void Prefix(AbstractRoom room)
    {
        if (NonInteractiveMode.IsActive || DisplayServer.GetName() == "headless"
            || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant
            || room is not CombatRoom { IsPreFinished: false } combat
            || NCombatRoom.Instance is not { Mode: CombatRoomMode.ActiveCombat } view
            || !combat.CombatState.Players.Any(p => p.Character is Karen && LocalContext.IsMe(p))) return;
        try
        {
            NKarenCombatEntranceVfx.Play(view, combat.CombatState.Players.Where(p => p.Character is Karen));
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[Karen Entrance] Could not create visual cue: {ex}");
        }
    }
}
