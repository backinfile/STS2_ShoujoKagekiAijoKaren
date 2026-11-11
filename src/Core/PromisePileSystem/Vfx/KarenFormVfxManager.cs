using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public static class KarenFormVfxManager
{
    private static readonly CombatVfxNodes<NKarenFormVfx> Nodes = new(creature => creature);

    public static void Start(Player player)
    {
        if (LocalContext.IsMe(player))
            Nodes.Start(player, () => player.Creature.HasPower<KarenFormPower>());
    }

    public static void PulseDraw(Player player)
    {
        if (LocalContext.IsMe(player))
            Nodes.Update(player, node => { if (player.Creature.HasPower<KarenFormPower>()) node.PulseDraw(); });
    }

    public static void Stop(Player player)
    {
        if (LocalContext.IsMe(player)) Nodes.Stop(player);
    }
}
