using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public static class KarenGeerVfxManager
{
    public static void PlayCast(Player player) => Play(player, true);
    public static void PlayTrigger(Player player) => Play(player, false);

    private static void Play(Player player, bool fullScreen)
    {
        if (NonInteractiveMode.IsActive) return;
        var room = NCombatRoom.Instance;
        if (room == null) return;
        Callable.From(() =>
        {
            // Never attach a deferred effect to a different combat.
            if (!GodotObject.IsInstanceValid(room) || NCombatRoom.Instance != room
                || !CombatManager.Instance.IsInProgress) return;
            var creature = room.GetCreatureNode(player.Creature);
            if (creature == null) return;
            var parent = fullScreen ? (Node?)NRun.Instance?.GlobalUi : creature;
            if (parent == null) return;
            var node = new NKarenGeerVfx();
            node.Init(creature, fullScreen);
            parent.AddChild(node);
        }).CallDeferred();
    }
}
