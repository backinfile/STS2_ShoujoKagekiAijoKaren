using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Models.Cards;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// 将 Karen 进阶5获得的进阶之灾替换为困意
/// </summary>
[HarmonyPatch(typeof(AscensionManager), nameof(AscensionManager.ApplyEffectsTo))]
public class AscendersBanePatch
{
    private static void Postfix(Player player)
    {
        if (player.Character.Id.Entry != "KAREN")
            return;

        var ascendersBane = player.Deck.Cards.FirstOrDefault(c => c is AscendersBane);
        if (ascendersBane != null)
        {
            player.Deck.RemoveInternal(ascendersBane, silent: true);
            ascendersBane.RemoveFromState();
        }

        var sleepy = player.RunState.CreateCard<KarenSleepy>(player);
        sleepy.FloorAddedToDeck = 1;
        player.Deck.AddInternal(sleepy, -1, silent: true);
    }
}
