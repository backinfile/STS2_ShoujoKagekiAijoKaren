using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
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
        ("KAREN_SHINE", card => card.IsShineCard() || card.Tags.Contains(KarenCustomEnum.ShineRelated)),
        ("KAREN_PROMISE_PILE", card => card.Tags.Contains(KarenCustomEnum.PromisePileRelated)),
        ("KAREN_SHINE_CARD_REWARD", card => card.Tags.Contains(KarenCustomEnum.ShineCardReward)),
        ("KAREN_SHINE_CARD_REWARD", card => card.Tags.Contains(KarenCustomEnum.ShineCardReward)),
        ("KAREN_TMP_STRENGTH", card => card.Tags.Contains(KarenCustomEnum.TmpStrength)),
        ("KAREN_RETAIN_TMP_STRENGTH", card => card.Tags.Contains(KarenCustomEnum.RetainTmpStrength)),
        ("KAREN_DISABLE_RELIC", card => card.Tags.Contains(KarenCustomEnum.DisableRelicRelated) || card is KarenDisableRelicBaseCardModel),
    ];

    private static readonly Dictionary<string, HoverTip> HoverTopCache = new();

    private static HoverTip GetHoverTip(string key)
    {
        if (HoverTopCache.TryGetValue(key, out var cachedTip))
            return cachedTip;
        var title = new LocString("card_keywords", key + ".title");
        var desc = new LocString("card_keywords", key + ".description");
        var hoverTip = new HoverTip(title, desc);
        HoverTopCache[key] = hoverTip;
        return hoverTip;
    }

    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        var addTips = new List<IHoverTip>();
        foreach (var (key, condition) in Keywords)
        {
            if (!condition(__instance)) continue;

            addTips.Add(GetHoverTip(key));
        }
        if (addTips.Count > 0)
        {
            __result = __result.Concat(addTips);
        }
    }
}
