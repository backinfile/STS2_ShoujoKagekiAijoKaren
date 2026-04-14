using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem.Nodes;

namespace ShoujoKagekiAijoKaren.src.Core.ShineSystem.Patches;

[HarmonyPatch(typeof(NTopBar), nameof(NTopBar._Ready))]
public static class NTopBarShinePileButtonPatch
{
    [HarmonyPostfix]
    private static void Postfix(NTopBar __instance)
    {
        MainFile.Logger.Info("[ShinePilePatch] NTopBar._Ready Postfix executing");
        if (__instance.HasNode("RightAlignedStuff/ShinePile"))
        {
            MainFile.Logger.Info("[ShinePilePatch] ShinePile button already exists, skipping");
            return;
        }

        var scene = ResourceLoader.Load<PackedScene>("res://scenes/ui/top_bar/top_bar_shine_pile_button.tscn");
        if (scene == null)
        {
            MainFile.Logger.Error("[ShinePilePatch] Failed to load top_bar_shine_pile_button.tscn");
            return;
        }

        var shinePileButton = scene.Instantiate<NTopBarShinePileButton>();
        shinePileButton.Name = "ShinePile";
        MainFile.Logger.Info($"[ShinePilePatch] Button instantiated, name={shinePileButton.Name}");

        var rightAlignedStuff = __instance.GetNode<Control>("RightAlignedStuff");
        var mapButton = __instance.Map;
        int mapIndex = mapButton.GetIndex();
        rightAlignedStuff.AddChild(shinePileButton);
        rightAlignedStuff.MoveChild(shinePileButton, mapIndex);
        MainFile.Logger.Info($"[ShinePilePatch] Button added to RightAlignedStuff at index {mapIndex}, actual index={shinePileButton.GetIndex()}");

        var capstoneContainer = __instance.GetParent().GetNodeOrNull<Node>("%CapstoneScreenContainer");
        if (capstoneContainer != null)
        {
            capstoneContainer.Connect(Node.SignalName.ChildEnteredTree,
                Callable.From<Node>(_ => shinePileButton.ToggleAnimState()));
            capstoneContainer.Connect(Node.SignalName.ChildExitingTree,
                Callable.From<Node>(_ => shinePileButton.ToggleAnimState()));
            MainFile.Logger.Info("[ShinePilePatch] CapstoneContainer signals connected");
        }
        else
        {
            MainFile.Logger.Warn("[ShinePilePatch] CapstoneContainer not found, anim state won't sync");
        }
    }
}

[HarmonyPatch(typeof(NTopBar), nameof(NTopBar.Initialize))]
public static class NTopBarShinePileInitializePatch
{
    [HarmonyPostfix]
    private static void Postfix(NTopBar __instance, IRunState runState)
    {
        MainFile.Logger.Info("[ShinePilePatch] NTopBar.Initialize Postfix executing");
        var shinePileButton = __instance.GetNodeOrNull<NTopBarShinePileButton>("RightAlignedStuff/ShinePile");
        if (shinePileButton == null)
        {
            MainFile.Logger.Warn("[ShinePilePatch] ShinePile button not found during Initialize");
            return;
        }

        var player = LocalContext.GetMe(runState);
        if (player == null)
        {
            MainFile.Logger.Warn("[ShinePilePatch] LocalContext.GetMe returned null");
            return;
        }

        MainFile.Logger.Info($"[ShinePilePatch] Initializing button for player {player.Character.Id.Entry}");
        shinePileButton.Initialize(player);
    }
}
