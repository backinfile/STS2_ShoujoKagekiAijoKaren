using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.ancient;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.basic;
using ShoujoKagekiAijoKaren.src.Models.Cards;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// 让 ArchaicTooth（古老牙齿）能转换 Karen 的初始打击为 Ancient 版本。
/// </summary>
public static class ArchaicToothPatch
{
    private static string DescribeCard(CardModel? card)
    {
        if (card == null)
            return "card=<null>";

        var owner = card.Owner;
        var ownerId = owner?.NetId.ToString() ?? "<null>";
        var runState = owner?.RunState != null ? "set" : "null";
        var enchantment = card.Enchantment?.Id.Entry ?? "<null>";

        return $"card={card.Id.Entry}, upgraded={card.IsUpgraded}, owner={ownerId}, runState={runState}, enchantment={enchantment}";
    }

    /// <summary>
    /// 如果原版没找到可转换的起始卡，且玩家是 Karen，则返回 KarenFall。
    /// </summary>
    [HarmonyPatch(typeof(ArchaicTooth), "GetTranscendenceStarterCard")]
    public static class StarterPatch
    {
        private static void Postfix(ArchaicTooth __instance, Player player, ref CardModel __result)
        {
            MainFile.Logger.Info($"[ArchaicToothPatch.Starter] Entered. player={(player?.NetId.ToString() ?? "<null>")}, character={(player?.Character?.Id.Entry ?? "<null>")}, originalResult={DescribeCard(__result)}");
            if (__result != null) return;
            if (player?.Character?.Id?.Entry != Karen.CHAR_ID) return;

            var starter = player.Deck.Cards.FirstOrDefault(c => c.Id == ModelDb.Card<KarenFall>().Id);
            MainFile.Logger.Info($"[ArchaicToothPatch.Starter] Karen fallback lookup result: {DescribeCard(starter)}");
            if (starter != null)
            {
                __result = starter;
                MainFile.Logger.Info($"[ArchaicToothPatch.Starter] Overriding starter card with: {DescribeCard(__result)}");
            }
        }
    }

    /// <summary>
    /// 转换初始牌为先古牌，并继承升级/附魔。
    /// </summary>
    [HarmonyPatch(typeof(ArchaicTooth), "GetTranscendenceTransformedCard")]
    public static class TransformedPatch
    {
        private static bool Prefix(ArchaicTooth __instance, CardModel starterCard, ref CardModel __result)
        {
            if (starterCard?.Id != ModelDb.Card<KarenFall>().Id) return true;

            try
            {
                var owner = starterCard.Owner;
                var runState = owner?.RunState;

                var ancient = starterCard.Owner.RunState.CreateCard(ModelDb.Card<KarenWhy>(), starterCard.Owner);
                MainFile.Logger.Info($"[ArchaicToothPatch.Transform] Created transformed card: {DescribeCard(ancient)}");

                if (starterCard.IsUpgraded)
                {
                    CardCmd.Upgrade(ancient);
                }

                if (starterCard.Enchantment != null)
                {
                    var enchant = (EnchantmentModel)starterCard.Enchantment.MutableClone();
                    MainFile.Logger.Info($"[ArchaicToothPatch.Transform] Cloned enchantment {enchant.Id.Entry} amount={enchant.Amount}.");
                    CardCmd.Enchant(enchant, ancient, enchant.Amount);
                }

                __result = ancient;
                return false;
            }
            catch (System.Exception ex)
            {
                MainFile.Logger.Error($"[ArchaicToothPatch.Transform] Failed. starter={DescribeCard(starterCard)}, relicOwner={(__instance.Owner?.NetId.ToString() ?? "<null>")}, message={ex}");
                throw;
            }
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
            MainFile.Logger.Info($"[ArchaicToothPatch.TranscendenceCards] Before add count={__result.Count}");
            __result.Add(ModelDb.Card<KarenWhy>());
            MainFile.Logger.Info($"[ArchaicToothPatch.TranscendenceCards] Added KarenWhy. after count={__result.Count}");
        }
    }

}
