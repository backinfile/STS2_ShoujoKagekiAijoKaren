using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
/// 统计攻击次数（按实际造成的伤害段数计算，一段加一次）
/// </summary>
[HarmonyPatch]
public class AttackCounterPatch
{
    private static System.Reflection.MethodInfo TargetMethod()
    {
        return AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.Damage), new[] {
            typeof(PlayerChoiceContext),
            typeof(IEnumerable<Creature>),
            typeof(decimal),
            typeof(ValueProp),
            typeof(Creature),
            typeof(CardModel)
        });
    }

    private static void Prefix(Creature? dealer, CardModel? cardSource)
    {
        // 只统计玩家通过卡牌发起的攻击（AttackCommand 的每一段会调用一次 CreatureCmd.Damage）
        if (dealer?.Player is Player player && cardSource != null)
        {
            AttackCounter.AttackCounts[player] = AttackCounter.AttackCounts[player] + 1;
        }
    }
}
