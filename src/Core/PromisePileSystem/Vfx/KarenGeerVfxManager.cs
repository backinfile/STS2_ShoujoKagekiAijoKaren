using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public static class KarenGeerVfxManager
{
    private static readonly SpireField<Player, NKarenGeerVfx?> Nodes = new(() => null);

    public static void Start(Player player) => Run(() => Ensure(player));
    public static void Stop(Player player) => Run(() =>
    {
        // Removal hooks can finish after a replacement power has already been applied.
        if (player.Creature.Powers.Any(p => p is KarenGeerPower)) { Ensure(player); return; }
        if (GodotObject.IsInstanceValid(Nodes.Get(player))) Nodes.Get(player)!.Stop();
        Nodes.Set(player, null);
    });
    public static void BeginSelection(Player player) => Run(() =>
    {
        var pile = PromisePileManager.IsVoidMode(player)
            ? PileType.Draw.GetPile(player) : PromisePileManager.GetPromisePile(player);
        Ensure(player)?.BeginSelection(pile.Cards.Take(3).ToArray());
    });
    public static void CommitSelection(Player player, CardModel selected) => Run(() =>
        Ensure(player)?.CommitSelection(selected));
    public static void EndSelection(Player player) => Run(() =>
    {
        if (GodotObject.IsInstanceValid(Nodes.Get(player))) Nodes.Get(player)!.EndSelection();
    });

    private static void Run(System.Action action)
    {
        if (!NonInteractiveMode.IsActive) Callable.From(action).CallDeferred();
    }

    private static NKarenGeerVfx? Ensure(Player player)
    {
        if (player.PlayerCombatState == null || !player.Creature.Powers.Any(p => p is KarenGeerPower)) return null;
        var creature = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        if (creature == null) return null;
        var current = Nodes.Get(player);
        if (GodotObject.IsInstanceValid(current) && !current!.IsQueuedForDeletion() && current.BelongsTo(creature))
        {
            current.Restart();
            return current;
        }
        if (GodotObject.IsInstanceValid(current)) current!.Stop();
        var existing = creature.GetChildren().OfType<NKarenGeerVfx>()
            .FirstOrDefault(n => !n.IsQueuedForDeletion() && n.BelongsTo(creature));
        if (existing != null)
        {
            existing.Restart();
            Nodes.Set(player, existing);
            return existing;
        }
        var node = new NKarenGeerVfx();
        node.Init(creature);
        creature.AddChild(node);
        Nodes.Set(player, node);
        return node;
    }
}
