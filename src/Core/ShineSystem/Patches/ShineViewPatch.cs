using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem.Patches;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System;
using System.Linq;
using System.Reflection;

namespace ShoujoKagekiAijoKaren.src.Core.Shine.ShinePatches;

/// <summary>
/// 全局闪耀显示补丁：补充闪耀描述，并控制需要高亮的卡牌红边框。
/// </summary>
public static class ShineViewPatch
{
    [HarmonyPatch]
    public static class GetDescriptionForPile_Postfix
    {
        public static MethodBase? TargetMethod()
        {
            var descriptionPreviewType = typeof(CardModel).GetNestedType("DescriptionPreviewType", BindingFlags.NonPublic);
            var pileType = typeof(PileType);

            var result = typeof(CardModel).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "GetDescriptionForPile") return false;

                    var parameters = m.GetParameters();
                    return parameters.Length == 3 &&
                           parameters[0].ParameterType == pileType &&
                           parameters[1].ParameterType == descriptionPreviewType &&
                           parameters[2].ParameterType.Name.Contains("Creature");
                });

            if (result == null)
            {
                MainFile.Logger.Error("未找到 CardModel.GetDescriptionForPile，无法应用闪耀描述补丁。");
            }

            return result;
        }

        [HarmonyPostfix]
        public static void Postfix(CardModel __instance, ref string __result)
        {
            if (__instance == null || __result == null) return;
            if (!__instance.IsShineCard()) return;

            var current = __instance.GetShineValue();
            var max = __instance.GetShineMaxValue();

            string coloredNumber;
            if (ShineUpgradePatch.InUpgradePreviewMode(__instance))
            {
                coloredNumber = $"[gold]{Math.Max(current, max)}[/gold]";
            }
            else if (current > max)
            {
                coloredNumber = $"[gold]{current}[/gold]";
            }
            else if (current < max)
            {
                coloredNumber = $"[red]{current}[/red]";
            }
            else
            {
                coloredNumber = current.ToString();
            }

            var label = Tips.ShineLabel.GetFormattedText();
            var suffix = Tips.ShineSuffix.GetFormattedText();
            var shineText = label + coloredNumber + suffix;

            bool hasExhaust = __instance.Keywords?.Contains(CardKeyword.Exhaust) == true;
            __result = __result + (hasExhaust ? "" : "\n") + shineText;
        }
    }

    /// <summary>
    /// 红色边框补丁：
    /// 1. 即将耗尽的 Shine 牌。
    /// 2. Burn 模式下从 Promise Pile 抽出的牌。
    /// </summary>
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.ShouldGlowRed), MethodType.Getter)]
    public static class ShouldGlowRed_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(CardModel __instance, ref bool __result)
        {
            if (__result) return;

            if (KarenPromisePilePower.ShouldGlowRedForBurnDraw(__instance))
            {
                __result = true;
                return;
            }

            if (__instance.IsShineCard() && !__instance.Keywords.Contains(CardKeyword.Eternal))
            {
                if (__instance.GetShineValue() <= 1 || (__instance.Owner?.Creature?.Powers?.Any(p => p is KarenStarlight02Power) == true))
                {
                    __result = true;
                }
            }
        }
    }
}
