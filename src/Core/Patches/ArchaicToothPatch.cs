using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.ancient;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.basic;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// 让 ArchaicTooth（古老牙齿）能转换 Karen 的初始打击为 Ancient 版本。
/// </summary>
public static class ArchaicToothPatch
{
    /// <summary>
    /// 如果原版没找到可转换的起始卡，且玩家是 Karen，则返回 KarenStrike。
    /// </summary>
    [HarmonyPatch(typeof(ArchaicTooth), "GetTranscendenceStarterCard")]
    public static class StarterPatch
    {
        private static void Postfix(ArchaicTooth __instance, Player player, ref CardModel __result)
        {
            if (__result != null) return;
            if (player?.Character?.Id?.Entry != "KAREN") return;

            var starter = player.Deck.Cards.FirstOrDefault(c => c.Id == ModelDb.Card<KarenStrike>().Id);
            if (starter != null)
                __result = starter;
        }
    }

    /// <summary>
    /// 如果起始卡是 KarenStrike，则转换为 KarenAncientStrike，并继承升级/附魔。
    /// </summary>
    [HarmonyPatch(typeof(ArchaicTooth), "GetTranscendenceTransformedCard")]
    public static class TransformedPatch
    {
        private static void Postfix(ArchaicTooth __instance, CardModel starterCard, ref CardModel __result)
        {
            if (starterCard?.Id != ModelDb.Card<KarenStrike>().Id) return;

            var ancient = __instance.Owner.RunState.CreateCard(ModelDb.Card<KarenAncientStrike>(), starterCard.Owner);

            if (starterCard.IsUpgraded)
                CardCmd.Upgrade(ancient);

            if (starterCard.Enchantment != null)
            {
                var enchant = (EnchantmentModel)starterCard.Enchantment.MutableClone();
                CardCmd.Enchant(enchant, ancient, enchant.Amount);
            }

            __result = ancient;
        }
    }


    /// <summary>
    /// 将 KarenAncientStrike 加入 ArchaicTooth 的 TranscendenceCards，
    /// 使沉封魔典（DustyTome）等系统不会选中它。
    /// </summary>
    [HarmonyPatch(typeof(ArchaicTooth), "TranscendenceCards", MethodType.Getter)]
    public class ArchaicToothTranscendenceCardsPatch
    {
        private static void Postfix(ref List<CardModel> __result)
        {
            __result.Add(ModelDb.Card<KarenAncientStrike>());
        }
    }

}
