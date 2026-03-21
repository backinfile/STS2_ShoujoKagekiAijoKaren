using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Runs;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Patches;

/// <summary>
/// 约定牌堆战斗生命周期 Patch
/// - 战斗开始时清空（安全保障）
/// - 战斗结束时清空（将滞留卡牌从 CombatState 注销）
/// - 每回合结束时打印约定牌堆内容
/// </summary>

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCombatStart))]
internal static class PromisePile_BeforeCombatStart_Patch
{
    [HarmonyPostfix]
    private static void Postfix(IRunState runState, CombatState? combatState)
    {
        if (combatState == null) return;
        foreach (var player in combatState.Players)
        {
            PromisePileManager.ClearPromisePile(player);
            // 战斗开始时为华恋角色初始化 Power
            _ = PromisePileManager.InitPowerAsync(player);
        }
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

/// <summary>
/// 每回合结束时打印约定牌堆内容（调试用）
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterTurnEnd))]
internal static class PromisePile_AfterTurnEnd_Patch
{
    [HarmonyPostfix]
    private static void Postfix(CombatState combatState, CombatSide side)
    {
        foreach (var player in combatState.Players)
        {
            var pile = PromisePileManager.GetPromisePile(player);
            if (pile.Count > 0)
            {
                var cards = string.Join(", ", pile.Select(c => $"'{c.Title}'"));
                MainFile.Logger.Info($"[PromisePile] Turn end - {pile.Count} card(s): {cards}");
            }
            else
            {
                MainFile.Logger.Info($"[PromisePile] Turn end - empty");
            }
        }
    }
}
