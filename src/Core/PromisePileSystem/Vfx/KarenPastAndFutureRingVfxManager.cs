using MegaCrit.Sts2.Core.Entities.Players;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public static class KarenPastAndFutureRingVfxManager
{
    private static readonly CombatVfxNodes<NKarenPastAndFutureRingVfx> Nodes = new(
        creature => creature.GetParent(),
        (parent, node, creature) => parent.MoveChild(node,
            creature.GetIndex() - (node.GetIndex() < creature.GetIndex() ? 1 : 0)));

    public static void Start(Player player) => Nodes.Start(player);
    public static void Stop(Player player) => Nodes.Stop(player);
    public static void Pulse(Player player) => Nodes.Update(player, node => node.Pulse());
}
