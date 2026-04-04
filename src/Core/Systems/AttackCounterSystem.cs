using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ShoujoKagekiAijoKaren.src.Core.Systems;

/// <summary>
/// 统计每回合攻击次数的系统
/// </summary>
public static class AttackCounterSystem
{
    // 每个玩家在每回合的攻击次数 (PlayerId -> 攻击次数)
    private static readonly Dictionary<ulong, int> _attackCounts = new();

    // 记录当前回合标识，用于检测回合变化
    private static int _currentRoundNumber = -1;
    private static CombatSide _currentSide = CombatSide.Player;

    /// <summary>
    /// 获取指定玩家在本回合的攻击次数
    /// </summary>
    public static int GetAttackCount(Player player)
    {
        _attackCounts.TryGetValue(player.NetId, out int count);
        return count;
    }

    /// <summary>
    /// 获取指定生物在本回合的攻击次数
    /// </summary>
    public static int GetAttackCount(Creature creature)
    {
        if (creature.Player == null) return 0;
        return GetAttackCount(creature.Player);
    }

    /// <summary>
    /// 增加攻击次数（由 Patch 调用）
    /// </summary>
    internal static void IncrementAttackCount(Player player)
    {
        var combatState = player.Creature.CombatState;
        if (combatState == null) return;

        // 检查回合是否变化，如果是则重置计数
        CheckAndResetIfNewTurn(combatState);

        // 增加攻击次数
        if (_attackCounts.ContainsKey(player.NetId))
        {
            _attackCounts[player.NetId]++;
        }
        else
        {
            _attackCounts[player.NetId] = 1;
        }
    }

    /// <summary>
    /// 重置指定玩家的攻击计数
    /// </summary>
    internal static void ResetAttackCount(Player player)
    {
        _attackCounts[player.NetId] = 0;
    }

    /// <summary>
    /// 检查是否是新回合，如果是则重置所有计数
    /// </summary>
    internal static void CheckAndResetIfNewTurn(CombatState combatState)
    {
        if (_currentRoundNumber != combatState.RoundNumber || _currentSide != combatState.CurrentSide)
        {
            _currentRoundNumber = combatState.RoundNumber;
            _currentSide = combatState.CurrentSide;
            _attackCounts.Clear();
        }
    }

    /// <summary>
    /// 战斗结束时清理数据
    /// </summary>
    internal static void Clear()
    {
        _attackCounts.Clear();
        _currentRoundNumber = -1;
        _currentSide = CombatSide.Player;
    }
}
