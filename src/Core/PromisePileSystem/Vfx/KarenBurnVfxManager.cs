using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public static class KarenBurnVfxManager
{
    private static readonly SpireField<Player, NKarenBurnVfx?> burnNodes = new(() => null);

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

        var cachedNode = burnNodes.Get(player);
        if (GodotObject.IsInstanceValid(cachedNode))
        {
            cachedNode!.Restart();
            return;
        }

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        if (creatureNode == null) return;

        foreach (var child in creatureNode.GetChildren())
        {
            if (child is NKarenBurnVfx existing)
            {
                existing.Restart();
                burnNodes.Set(player, existing);
                return;
            }
        }

        var newNode = new NKarenBurnVfx();
        creatureNode.AddChild(newNode);
        newNode.Init(creatureNode);
        burnNodes.Set(player, newNode);
    }

    private static void StopInternal(Player player)
    {
        var node = burnNodes.Get(player);
        if (GodotObject.IsInstanceValid(node))
            node!.Stop();

        burnNodes.Set(player, null);
    }
}
