using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Patches;

/// <summary>
/// Promise pile combat lifecycle patch.
/// - Clear all promise piles when combat begins.
/// - Clear any remaining promise pile cards when combat ends.
/// - Print promise pile contents at the end of each player turn.
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterRoomEntered))]
internal static class PromisePile_BeforeCombatStart_Patch
{
    [HarmonyPostfix]
    private static void Postfix(AbstractRoom room, ref Task __result)
    {
        if (room is not CombatRoom combatRoom) return;

        Async.Postfix(ref __result, () => OnCombatStart(combatRoom.CombatState));
    }

    private static async Task OnCombatStart(CombatState? combatState)
    {
        if (combatState == null) return;

        foreach (var player in combatState.Players)
        {
            PromisePileManager.ClearPromisePileInternal(player);

            if (player.Character?.Id.Entry == Karen.CHAR_ID)
            {
                await PromisePileManager.InitPowerAsync(player);
            }
        }
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.AfterCombatEnd))]
internal static class PromisePile_AfterCombatEnd_Patch
{
    [HarmonyPostfix]
    private static void Postfix(Player __instance)
    {
        MainFile.Logger.Info($"[PromisePile] AfterCombatEnd Patch triggered for player {__instance.Character?.Id?.Entry}");
        PromisePileManager.ClearPromisePileInternal(__instance);
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeSideTurnStart))]
internal static class PromisePile_BeforeSideTurnStart_Patch
{
    [HarmonyPrefix]
    private static void Prefix(CombatState combatState, CombatSide side)
    {
        if (side != CombatSide.Player) return;

        foreach (var player in combatState.Players)
        {
            PastAndFuturePromisePileAudio.Reset(player);
        }
    }
}

/// <summary>
/// Print promise pile contents at the end of each player turn and trigger promise pile turn-end hooks.
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterTurnEnd))]
internal static class PromisePile_AfterTurnEnd_Patch
{
    [HarmonyPostfix]
    public static void Postfix(CombatState combatState, CombatSide side, ref Task __result)
    {
        Async.Postfix(ref __result, async () =>
        {
            if (side != CombatSide.Player) return;

            var player = combatState.Players.FirstOrDefault(p => p.Character?.Id.Entry == Karen.CHAR_ID);
            if (player == null)
            {
                MainFile.Logger.Warn("[PromisePile] Failed to find Karen player for turn end trigger.");
                return;
            }

            await PromisePileHooks.TriggerPromisePileTurnEnd(player);
            PrintSomething(player);
        });
    }

    private static void PrintSomething(Player player)
    {
        MainFile.Logger.Info("[PromisePile] Current promise pile contents at turn end:");
        var pile = KarenCustomEnum.PromisePile.GetPile(player);
        foreach (var card in pile.Cards)
        {
            MainFile.Logger.Info($"[PromisePile] Card in promise pile: {card.Title}");
        }

        MainFile.Logger.Info("[PromisePile] Current hand contents at turn end:");
        var hand = PileType.Hand.GetPile(player);
        foreach (var card in hand.Cards)
        {
            MainFile.Logger.Info($"[PromisePile] Card in hand: {card.Title}");
        }
    }
}
