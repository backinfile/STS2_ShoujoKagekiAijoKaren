using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System.Linq;
using System.Reflection;

namespace ShoujoKagekiAijoKaren.src.Core.Shine.ShinePatches;

/// <summary>
/// 全局闪耀补丁 - 在DynamicVars.AddTo后注入KarenShine变量
/// 正确时机：DynamicVars.AddTo之后、GetFormattedText之前
/// </summary>
public static class ShineGlobalPatch
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
            if (current < max)
                coloredNumber = $"[red]{current}[/red]";
            else if (current > max)
                coloredNumber = $"[blue]{current}[/blue]";
            else
                coloredNumber = current.ToString(); // current == max：白色（默认色）

            var label = new LocString("gameplay_ui", "KAREN_SHINE_LABEL").GetFormattedText();
            var suffix = new LocString("gameplay_ui", "KAREN_SHINE_SUFFIX").GetFormattedText();
            string shineText = label + coloredNumber + suffix;

            __result = __result + "\n" + shineText;
        }
    }

    /// <summary>
    /// MutableClone补丁 - 在卡牌克隆时复制闪耀值
    /// 解决SpireField数据在MemberwiseClone后丢失的问题
    /// </summary>
    [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.MutableClone))]
    public static class MutableClone_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(AbstractModel __instance, AbstractModel __result)
        {
            // __instance 是原卡牌（canonical 或 mutable）
            // __result 是新克隆的卡牌（mutable）
            if (__instance is not CardModel source || __result is not CardModel clone)
                return;

            // 只处理闪耀牌
            if (!source.IsShineCard())
                return;

            // 复制闪耀值
            int currentValue = source.GetShineValue();
            int maxValue = source.GetShineMaxValue();

            // 使用内部方法直接设置，避免触发其他逻辑
            clone.AddShineMax(currentValue);
            // 同时设置最大值（SetShineValue已经同时设置了当前值和最大值）
            // 但我们可能需要分别设置，因为当前值可能已经减少
            if (currentValue != maxValue)
            {
                clone.SetShineMax(maxValue);
                clone.SetShineCurrent(currentValue);
            }
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
            if (__instance.IsShineCard() && __instance.GetShineValue() == 1)
                __result = true;
        }
    }

}
