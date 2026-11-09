using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Relics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem;

/// <summary>Projects the player's current relic models into the native inventory.</summary>
public static class DisableRelicNodeManager
{
    private static readonly System.Reflection.MethodInfo Add = AccessTools.Method(typeof(NRelicInventory), "Add");
    private static readonly System.Reflection.MethodInfo Remove = AccessTools.Method(typeof(NRelicInventory), "Remove");
    private static readonly System.Reflection.FieldInfo RelicNodes = AccessTools.Field(typeof(NRelicInventory), "_relicNodes");

    private static NRelicInventory? Inventory(Player player)
        => LocalContext.IsMe(player) ? NRun.Instance?.GlobalUi?.RelicInventory : null;
    private static NRelicInventoryHolder[] Holders(NRelicInventory inventory)
        => ((IEnumerable<NRelicInventoryHolder>)RelicNodes.GetValue(inventory)!).ToArray();
    public static NRelicInventoryHolder? FindRelicHolder(RelicModel relic, Player player)
        => Inventory(player) is { } inventory ? Holders(inventory).FirstOrDefault(holder => holder.Relic.Model == relic) : null;

    public static void Replace(RelicModel previous, RelicModel current, Player player)
    {
        if (Inventory(player) is not { } inventory) return;
        try
        {
            if (FindRelicHolder(previous, player) != null) Remove.Invoke(inventory, [previous]);
            if (FindRelicHolder(current, player) == null)
                Add.Invoke(inventory, [current, true, player.Relics.ToList().IndexOf(current)]);
        }
        catch (Exception error)
        {
            MainFile.Logger.Error($"Relic display replacement failed, rebuilding inventory: {error}");
            Rebuild(player);
        }
    }
    public static void Rebuild(Player player)
    {
        if (Inventory(player) is not { } inventory) return;
        foreach (var holder in Holders(inventory)) Remove.Invoke(inventory, [holder.Relic.Model]);
        for (int index = 0; index < player.Relics.Count; index++)
            Add.Invoke(inventory, [player.Relics[index], true, index]);
    }
}
