using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem.Nodes;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.ShineSystem;

public static class ShineVfx
{
    public static bool WillDeplete(CardModel card) => card.IsShineCard() && !card.IsDupe
        && !card.Keywords.Contains(CardKeyword.Eternal)
        && (card.GetShineValue() <= 1 || card.ShouldEnterShinePileAfterPlay()
            || card.Owner?.Creature.Powers.Any(p => p is KarenStarlight02Power) == true);

    private static readonly SpireField<NCard, NKarenShineWarningVfx?> Warnings = new(() => null);

    private static void Refresh(NCard card)
    {
        if (!card.IsNodeReady()) return;
        var old = Warnings.Get(card);
        if (GodotObject.IsInstanceValid(old) && old!.BelongsTo(card.Model)) return;
        Clear(card);
        if (card.Model?.IsShineCard() != true) return;
        var warning = new NKarenShineWarningVfx(card, card.Model);
        card.OverlayContainer.AddChild(warning);
        Warnings.Set(card, warning);
    }

    private static void Clear(NCard card)
    {
        var old = Warnings.Get(card);
        Warnings.Set(card, null);
        if (!GodotObject.IsInstanceValid(old)) return;
        old!.GetParent()?.RemoveChild(old);
        old.QueueFree();
    }

    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
    private static class VisualPatch { private static void Postfix(NCard __instance) => Refresh(__instance); }

    [HarmonyPatch(typeof(NCard), "ReloadOverlay")]
    private static class OverlayPatch { private static void Postfix(NCard __instance) => Refresh(__instance); }

    [HarmonyPatch(typeof(NCard), nameof(NCard.OnFreedToPool))]
    private static class PoolPatch { private static void Prefix(NCard __instance) => Clear(__instance); }
}
