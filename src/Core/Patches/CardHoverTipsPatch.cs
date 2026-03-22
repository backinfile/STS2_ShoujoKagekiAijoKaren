using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// HoverTips补丁 - 从 card_keywords.json 动态添加关键字悬浮提示。
/// 新增关键字：①在 card_keywords.json 添加 KEY.title / KEY.description，
///             ②在此处 Keywords 数组中添加 (key, 适用条件)。
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.HoverTips), MethodType.Getter)]
public static class CardHoverTipsPatch
{
    private static readonly (string Key, Func<CardModel, bool> Condition)[] Keywords =
    [
        ("KAREN_SHINE", card => card.IsShineCard()),
        ("KAREN_PROMISE_PILE", card => card.Tags.Contains(KarenCardTags.PromisePileRelated)),
    ];

    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        var tips = __result.ToList();
        bool modified = false;

        foreach (var (key, condition) in Keywords)
        {
            if (!condition(__instance)) continue;

            var title = new LocString("card_keywords", key + ".title");
            string titleText = title.GetFormattedText();
            if (tips.Any(t => t is HoverTip ht && ht.Title == titleText)) continue;

            var desc = new LocString("card_keywords", key + ".description");
            tips.Add(new HoverTip(title, desc));
            modified = true;
        }

        if (modified)
            __result = tips;
    }
}
