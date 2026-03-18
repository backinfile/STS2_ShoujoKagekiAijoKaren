using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ShoujoKagekiAijoKaren.src.KarenMod.Patches;

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
        public static MethodBase TargetMethod()
        {
            GD.Print("[ShineGlobalPatch] TargetMethod: 开始查找 GetDescriptionForPile...");

            // 获取 protected 嵌套类型 DescriptionPreviewType
            var descriptionPreviewType = typeof(CardModel).GetNestedType("DescriptionPreviewType",
                BindingFlags.NonPublic);
            GD.Print($"[ShineGlobalPatch] TargetMethod: DescriptionPreviewType = {descriptionPreviewType}");

            // 获取 PileType 类型
            var pileType = typeof(PileType);

            // 查找匹配的方法:
            // private unsafe string GetDescriptionForPile(PileType, DescriptionPreviewType, Creature)
            var result = typeof(CardModel).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "GetDescriptionForPile") return false;

                    var parameters = m.GetParameters();
                    GD.Print($"[ShineGlobalPatch] TargetMethod: 检查方法，参数数量={parameters.Length}");
                    if (parameters.Length == 3)
                    {
                        GD.Print($"[ShineGlobalPatch] TargetMethod:   [0]={parameters[0].ParameterType}, [1]={parameters[1].ParameterType}, [2]={parameters[2].ParameterType.Name}");
                    }

                    // 检查前两个参数类型，第三个参数Creature通过名称匹配（可能是Creature或Creature?）
                    return parameters.Length == 3 &&
                           parameters[0].ParameterType == pileType &&
                           parameters[1].ParameterType == descriptionPreviewType &&
                           parameters[2].ParameterType.Name.Contains("Creature");
                });

            if (result != null)
            {
                GD.Print($"[ShineGlobalPatch] TargetMethod: 找到方法！{result}");
            }
            else
            {
                GD.Print("[ShineGlobalPatch] TargetMethod: 未找到匹配的方法！");
            }

            return result;
        }

        [HarmonyPostfix]
        public static void Postfix(CardModel __instance, ref string __result)
        {
            GD.Print($"[ShineGlobalPatch] Postfix: 被调用，卡牌={__instance?.GetType().Name}");

            if (__instance == null || __result == null)
            {
                GD.Print("[ShineGlobalPatch] Postfix: ✗ 空引用！");
                return;
            }

            // 检查是否已初始化闪耀
            bool isInitialized = __instance.IsShineInitialized();
            GD.Print($"[ShineGlobalPatch] Postfix: 卡牌 '{__instance.Title}' 是否初始化闪耀={isInitialized}");

            if (!isInitialized)
            {
                GD.Print($"[ShineGlobalPatch] Postfix: 跳过（未初始化闪耀）{__instance.Title}");
                return;
            }

            // 获取当前闪耀值
            var currentValue = __instance.GetShineValue();

            // 创建新的 LocString 用于格式化
            var locString = new LocString("gameplay_ui", "KAREN_SHINE_KEY");
            locString.Add(ShineExtension.ShineVarName, currentValue);

            // 获取格式化后的闪耀文本
            string shineText = locString.GetFormattedText();

            // 将闪耀文本追加到结果中
            __result = __result + "\n" + shineText;

            GD.Print($"[ShineGlobalPatch] Postfix: ✓ 成功追加 KarenShine={currentValue} for {__instance.GetType().Name}");
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

            GD.Print($"[ShineGlobalPatch] MutableClone: 克隆卡牌 {source.GetType().Name} -> {clone.GetType().Name}");

            // 检查原卡牌是否已初始化闪耀
            if (!source.IsShineInitialized())
            {
                GD.Print($"[ShineGlobalPatch] MutableClone: 原卡牌未初始化闪耀，跳过");
                return;
            }

            // 复制闪耀值
            int currentValue = source.GetShineValue();
            int maxValue = source.GetShineMaxValue();

            // 使用内部方法直接设置，避免触发其他逻辑
            clone.SetShineValue(currentValue);
            // 同时设置最大值（SetShineValue已经同时设置了当前值和最大值）
            // 但我们可能需要分别设置，因为当前值可能已经减少
            if (currentValue != maxValue)
            {
                clone.SetShineMax(maxValue);
                clone.SetShineCurrent(currentValue);
            }

            GD.Print($"[ShineGlobalPatch] MutableClone: ✓ 复制闪耀值 current={currentValue}, max={maxValue}");
        }
    }

    /// <summary>
    /// HoverTips补丁 - 添加闪耀提示
    /// </summary>
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.HoverTips), MethodType.Getter)]
    public static class HoverTips_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
        {
            if (__instance.IsShineInitialized())
            {
                var tips = __result.ToList();

                bool alreadyHasShineTip = tips.Any(t =>
                {
                    if (t is HoverTip ht)
                    {
                        return ht.Title.Contains("Shine") || ht.Title.Contains("闪耀");
                    }
                    return false;
                });

                if (!alreadyHasShineTip)
                {
                    var shineTitle = new LocString("cards", "KAREN_SHINE_KEYWORD.title");
                    var shineDesc = new LocString("cards", "KAREN_SHINE_KEYWORD.description");
                    tips.Add(new HoverTip(shineTitle, shineDesc));
                    __result = tips;
                }
            }
        }
    }
}
