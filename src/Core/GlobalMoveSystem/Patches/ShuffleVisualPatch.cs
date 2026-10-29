#if STS2_BETA
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using System.Collections.Generic;

namespace ShoujoKagekiAijoKaren.src.Core.GlobalMoveSystem.Patches;

// v0.111 moves the entire shuffle batch before running its pile-change hooks,
// then builds the animations. Next Stage+ can draw those cards during the hooks.
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.GetTweenForCardsChangingPiles))]
[HarmonyPatch([typeof(IEnumerable<CardPileAddResult>), typeof(bool)])]
internal static class ShuffleVisualPatch
{
    [HarmonyPrefix]
    private static void Prefix(ref IEnumerable<CardPileAddResult> results, bool fromSilentAdd)
    {
        if (!fromSilentAdd) return;

        var pending = new List<CardPileAddResult>();
        foreach (var result in results)
        {
            if (result.success && result.targetPile == PileType.Draw &&
                result.oldPile is { Type: PileType.Discard } oldPile &&
                result.cardAdded.Pile?.Type != PileType.Draw)
            {
                // The nested move already owns the card's current visual. Complete
                // the silent removal notifications without replaying the stale move.
                oldPile.InvokeCardRemoved(result.cardAdded);
                oldPile.InvokeCardRemoveFinished();
                oldPile.InvokeContentsChanged();
            }
            else
            {
                pending.Add(result);
            }
        }

        results = pending;
    }
}
#endif
