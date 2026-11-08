using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Patches;

/// <summary>Native callers targeting the logical promise pile use the same mode routing as character commands.</summary>
public static class PromisePileContainerPatch
{
    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add))]
#if STS2_BETA
    [HarmonyPatch([typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool), typeof(bool)])]
#else
    [HarmonyPatch([typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool)])]
#endif
    private static class RouteDestination
    {
        [HarmonyPrefix, HarmonyPriority(Priority.First * 10)]
        private static void Prefix(ref IEnumerable<CardModel> cards, ref CardPile newPile)
        {
            if (newPile.Type != KarenCustomEnum.PromisePile) return;
            var snapshot = cards.ToArray();
            cards = snapshot;
            if (snapshot.FirstOrDefault()?.Owner is { PlayerCombatState: not null } player)
                newPile = PromisePileManager.EffectivePile(player);
        }
    }
}
