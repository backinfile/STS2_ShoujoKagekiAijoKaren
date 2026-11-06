using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>Visual-only ownership for a Fuel card that has already burst. Never changes its destination.</summary>
public static class KarenFuelCardVisuals
{
    private sealed record State(CardModel Model, bool Visible);
    private static readonly SpireField<NCard, State?> States = new(() => null);
    private sealed record RevealState(Tween Tween, Color Color);
    private static readonly SpireField<NCard, RevealState?> Reveals = new(() => null);

    public static bool IsBurst(NCard card) => States.Get(card) is { } state && state.Model == card.Model;

    public static void Burst(NCard card)
    {
        if (IsBurst(card)) return;
        States.Set(card, new State(card.Model, card.Visible));
        card.Visible = false;
    }

    public static void Restore(NCard card)
    {
        var reveal = Reveals.Get(card);
        Reveals.Set(card, null);
        if (reveal != null)
        {
            reveal.Tween.Kill();
            card.Body.SelfModulate = reveal.Color;
        }
        var state = States.Get(card);
        States.Set(card, null);
        if (state != null && state.Model == card.Model) card.Visible = state.Visible;
    }

    [HarmonyPatch(typeof(NCard), nameof(NCard.OnFreedToPool))]
    private static class PoolPatch { private static void Prefix(NCard __instance) => Restore(__instance); }

    // A returning hand card and each replay need their face back before the next burst.
    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
    private static class RefreshPatch
    {
        private static void Prefix(NCard __instance, PileType pileType)
        {
            if (pileType is PileType.Hand or PileType.Play) Restore(__instance);
        }
    }

    // Non-exhausted Fuel keeps its card identity: reveal the surviving card for its native return flight.
    [HarmonyPatch(typeof(NCardFlyVfx), nameof(NCardFlyVfx._Ready))]
    private static class FlyPatch
    {
        private static readonly AccessTools.FieldRef<NCardFlyVfx, NCard> Card =
            AccessTools.FieldRefAccess<NCardFlyVfx, NCard>("_card");
        [HarmonyPriority(Priority.First)]
        private static void Prefix(NCardFlyVfx __instance)
        {
            var card = Card(__instance);
            if (!IsBurst(card)) return;
            Restore(card);
            var color = card.Body.SelfModulate;
            card.Body.SelfModulate = new Color(color, 0);
            var tween = card.CreateTween();
            tween.TweenProperty(card.Body, "self_modulate", color, .18f);
            Reveals.Set(card, new RevealState(tween, color));
        }
    }

#if STS2_BETA
    [HarmonyPatch(typeof(NCardExhaustVfx), nameof(NCardExhaustVfx.Create))]
    private static class ExhaustPatch
    {
        private static bool Prefix([HarmonyArgument(0)] NCard card, ref NCardExhaustVfx? __result)
#else
    [HarmonyPatch(typeof(NExhaustVfx), nameof(NExhaustVfx.Create))]
    private static class ExhaustPatch
    {
        private static bool Prefix([HarmonyArgument(0)] NCard card, ref NExhaustVfx? __result)
#endif
        {
            if (!IsBurst(card)) return true;
            // Both native versions still complete the model move and return the card
            // to the pool when no exhaust effect is created.
            __result = null;
            return false;
        }
    }
}
