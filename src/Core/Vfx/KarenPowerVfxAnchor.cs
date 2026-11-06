using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Resolve only bound, live native power icons; scene placeholders have no Model.</summary>
public static class KarenPowerVfxAnchor
{
    public static bool IsFor(NPower? node, PowerModel? power)
    {
        if (power == null || !GodotObject.IsInstanceValid(node) || !node!.IsInsideTree()
            || !node.IsNodeReady() || node.IsQueuedForDeletion() || !node.IsVisibleInTree()
            // Native NPower.Create binds Model before NPowerContainer assigns Container.
            // Unbound scene placeholders can already be ready and visible in the room.
            || node.Container == null) return false;
        return node.Model == power;
    }

    public static NPower? Find(Node root, PowerModel? power)
        => power == null ? null : root.FindChildren("*", "", true, false).OfType<NPower>()
            .FirstOrDefault(node => IsFor(node, power));
}
