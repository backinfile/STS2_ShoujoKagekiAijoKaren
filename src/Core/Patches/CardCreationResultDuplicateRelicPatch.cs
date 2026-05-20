using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

[HarmonyPatch(typeof(CardCreationResult), nameof(CardCreationResult.ModifyCard), typeof(CardModel), typeof(RelicModel))]
internal static class CardCreationResultDuplicateRelicPatch
{
    private static bool Prefix(CardCreationResult __instance, RelicModel modifyingRelic)
    {
        return !__instance.ModifyingRelics.Any(relic => ReferenceEquals(relic, modifyingRelic));
    }
}
