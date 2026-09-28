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

    public static void Pulse(Player player)
    {
        Callable.From(() => PulseInternal(player)).CallDeferred();
    }

    private static void StartInternal(Player player)
    {
        if (player?.Creature == null) return;

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        if (creatureNode == null) return;

        var cachedNode = RingNodes.Get(player);
        if (GodotObject.IsInstanceValid(cachedNode) && !cachedNode!.IsQueuedForDeletion() && cachedNode.BelongsTo(creatureNode))
        {
            cachedNode!.Restart();
            return;
        }

        var parent = creatureNode.GetParent();
        if (parent == null) return;

        foreach (var child in parent.GetChildren())
        {
            if (child is NKarenPastAndFutureRingVfx existing && !existing.IsQueuedForDeletion() && existing.BelongsTo(creatureNode))
            {
                existing.Init(creatureNode);
                MoveBehindCreature(parent, existing, creatureNode);
                existing.Restart();
                RingNodes.Set(player, existing);
                return;
            }
        }

        var newNode = new NKarenPastAndFutureRingVfx();
        parent.AddChildSafely(newNode);
        MoveBehindCreature(parent, newNode, creatureNode);
        newNode.Init(creatureNode);
        RingNodes.Set(player, newNode);
    }

    private static void MoveBehindCreature(Node parent, Node ringNode, Node creatureNode)
    {
        var targetIndex = creatureNode.GetIndex() - (ringNode.GetIndex() < creatureNode.GetIndex() ? 1 : 0);
        parent.MoveChild(ringNode, targetIndex);
    }

    private static void StopInternal(Player player)
    {
        var node = RingNodes.Get(player);
        if (GodotObject.IsInstanceValid(node))
            node!.Stop();

        RingNodes.Set(player, null);
    }

    private static void PulseInternal(Player player)
    {
        var node = RingNodes.Get(player);
        if (GodotObject.IsInstanceValid(node))
            node!.Pulse();
    }
}
