using BaseLib.Utils;
using Godot;
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

    public static void Stop(Player player)
    {
        if (!LocalContext.IsMe(player))
            return;

        Callable.From(() => StopInternal(player)).CallDeferred();
    }

    private static void StartInternal(Player player)
    {
        if (player?.Creature == null) return;

        var cachedNode = formNodes.Get(player);
        if (GodotObject.IsInstanceValid(cachedNode))
        {
            cachedNode!.Restart();
            return;
        }

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        if (creatureNode == null) return;

        foreach (var child in creatureNode.GetChildren())
        {
            if (child is NKarenFormVfx existing)
            {
                existing.Restart();
                formNodes.Set(player, existing);
                return;
            }
        }

        var newNode = new NKarenFormVfx();
        creatureNode.AddChild(newNode);
        newNode.Init(creatureNode);
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
