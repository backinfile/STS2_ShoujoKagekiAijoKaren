using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

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
/// 统计攻击次数（在 Hook.BeforeDamageReceived 中计数，AOE 每命中一个敌人各计一次）
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeDamageReceived))]
public class AttackCounterPatch
{
    private static void Prefix(Creature? dealer, CardModel? cardSource, ValueProp props)
    {
        // 只统计玩家通过卡牌发起的攻击
        if (dealer?.Player is Player player && cardSource != null && props == ValueProp.Move)
        {
            AttackCounter.AttackCounts[player] = AttackCounter.AttackCounts[player] + 1;
        }
    }
}
