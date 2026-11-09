using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem.Vfx;
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
    private static readonly System.Reflection.PropertyInfo Removed = HarmonyLib.AccessTools.Property(typeof(RelicModel), nameof(RelicModel.HasBeenRemovedFromState));
    private static void ReplaceRelic(Player player, RelicModel previous, RelicModel current, int position)
    {
        bool currentWasRemoved = current.HasBeenRemovedFromState;
        try
        {
            player.RemoveRelicInternal(previous, silent: true);
            Removed.SetValue(current, false);
            player.AddRelicInternal(current, position, silent: true);
        }
        catch
        {
            if (player.Relics.Contains(current)) player.RemoveRelicInternal(current, silent: true);
            Removed.SetValue(current, currentWasRemoved);
            Removed.SetValue(previous, false);
            if (!player.Relics.Contains(previous)) player.AddRelicInternal(previous, position, silent: true);
            DisableRelicNodeManager.Rebuild(player);
            throw;
        }
        DisableRelicNodeManager.Replace(previous, current, player);
    }

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
    public static bool DisableRelicAtPosition(Player player, int position, NDisableRelicAbsorbVfx.TargetKind target = NDisableRelicAbsorbVfx.TargetKind.Player)
    {
        if (player == null || position < 0 || position >= player.Relics.Count)
            return false;

        var relic = player.Relics[position];

        if (!IsRelicLockable(relic))
        {
            MainFile.Logger.Warn($"[DisableRelicManager] Relic '{relic.Id.Entry}' ({relic.Title}) at position {position} is not lockable and cannot be disabled.");
            return false;
        }

        var relicHolder = DisableRelicNodeManager.FindRelicHolder(relic, player);
        Godot.Vector2? relicPosition = null;
        if (relicHolder != null)
            relicPosition = relicHolder.GetGlobalTransformWithCanvas() * (relicHolder.Size * 0.5f);

        var lockRelic = (KarenLockRelic)ModelDb.Relic<KarenLockRelic>().MutableClone();
        lockRelic.LockedRelic = relic;
        ReplaceRelic(player, relic, lockRelic, position);
        if (relicPosition.HasValue)
            NDisableRelicAbsorbVfx.Play(player, relicPosition.Value, target);

        MainFile.Logger.Info($"[DisableRelicManager] Disabled relic '{relic.Id.Entry}' ({relic.Title}) at position {position} for player {player.NetId}");

        return true;
    }

    /// <summary>
    /// 判断一个遗物是否可以禁用
    /// </summary>
    public static bool IsRelicLockable(RelicModel relic)
    {
        if (relic == null) return false;
        // 不能是锁定遗物
        if (relic is KarenLockRelic)
            return false;
        // 不能是先古遗物
        if (relic.Rarity == RelicRarity.Ancient)
            return false;
        // 不能在白名单中
        if (IsInWhiteList(relic))
            return false;
        return true;
    }


    /// <summary>
    /// 恢复所有被禁用的遗物
    /// 从后往前恢复，避免位置偏移问题
    /// </summary>
    /// <param name="player">玩家</param>
    public static void RestoreAllRelics(Player player)
    {
        if (player == null) return;

        var lockRelics = player.Relics.OfType<KarenLockRelic>().ToList();
        if (lockRelics.Count == 0) return;

        MainFile.Logger.Info($"[DisableRelicManager] Restoring {lockRelics.Count} disabled relics for player {player.NetId}");

        // 记录每个锁定遗物及其原始位置
        var lockRelicInfos = lockRelics
            .Select(lr => new { LockRelic = lr, OriginalPosition = player.Relics.IndexOf(lr) })
            .Where(x => x.OriginalPosition >= 0)
            .OrderByDescending(x => x.OriginalPosition)  // 按原始位置从大到小排序（从后往前恢复）
            .ToList();

        foreach (var info in lockRelicInfos)
        {
            var lockRelic = info.LockRelic;
            // 每次重新获取当前位置（因为之前的可能已移除，位置发生了变化）
            int currentPosition = player.Relics.IndexOf(lockRelic);
            if (currentPosition < 0)
            {
                MainFile.Logger.Warn($"[DisableRelicManager] Lock relic '{lockRelic.Id.Entry}' not found in player relics");
                continue;
            }

            // 获取原始遗物
            var originalRelic = lockRelic.LockedRelic;
            if (originalRelic == null)
            {
                MainFile.Logger.Warn($"[DisableRelicManager] Original relic is null");
                continue;
            }

            ReplaceRelic(player, lockRelic, originalRelic, currentPosition);
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
            if (!IsRelicLockable(relic)) continue;
            return i;
        }

        return -1;
    }

    public static IReadOnlyList<RelicModel> GetRelicsToDisable(Player player, int count)
    {
        var relics = new List<RelicModel>();
        if (player == null || count <= 0) return relics;

        int searchEndPosition = player.Relics.Count;
        while (relics.Count < count)
        {
            int position = GetDisableableRelicPosition(player, searchEndPosition);
            if (position < 0) break;

            relics.Add(player.Relics[position]);
            searchEndPosition = position;
        }

        return relics;
    }

    /// <summary>
    /// 获取可以禁用的遗物数量
    /// </summary>
    /// <param name="player">玩家</param>
    /// <returns>可禁用的遗物数量</returns>
    public static int GetDisableableRelicCount(Player player)
    {
        if (player == null) return 0;

        return player.Relics.Count(IsRelicLockable);
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
