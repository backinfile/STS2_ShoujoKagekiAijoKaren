using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public static class KarenPastAndFutureRingVfxManager
{
    private static readonly SpireField<Player, NKarenPastAndFutureRingVfx?> RingNodes = new(() => null);

    public static void Start(Player player)
    {
        Callable.From(() => StartInternal(player)).CallDeferred();
    }

    public static void Stop(Player player)
    {
        Callable.From(() => StopInternal(player)).CallDeferred();
    }

    private static void StartInternal(Player player)
    {
        if (player?.Creature == null) return;

        var cachedNode = RingNodes.Get(player);
        if (GodotObject.IsInstanceValid(cachedNode))
        {
            cachedNode!.Restart();
            return;
        }

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        if (creatureNode == null) return;

        var parent = creatureNode.GetParent();
        if (parent == null) return;

        foreach (var child in parent.GetChildren())
        {
            if (child is NKarenPastAndFutureRingVfx existing)
            {
                existing.Init(creatureNode);
                existing.Restart();
                RingNodes.Set(player, existing);
                return;
            }
        }

        var newNode = new NKarenPastAndFutureRingVfx();
        parent.AddChildSafely(newNode);
        parent.MoveChild(newNode, creatureNode.GetIndex());
        newNode.Init(creatureNode);
        RingNodes.Set(player, newNode);
    }

    private static void StopInternal(Player player)
    {
        var node = RingNodes.Get(player);
        if (GodotObject.IsInstanceValid(node))
            node!.Stop();

        RingNodes.Set(player, null);
    }
}
