using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem.Patches;

/// <summary>
/// 战斗结束后自动恢复被禁用的遗物
/// TODO 需要在存档前修复被禁用的遗物状态，以免存档后永久丢失遗物
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
public static class DisableRelicRestorePatch
{
    [HarmonyPostfix]
    private static void Postfix(IRunState runState, CombatState combatState, AbstractRoom room)
    {
        // 恢复所有玩家的被禁用遗物
        foreach (var player in combatState.Players)
        {
            if (DisableRelicManager.GetDisabledRelicCount(player) > 0)
            {
                MainFile.Logger.Info($"[DisableRelicRestorePatch] Restoring disabled relics for player {player.NetId} after combat");
                DisableRelicManager.RestoreAllRelics(player);
            }
        }
    }
}

/// <summary>
/// 生物死亡时也恢复被禁用的遗物（备用机制）
/// 主要恢复机制是 AfterCombatEnd，但死亡时提前恢复以防万一
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterDeath))]
public static class DisableRelicOnDeathPatch
{
    [HarmonyPostfix]
    private static void Postfix(IRunState runState, CombatState? combatState, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        // 只有玩家角色死亡时才恢复遗物
        if (creature?.Player == null) return;

        var player = creature.Player;
        if (DisableRelicManager.GetDisabledRelicCount(player) > 0)
        {
            MainFile.Logger.Info($"[DisableRelicOnDeathPatch] Restoring disabled relics for player {player.NetId} on death");
            DisableRelicManager.RestoreAllRelics(player);
        }
    }
}
