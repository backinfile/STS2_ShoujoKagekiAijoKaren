using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Models.Relics;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>Show Mr. White above the native end-turn button while its choice is pending.</summary>
[HarmonyPatch(typeof(NEndTurnButton))]
internal static class KarenMrWhiteEndTurnPatch
{
    private const string MarkerName = "KarenMrWhiteMarker";

    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var method in new[]
                 {
                     nameof(NEndTurnButton.Initialize), nameof(NEndTurnButton.RefreshEnabled),
                     "OnCombatStateChanged", "AfterPlayerEndedTurn", "AfterPlayerUnendedTurn"
                 })
            yield return AccessTools.DeclaredMethod(typeof(NEndTurnButton), method);
    }

    private static void Postfix(NEndTurnButton __instance, CombatState? ____combatState)
    {
        var visuals = __instance.GetNode<Control>("Visuals");
        var marker = visuals.GetNodeOrNull<TextureRect>(MarkerName);
        var player = ____combatState?.Players.FirstOrDefault(LocalContext.IsMe);
        var relic = player?.Relics.OfType<KarenMrWhiteRelic>().FirstOrDefault();
        var visible = __instance.IsEnabled && CombatManager.Instance.IsInProgress
            && ____combatState?.CurrentSide == CombatSide.Player
            && player is { Creature.IsAlive: true } && relic is { WillTriggerAtTurnEnd: true }
            && !CombatManager.Instance.IsPlayerReadyToEndTurn(player);

        if (marker == null)
        {
            if (!visible) return;
            marker = new TextureRect
            {
                Name = MarkerName,
                Texture = relic!.Icon,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                FocusMode = Control.FocusModeEnum.None,
                AnchorLeft = 0.5f, AnchorRight = 0.5f,
                AnchorTop = 0f, AnchorBottom = 0f,
                OffsetLeft = -24f, OffsetRight = 24f,
                OffsetTop = -24f, OffsetBottom = 24f
            };
            visuals.AddChild(marker);
        }
        marker.Visible = visible;
    }
}

[HarmonyPatch(typeof(Player))]
internal static class KarenMrWhiteRelicChangedPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.DeclaredMethod(typeof(Player), nameof(Player.AddRelicInternal));
        yield return AccessTools.DeclaredMethod(typeof(Player), nameof(Player.RemoveRelicInternal));
    }

    private static void Postfix(Player __instance, RelicModel relic)
    {
        if (relic is KarenMrWhiteRelic && LocalContext.IsMe(__instance)
            && NCombatRoom.Instance?.Ui.EndTurnButton is { } button)
            button.RefreshEnabled();
    }
}
