using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System.Reflection;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// 当虚无(Ethereal)和固有(Innate)同时出现在一张卡上时，
/// 去掉它们 badge 之间的换行，放在同一行显示。
/// </summary>
[HarmonyPatch]
public static class CardKeywordInlinePatch
{
    static MethodBase TargetMethod()
    {
        var previewType = typeof(CardModel).GetNestedType("DescriptionPreviewType", BindingFlags.NonPublic);
        return AccessTools.Method(typeof(CardModel), "GetDescriptionForPile", new[] { typeof(PileType), previewType, typeof(Creature) });
    }

    private static readonly LocString Period = new("card_keywords", "PERIOD");

    private static string GetKeywordBadgeText(CardKeyword keyword)
    {
        string titleKey = $"{StringHelper.Slugify(keyword.ToString())}.title";
        string title = new LocString("card_keywords", titleKey).GetFormattedText();
        return $"[gold]{title}[/gold]{Period.GetRawText()}";
    }

    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is not KarenBaseCardModel)
            return;

        if (!__instance.Keywords.Contains(CardKeyword.Innate) || !__instance.Keywords.Contains(CardKeyword.Ethereal))
        {
            return;
        }

        string innateBadge = GetKeywordBadgeText(CardKeyword.Innate);
        string etherealBadge = GetKeywordBadgeText(CardKeyword.Ethereal);

        __result = __result
            .Replace($"{innateBadge}\n{etherealBadge}", $"{innateBadge}{etherealBadge}");
    }
}
