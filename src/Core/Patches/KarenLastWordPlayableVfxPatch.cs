using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

[HarmonyPatch(typeof(NHandCardHolder), nameof(NHandCardHolder.UpdateCard))]
public static class KarenLastWordPlayableVfxPatch
{
    private static readonly SpireField<NCard, NKarenLastWordPlayableVfx?> Vfx = new(() => null);

    private static void Postfix(NHandCardHolder __instance)
    {
        var cardNode = __instance.CardNode;
        if (cardNode?.Model is not KarenLastWord lastWord)
        {
            Clear(cardNode);
            return;
        }

        if (lastWord.Pile?.Type == PileType.Hand && lastWord.CanPlay())
            Ensure(cardNode);
        else
            Clear(cardNode);
    }

    private static void Ensure(NCard cardNode)
    {
        var existing = Vfx.Get(cardNode);
        if (existing != null && GodotObject.IsInstanceValid(existing))
            return;

        var vfx = new NKarenLastWordPlayableVfx(cardNode);
        var parent = (Node?)NRun.Instance?.GlobalUi ?? NCombatRoom.Instance?.Ui ?? cardNode.GetParent();
        parent?.AddChildSafely(vfx);
        Vfx.Set(cardNode, vfx);
    }

    private static void Clear(NCard? cardNode)
    {
        if (cardNode == null)
            return;

        var existing = Vfx.Get(cardNode);
        if (existing == null)
            return;

        if (GodotObject.IsInstanceValid(existing))
            existing.QueueFree();

        Vfx.Set(cardNode, null);
    }
}
