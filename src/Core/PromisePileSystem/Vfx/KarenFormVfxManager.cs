using BaseLib.Utils;
using Godot;
using System.Linq;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public static class KarenFormVfxManager
{
    private static readonly SpireField<Player, NKarenFormVfx?> formNodes = new(() => null);

    public static void Start(Player player)
    {
        if (!LocalContext.IsMe(player))
            return;

        Callable.From(() => StartInternal(player)).CallDeferred();
    }

    public static void PulseDraw(Player player)
    {
        if (!LocalContext.IsMe(player)) return;
        Callable.From(() =>
        {
            if (!player.Creature.Powers.Any(p => p is KarenFormPower)) return;
            var creature = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
            var node = formNodes.Get(player);
            if (creature != null && GodotObject.IsInstanceValid(node)
                && !node!.IsQueuedForDeletion() && node.BelongsTo(creature))
                node.PulseDraw();
        }).CallDeferred();
    }

    public static void Stop(Player player)
    {
        if (!LocalContext.IsMe(player))
            return;

        Callable.From(() => StopInternal(player)).CallDeferred();
    }

    private static void StartInternal(Player player)
    {
        if (player?.Creature == null) return;

        if (!player.Creature.Powers.Any(p => p is KarenFormPower)) return;
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        if (creatureNode == null) return;

        var cachedNode = formNodes.Get(player);
        if (GodotObject.IsInstanceValid(cachedNode) && !cachedNode!.IsQueuedForDeletion()
            && cachedNode.BelongsTo(creatureNode))
        {
            cachedNode.Restart();
            return;
        }
        if (GodotObject.IsInstanceValid(cachedNode) && !cachedNode!.IsQueuedForDeletion())
            cachedNode.Stop();

        foreach (var child in creatureNode.GetChildren())
        {
            if (child is NKarenFormVfx existing && !existing.IsQueuedForDeletion()
                && existing.BelongsTo(creatureNode))
            {
                existing.Restart();
                formNodes.Set(player, existing);
                return;
            }
        }

        var newNode = new NKarenFormVfx();
        newNode.Init(creatureNode);
        creatureNode.AddChild(newNode);
        formNodes.Set(player, newNode);
    }

    private static void StopInternal(Player player)
    {
        var node = formNodes.Get(player);
        if (GodotObject.IsInstanceValid(node))
            node!.Stop();

        formNodes.Set(player, null);
    }
}
