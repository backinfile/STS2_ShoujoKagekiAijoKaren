using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem.Patches;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Players;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;

namespace ShoujoKagekiAijoKaren.src.Core.Shine.ShinePatches;

/// <summary>
/// 全局闪耀补丁 - 在DynamicVars.AddTo后注入KarenShine变量
/// 正确时机：DynamicVars.AddTo之后、GetFormattedText之前
/// </summary>
public static class ShineViewPatch
{

    /// <summary>
    /// Postfix: 在GetDescriptionForPile方法返回后修改结果，注入KarenShine变量
    /// </summary>
    [HarmonyPatch]
    public static class GetDescriptionForPile_Postfix
    {
        public static MethodBase? TargetMethod()
        {
            // 获取 protected 嵌套类型 DescriptionPreviewType
            var descriptionPreviewType = typeof(CardModel).GetNestedType("DescriptionPreviewType",
                BindingFlags.NonPublic);

            // 获取 PileType 类型
            var pileType = typeof(PileType);

            // 查找匹配的方法:
            // private unsafe string GetDescriptionForPile(PileType, DescriptionPreviewType, Creature)
            var result = typeof(CardModel).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "GetDescriptionForPile") return false;

                    var parameters = m.GetParameters();

                    // 检查前两个参数类型，第三个参数Creature通过名称匹配（可能是Creature或Creature?）
                    return parameters.Length == 3 &&
                           parameters[0].ParameterType == pileType &&
                           parameters[1].ParameterType == descriptionPreviewType &&
                           parameters[2].ParameterType.Name.Contains("Creature");
                });
            if (result == null)
            {
                MainFile.Logger.Error("未找到 CardModel.GetDescriptionForPile 方法，无法应用闪耀描述补丁！");
            }

            return result;
        }

        [HarmonyPostfix]
        public static void Postfix(CardModel __instance, ref string __result)
        {
            if (__instance == null || __result == null)
                return;

            // 只处理闪耀牌
            if (!__instance.IsShineCard())
                return;

            var current = __instance.GetShineValue();
            var max = __instance.GetShineMaxValue();

            // 根据 current 与 max 的关系决定颜色，current==1 时加夸张抖动特效
            string coloredNumber;
            if (ShineUpgradePatch.InUpgradePreviewMode(__instance))
                coloredNumber = $"[gold]{Math.Max(current, max)}[/gold]"; // 预览状态下，恢复满闪耀值
            else if (current > max)
                coloredNumber = $"[gold]{current}[/gold]";
            else if (current < max)
                coloredNumber = $"[red]{current}[/red]";
            else
                coloredNumber = current.ToString(); // current == max：白色（默认色）

            var label = Tips.ShineLabel.GetFormattedText();
            var suffix = Tips.ShineSuffix.GetFormattedText();
            string shineText = label + coloredNumber + suffix;

            // 若卡牌有消耗关键字，游戏已将"消耗。"附加在描述末尾，闪耀文本与其同行
            bool hasExhaust = __instance.Keywords?.Contains(CardKeyword.Exhaust) == true;
            __result = __result + (hasExhaust ? "" : "\n") + shineText;
        }
    }



    /// <summary>
    /// ShouldGlowRed补丁 - 当闪耀值为1时，将卡牌边框显示为红色
    /// </summary>
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.ShouldGlowRed), MethodType.Getter)]
    public static class ShouldGlowRed_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(CardModel __instance, ref bool __result)
        {
            if (__result) return; // 已经是红色，不覆盖

            // 非永恒的闪耀牌
            if (__instance.IsShineCard() && !__instance.Keywords.Contains(CardKeyword.Eternal))
            {
                // 即将耗尽
                if (__instance.GetShineValue() <= 1 || (__instance.Owner?.Creature?.Powers?.Any(p => p is KarenStarlight02Power) == true))
                {
                    __result = true;
                }
            }
        }
    }

}
