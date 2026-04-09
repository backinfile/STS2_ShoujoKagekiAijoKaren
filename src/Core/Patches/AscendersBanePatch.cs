using System.Linq;
using GodotPlugins.Game;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Models.Cards;
using ShoujoKagekiAijoKaren.src.Models.Characters;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// 将 Karen 进阶5获得的进阶之灾替换为困意
/// </summary>
[HarmonyPatch(typeof(AscensionManager), nameof(AscensionManager.ApplyEffectsTo))]
public class AscendersBanePatch
{
    private static void Postfix(Player player)
    {
        if (player.Character.Id.Entry != Karen.CHAR_ID)
            return;

        var curse = player.Deck.Cards.FirstOrDefault(c => c is AscendersBane);

        if (curse == null)
        {
            return;
        }

        MainFile.Logger.Info("Removing Ascender's Bane and adding Karen Sleepy for Karen's Ascension 5.");

        // 移除进阶之灾
        player.Deck.RemoveInternal(curse, silent: true);
        curse.RemoveFromState();

        // 添加困意
        var sleepy = player.RunState.CreateCard<KarenSleepy>(player);
        sleepy.FloorAddedToDeck = 1;
        player.Deck.AddInternal(sleepy, -1, silent: true);
    }
}
