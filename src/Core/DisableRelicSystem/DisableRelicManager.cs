using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem;

/// <summary>
/// 禁用遗物系统管理器
/// 将遗物替换为锁定遗物，在战斗期间使其失效，战斗结束后自动恢复
/// </summary>
public static class DisableRelicManager
{
    /// <summary>
    /// 白名单：不能被禁用的遗物ID前缀列表
    /// </summary>
    public static readonly List<string> WhiteList = new()
    {
        // 可以添加特殊遗物的前缀，如 "loadout:"
    };

    /// <summary>
    /// 禁用指定的遗物（将其替换为锁定遗物）
    /// </summary>
    /// <param name="player">玩家</param>
    /// <param name="position">遗物位置</param>
    /// <returns>是否成功禁用</returns>
    public static bool DisableRelicAtPosition(Player player, int position)
    {
        if (player == null || position < 0 || position >= player.Relics.Count)
            return false;

        var relic = player.Relics[position];

        // 检查是否已经是锁定遗物
        if (relic is KarenLockRelic)
            return false;

        // 跳过BOSS遗物
        if (relic.Rarity == RelicRarity.Ancient)
            return false;

        // 跳过白名单中的遗物
        if (IsInWhiteList(relic))
            return false;

        // 跳过特殊遗物（如融化的蜡质遗物）
        if (relic.IsMelted)
            return false;

        // 移除原始遗物
        player.RemoveRelicInternal(relic, silent: true);

        // 创建锁定遗物
        var lockRelic = new KarenLockRelic { LockedRelic = relic };

        // 在相同位置添加锁定遗物
        player.AddRelicInternal(lockRelic, position, silent: true);

        MainFile.Logger.Info($"[DisableRelicManager] Disabled relic '{relic.Id.Entry}' ({relic.Title}) at position {position} for player {player.NetId}");

        return true;
    }

    /// <summary>
    /// 恢复所有被禁用的遗物
    /// </summary>
    /// <param name="player">玩家</param>
    public static void RestoreAllRelics(Player player)
    {
        if (player == null) return;

        var lockRelics = player.Relics.OfType<KarenLockRelic>().ToList();
        if (lockRelics.Count == 0) return;

        MainFile.Logger.Info($"[DisableRelicManager] Restoring {lockRelics.Count} disabled relics for player {player.NetId}");

        foreach (var lockRelic in lockRelics)
        {
            // 获取当前位置
            int currentPosition = player.Relics.IndexOf(lockRelic);
            if (currentPosition < 0)
            {
                MainFile.Logger.Warn($"[DisableRelicManager] Lock relic position not found");
                continue;
            }

            // 获取原始遗物
            var originalRelic = lockRelic.LockedRelic;
            if (originalRelic == null)
            {
                MainFile.Logger.Warn($"[DisableRelicManager] Original relic is null");
                continue;
            }

            // 移除锁定遗物
            player.RemoveRelicInternal(lockRelic, silent: true);

            // 恢复原始遗物
            player.AddRelicInternal(originalRelic, currentPosition, silent: true);

            MainFile.Logger.Info($"[DisableRelicManager] Restored relic '{originalRelic.Id.Entry}' ({originalRelic.Title}) at position {currentPosition}");
        }
    }

    /// <summary>
    /// 获取可以禁用的遗物位置（从右向左查找）
    /// </summary>
    /// <param name="player">玩家</param>
    /// <param name="endPosition">结束位置（默认为列表末尾）</param>
    /// <returns>可禁用遗物的位置，如果没有则返回-1</returns>
    public static int GetDisableableRelicPosition(Player player, int endPosition = -1)
    {
        if (player == null || player.Relics.Count == 0) return -1;

        if (endPosition < 0) endPosition = player.Relics.Count;

        // 从右向左遍历
        for (int i = endPosition - 1; i >= 0; i--)
        {
            var relic = player.Relics[i];

            // 跳过已经是锁定遗物的
            if (relic is KarenLockRelic)
                continue;

            // 跳过BOSS遗物
            if (relic.Rarity == RelicRarity.Ancient)
                continue;

            // 跳过白名单中的遗物
            if (IsInWhiteList(relic))
                continue;

            // 跳过融化的遗物
            if (relic.IsMelted)
                continue;

            return i;
        }

        return -1;
    }

    /// <summary>
    /// 获取可以禁用的遗物数量
    /// </summary>
    /// <param name="player">玩家</param>
    /// <returns>可禁用的遗物数量</returns>
    public static int GetDisableableRelicCount(Player player)
    {
        if (player == null) return 0;

        return player.Relics.Count(r =>
            r is not KarenLockRelic &&
            r.Rarity != RelicRarity.Ancient &&
            !IsInWhiteList(r) &&
            !r.IsMelted);
    }

    /// <summary>
    /// 获取已被禁用的遗物数量
    /// </summary>
    /// <param name="player">玩家</param>
    /// <returns>已禁用的遗物数量</returns>
    public static int GetDisabledRelicCount(Player player)
    {
        if (player == null) return 0;
        return player.Relics.Count(r => r is KarenLockRelic);
    }

    /// <summary>
    /// 检查遗物是否在白名单中（不能被禁用）
    /// </summary>
    /// <param name="relic">遗物</param>
    /// <returns>是否在白名单中</returns>
    public static bool IsInWhiteList(RelicModel relic)
    {
        if (relic == null) return false;

        string relicId = relic.Id.Entry.ToLowerInvariant();
        return WhiteList.Any(prefix => relicId.Contains(prefix.ToLowerInvariant()));
    }

    /// <summary>
    /// 检查是否有可禁用的遗物
    /// </summary>
    /// <param name="player">玩家</param>
    /// <returns>是否有可禁用的遗物</returns>
    public static bool HasDisableableRelic(Player player)
    {
        return GetDisableableRelicCount(player) > 0;
    }
}
