using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Potions;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>Only the exact potion created by a Banana cake gets a formation-to-slot origin.</summary>
[HarmonyPatch(typeof(NPotionContainer), nameof(NPotionContainer.AnimatePotion))]
internal static class KarenBananaCakePotionVfxPatch
{
    private static void Prefix(NPotionContainer __instance, PotionModel potion, ref Vector2? startPosition)
    {
        if (startPosition != null || !NKarenBananaVfx.TryGetPotionOrigin(potion, out var viewportPosition)) return;
        // Native animation moves the potion's Container origin, rather than the image centre.
        var hudPosition = __instance.GetCanvasTransform().AffineInverse() * viewportPosition;
        foreach (var nativePotion in __instance.FindChildren("*", "", true, false))
        {
            if (nativePotion is not NPotion node || node.Model != potion) continue;
            var container = node.GetNode<Control>("Container");
            hudPosition -= node.Image.GetGlobalTransform() * (node.Image.Size / 2) - container.GlobalPosition;
            break;
        }
        startPosition = hudPosition;
    }
}
