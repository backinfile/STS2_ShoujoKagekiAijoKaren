using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System;
using System.Linq;
using System.Reflection;

namespace ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem.Patches;

/// <summary>
/// 禁用遗物系统描述补丁 - 为KarenDisableRelicBaseCardModel卡牌添加"当前可禁用n个遗物"描述
/// </summary>
public static class DisableRelicViewPatch
{
    /// <summary>
    /// Postfix: 在GetDescriptionForPile方法返回后修改结果，注入可禁用遗物数量
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
                MainFile.Logger.Error("未找到 CardModel.GetDescriptionForPile 方法，无法应用禁用遗物描述补丁！");
            }

            return result;
        }

        [HarmonyPostfix]
        public static void Postfix(CardModel __instance, ref string __result)
        {
            if (__instance == null || __result == null)
                return;

            // 只在爬塔进行中时显示
            if (RunManager.Instance?.IsInProgress != true)
                return;

            // 只处理继承自 KarenDisableRelicBaseCardModel 的卡牌
            if (__instance is not KarenDisableRelicBaseCardModel disableRelicCard)
                return;

            // 获取可禁用遗物数量
            var owner = __instance.Owner;
            if (owner == null)
                return;

            int count = DisableRelicManager.GetDisableableRelicCount(owner);

            // 构建描述文本
            var label = Tips.DisableRelicLabel.GetFormattedText();
            var suffix = Tips.DisableRelicSuffix.GetFormattedText();
            string disableRelicText = $"\n{label}[blue]{count}[/blue]{suffix}";

            __result += disableRelicText;
        }
    }
}
