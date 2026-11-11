using System;
using System.Linq;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

internal interface ICombatVfx
{
    bool BelongsTo(NCreature creature);
    void Init(NCreature creature);
    void Restart();
    void Stop();
}

internal sealed class CombatVfxNodes<T>(Func<NCreature, Node?> parent,
    Action<Node, T, NCreature>? place = null) where T : Node, ICombatVfx, new()
{
    private readonly SpireField<Player, T?> _nodes = new(() => null);

    private static bool Live(T? node) => GodotObject.IsInstanceValid(node) && !node!.IsQueuedForDeletion();

    private static void Defer(Player player, Action<NCreature> action)
    {
        var room = NCombatRoom.Instance;
        if (room == null) return;
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(room) || room != NCombatRoom.Instance) return;
            if (room.GetCreatureNode(player.Creature) is { } creature) action(creature);
        }).CallDeferred();
    }

    internal void Start(Player player, Func<bool>? enabled = null) => Defer(player, creature =>
    {
        if (enabled?.Invoke() == false || parent(creature) is not { } host) return;
        var node = _nodes.Get(player);
        if (!Live(node) || !node!.BelongsTo(creature))
        {
            if (Live(node)) node!.Stop();
            node = host.GetChildren().OfType<T>().FirstOrDefault(child => Live(child) && child.BelongsTo(creature));
            if (node == null)
            {
                node = new T();
                host.AddChild(node);
            }
            node.Init(creature);
            place?.Invoke(host, node, creature);
            _nodes.Set(player, node);
        }
        node!.Restart();
    });

    internal void Stop(Player player) => Defer(player, _ =>
    {
        var node = _nodes.Get(player);
        if (Live(node)) node!.Stop();
        _nodes.Set(player, null);
    });

    internal void Update(Player player, Action<T> update) => Defer(player, creature =>
    {
        var node = _nodes.Get(player);
        if (Live(node) && node!.BelongsTo(creature)) update(node);
    });
}
