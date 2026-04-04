using System.Collections.Generic;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// 统计每回合攻击次数的 Patch
/// </summary>
public static class AttackCounter
{
    // 每个玩家在每回合的攻击次数
    public static readonly SpireField<Player, int> AttackCounts = new(() => 0);

    /// <summary>
    /// 获取玩家在本回合的攻击次数
    /// </summary>
    public static int GetAttackCount(Player player)
    {
        return AttackCounts.Get(player);
    }
}

/// <summary>
/// 在玩家回合开始时重置攻击计数
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeSideTurnStart))]
public class AttackCounterResetPatch
{
    private static void Prefix(CombatState combatState, CombatSide side)
    {
        // 只在玩家回合开始时重置
        if (side == CombatSide.Player)
        {
            foreach (var player in combatState.Players)
            {
                AttackCounter.AttackCounts[player] = 0;
            }
        }
    }
}

/// <summary>
/// 统计攻击次数
/// </summary>
[HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute))]
public class AttackCounterPatch
{
    private static void Prefix(AttackCommand __instance)
    {
        var attacker = __instance.Attacker;
        // 只统计玩家攻击
        if (attacker?.Player is Player player)
        {
            AttackCounter.AttackCounts[player] = AttackCounter.AttackCounts[player] + 1;
        }
    }
}
