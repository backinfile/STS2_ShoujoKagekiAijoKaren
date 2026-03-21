using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Runs;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Patches;

/// <summary>
/// 约定牌堆战斗生命周期 Patch
/// - 战斗开始时清空（安全保障）
/// - 战斗结束时清空（将滞留卡牌从 CombatState 注销）
/// </summary>

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCombatStart))]
internal static class PromisePile_BeforeCombatStart_Patch
{
    [HarmonyPostfix]
    private static void Postfix(IRunState runState, CombatState? combatState)
    {
        if (combatState == null) return;
        foreach (var player in combatState.Players)
            PromisePileManager.ClearPromisePile(player);
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.AfterCombatEnd))]
internal static class PromisePile_AfterCombatEnd_Patch
{
    [HarmonyPostfix]
    private static void Postfix(Player __instance)
    {
        PromisePileManager.ClearPromisePile(__instance);
    }
}
