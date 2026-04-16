using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Relics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem;

/// <summary>
/// 禁用遗物系统的节点管理器
/// 复用游戏本体的 NRelicInventory.Add/Remove 方法管理UI节点
/// </summary>
public static class DisableRelicNodeManager
{
    private static readonly BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>
    /// 使用游戏本体的 Add 方法创建遗物节点
    /// </summary>
    public static void AddRelicNode(RelicModel relic, Player player)
    {
        if (!LocalContext.IsMe(player)) return;

        var inventory = GetRelicInventory();
        if (inventory == null) return;

        var addMethod = typeof(NRelicInventory).GetMethod("Add", PrivateInstance);
        addMethod?.Invoke(inventory, new object[] { relic, true, -1 });
    }

    /// <summary>
    /// 使用游戏本体的 Remove 方法移除遗物节点
    /// </summary>
    public static void RemoveRelicNode(RelicModel relic, Player player)
    {
        if (!LocalContext.IsMe(player)) return;

        var inventory = GetRelicInventory();
        if (inventory == null) return;

        var removeMethod = typeof(NRelicInventory).GetMethod("Remove", PrivateInstance);
        removeMethod?.Invoke(inventory, new object[] { relic });
    }

    /// <summary>
    /// 保存原节点并替换为新的遗物节点
    /// 返回保存的原节点（从UI移除但未销毁）
    /// </summary>
    public static NRelicInventoryHolder? SaveAndReplaceRelicNode(RelicModel oldRelic, RelicModel newRelic, Player player)
    {
        if (!LocalContext.IsMe(player)) return null;

        try
        {
            var inventory = GetRelicInventory();
            if (inventory == null) return null;

            var holder = FindRelicHolder(oldRelic, player);
            if (holder == null)
            {
                MainFile.Logger.Warn($"[DisableRelicNodeManager] Cannot find node for relic '{oldRelic.Id.Entry}'");
                return null;
            }

            // 从列表移除（但不销毁），这样 Remove 方法找不到它就不会处理
            var relicNodes = GetRelicNodes(inventory);
            relicNodes?.Remove(holder);

            // 从UI移除但保留引用
            inventory.RemoveChild(holder);

            // 获取新遗物在 player.Relics 中的位置
            var playerRelicsField = typeof(Player).GetField("_relics", BindingFlags.NonPublic | BindingFlags.Instance);
            var playerRelics = playerRelicsField?.GetValue(player) as List<RelicModel>;
            int index = playerRelics?.IndexOf(newRelic) ?? -1;

            // 使用游戏本体方法添加新遗物到正确位置
            var addMethod = typeof(NRelicInventory).GetMethod("Add", PrivateInstance);
            addMethod?.Invoke(inventory, new object[] { newRelic, true, index });

            MainFile.Logger.Info($"[DisableRelicNodeManager] Saved node for '{oldRelic.Id.Entry}', replaced with '{newRelic.Id.Entry}' at position {index}");
            return holder;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[DisableRelicNodeManager] Failed to replace relic node: {ex}");
            return null;
        }
    }

    /// <summary>
    /// 恢复遗物显示——使用游戏本体Add创建新节点，销毁保存的旧节点
    /// </summary>
    public static void RestoreRelicNode(RelicModel lockRelic, RelicModel originalRelic, NRelicInventoryHolder savedNode, Player player)
    {
        if (!LocalContext.IsMe(player)) return;

        try
        {
            var inventory = GetRelicInventory();
            if (inventory == null) return;

            var relicNodes = GetRelicNodes(inventory);
            if (relicNodes == null) return;

            // 找到锁定遗物的节点
            var lockHolder = FindRelicHolder(lockRelic, player);
            if (lockHolder == null)
            {
                MainFile.Logger.Warn($"[DisableRelicNodeManager] Cannot find lock relic node for '{lockRelic.Id.Entry}'");
                return;
            }

            // 获取原始遗物在 player.Relics 中的位置（更准确，因为 player.Relics 已更新）
            var playerRelicsField = typeof(Player).GetField("_relics", BindingFlags.NonPublic | BindingFlags.Instance);
            var playerRelics = playerRelicsField?.GetValue(player) as List<RelicModel>;
            int index = playerRelics?.IndexOf(originalRelic) ?? -1;
            if (index < 0) index = relicNodes.IndexOf(lockHolder); // 备用方案

            // 移除锁定遗物节点
            RemoveRelicNode(lockRelic, player);

            // 销毁保存的旧节点（不再复用，避免信号重复连接问题）
            savedNode.QueueFree();

            // 使用游戏本体Add方法创建新节点（正确处理信号连接）
            var addMethod = typeof(NRelicInventory).GetMethod("Add", PrivateInstance);
            addMethod?.Invoke(inventory, new object[] { originalRelic, true, index });

            MainFile.Logger.Info($"[DisableRelicNodeManager] Restored relic '{originalRelic.Id.Entry}' at position {index}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[DisableRelicNodeManager] Failed to restore relic node: {ex}");
        }
    }

    /// <summary>
    /// 查找遗物对应的Holder
    /// </summary>
    public static NRelicInventoryHolder? FindRelicHolder(RelicModel relic, Player player)
    {
        if (!LocalContext.IsMe(player)) return null;

        var inventory = GetRelicInventory();
        if (inventory == null) return null;

        return GetRelicNodes(inventory)?.FirstOrDefault(n => n.Relic.Model == relic);
    }

    /// <summary>
    /// 替换Holder中的遗物模型（备选方案）
    /// </summary>
    public static void ReplaceHolderModel(NRelicInventoryHolder holder, RelicModel newRelic, Player player)
    {
        if (!LocalContext.IsMe(player)) return;

        var inventory = GetRelicInventory();
        if (inventory == null) return;

        var relicNodes = GetRelicNodes(inventory);
        if (relicNodes == null) return;

        int index = relicNodes.IndexOf(holder);

        // 移除旧holder
        relicNodes.Remove(holder);
        inventory.RemoveChild(holder);
        holder.QueueFree();

        // 使用游戏本体方法添加新遗物
        var addMethod = typeof(NRelicInventory).GetMethod("Add", PrivateInstance);
        addMethod?.Invoke(inventory, new object[] { newRelic, true, index });
    }

    // ============ 私有辅助方法 ============

    private static NRelicInventory? GetRelicInventory()
    {
        return NRun.Instance?.GlobalUi?.RelicInventory;
    }

    private static List<NRelicInventoryHolder>? GetRelicNodes(NRelicInventory inventory)
    {
        var field = typeof(NRelicInventory).GetField("_relicNodes", PrivateInstance);
        return field?.GetValue(inventory) as List<NRelicInventoryHolder>;
    }

    private static void EmitRelicsChanged(NRelicInventory inventory)
    {
        // 使用 Godot 的 EmitSignal 方法触发信号
        inventory.EmitSignal(new StringName("RelicsChanged"));
    }

    private static void UpdateNavigation(NRelicInventory inventory)
    {
        var method = typeof(NRelicInventory).GetMethod("UpdateNavigation", PrivateInstance, Type.EmptyTypes);
        method?.Invoke(inventory, null);
    }
}
