using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public static class KarenPromiseVfxStarManager
{
    private static readonly SpireField<Player, NKarenPromiseStarNode?> Nodes = new(() => null);
    private static NKarenPromiseStarNode? GetOrCreateNode(Player player)
    {
        var creature = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        if (creature == null) return null;
        var node = Nodes.Get(player);
        if (GodotObject.IsInstanceValid(node) && !node!.IsQueuedForDeletion() && node.GetParent() == creature) return node;
        node = new NKarenPromiseStarNode();
        creature.AddChild(node);
        node.Init(creature);
        // Keep the tower above the room background but before all creature visuals.
        creature.MoveChild(node, 0);
        Nodes.Set(player, node);
        return node;
    }
    public static Vector2? GetTransferPosition(NCard card)
        => card.Model.Owner is { } player ? GetOrCreateNode(player)?.TransferPosition(card) : null;

    public static Vector2? GetTowerPosition(Player player)
        => GetOrCreateNode(player)?.TransferGlobalPosition;

    public static bool IsTowerPile(Player player, PileType pileType)
        => pileType == KarenCustomEnum.PromisePile ||
           (pileType == PileType.Draw && PromisePileManager.IsVoidMode(player));

    public static void UpdatePromisePileStarCount(Player player)
    {
        var combat = player.PlayerCombatState;
        var room = NCombatRoom.Instance;
        // Read authoritative state when executed, never a stale captured count.
        Callable.From(() =>
        {
            if (combat == null || room == null || player.PlayerCombatState != combat || NCombatRoom.Instance != room) return;
            var power = player.Creature.GetPower<KarenPromisePilePower>();
            PromisePileMode mode = PromisePileMode.None;
            foreach (var flag in new[] { PromisePileMode.Void, PromisePileMode.InfiniteReinforcement, PromisePileMode.Burn, PromisePileMode.PastAndFuture })
                if (power?.IsInMode(flag) == true) mode |= flag;
            var cards = (power?.IsVoidMode == true ? PileType.Draw.GetPile(player) : PromisePileManager.GetPromisePile(player)).Cards.ToArray();
            GetOrCreateNode(player)?.Sync(cards, mode);
        }).CallDeferred();
    }
    public static void ClearAll(Player player)
    {
        // Old deferred cleanup must not erase the next combat's constellation.
        var node = Nodes.Get(player);
        if (GodotObject.IsInstanceValid(node)) { node!.ClearAll(); node.QueueFree(); }
        Nodes.Set(player, null);
    }
}
