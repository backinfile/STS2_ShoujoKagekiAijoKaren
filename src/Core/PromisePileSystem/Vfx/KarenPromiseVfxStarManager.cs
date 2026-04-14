using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>
/// 约定牌堆星星管理器：负责获取/创建 NKarenPromiseStarManager 实例，并同步约定牌堆数量
/// </summary>
public static class KarenPromiseVfxStarManager
{
    private static readonly SpireField<Player, NKarenPromiseStarNode?> starNodes = new(() => null);


    private static NKarenPromiseStarNode? GetOrCreateNode(Player player)
    {
        var node = starNodes.Get(player);
        if (node != null) return node;

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        if (creatureNode == null) return null;

        foreach (var child in creatureNode.GetChildren())
            if (child is NKarenPromiseStarNode mgr)
            {
                starNodes.Set(player, mgr);
                return mgr;
            }

        var newNode = new NKarenPromiseStarNode();
        creatureNode.CallDeferred("add_child", newNode);
        newNode.Init(creatureNode);
        starNodes.Set(player, newNode);
        return newNode;
    }

    public static void UpdatePromisePileStarCount(Player player)
    {
        var count = PromisePileManager.GetCount(player);
        Callable.From(() => UpdateCountInternal(player, count)).CallDeferred();
    }

    private static void UpdateCountInternal(Player player, int count)
    {
        GetOrCreateNode(player)?.UpdateCount(count);
    }

    public static void ClearAll(Player player)
    {
        MainFile.Logger.Info($"[PromisePileStar] ClearAll called for player {player?.Character?.Id?.Entry}, scheduling deferred");
        Callable.From(() => ClearAllInternal(player)).CallDeferred();
    }

    private static void ClearAllInternal(Player player)
    {
        var node = starNodes.Get(player);
        MainFile.Logger.Info($"[PromisePileStar] ClearAllInternal executing, cached node is null? {node == null}, valid? {GodotObject.IsInstanceValid(node)}");
        if (GodotObject.IsInstanceValid(node))
        {
            node.ClearAll();
            MainFile.Logger.Info("[PromisePileStar] node.ClearAll() called");
        }
        else if (node != null)
        {
            MainFile.Logger.Warn("[PromisePileStar] Cached node is invalid (freed), skipping ClearAll");
        }
        starNodes.Set(player, null);
    }
}
