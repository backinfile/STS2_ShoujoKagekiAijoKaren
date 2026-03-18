using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

/// <summary>
/// 闪耀保存数据 - 用于序列化
/// </summary>
public class ShineSaveData
{
    public string CardId { get; set; } = "";
    public int ShineCurrent { get; set; }
    public int ShineMax { get; set; }
    public int? UpgradeLevel { get; set; }
}

/// <summary>
/// 闪耀保存系统 - 处理跨战斗保存
///
/// 说明：SpireField 的数据不会自动保存到存档中。
/// 需要额外机制来实现跨战斗保存。
///
/// 使用方式：
/// 1. 在保存前调用 CollectShineData 收集数据
/// 2. 将数据存储到自定义保存位置（如 BaseLib 扩展保存系统）
/// 3. 在加载后调用 RestoreShineData 恢复数据
/// </summary>
public static class ShineSaveSystem
{
    /// <summary>
    /// 收集所有卡牌的闪耀值数据
    /// </summary>
    public static List<ShineSaveData> CollectShineData(IEnumerable<CardModel> cards)
    {
        var result = new List<ShineSaveData>();

        foreach (var card in cards)
        {
            var shineCurrent = card.GetShineValue();
            var shineMax = card.GetShineMaxValue();
            if (shineMax > 0)
            {
                result.Add(new ShineSaveData
                {
                    CardId = card.Id.Entry,
                    ShineCurrent = shineCurrent,
                    ShineMax = shineMax,
                    UpgradeLevel = card.IsUpgraded ? card.CurrentUpgradeLevel : (int?)null
                });
            }
        }

        MainFile.Logger.Info($"[ShineSaveSystem] 收集了 {result.Count} 张卡牌的闪耀值数据");
        return result;
    }

    /// <summary>
    /// 恢复闪耀值到卡牌
    /// </summary>
    public static void RestoreShineData(IEnumerable<CardModel> cards, List<ShineSaveData> saveData)
    {
        if (saveData == null || saveData.Count == 0) return;

        // 创建查找字典
        var dataLookup = saveData
            .GroupBy(d => d.CardId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var card in cards)
        {
            if (dataLookup.TryGetValue(card.Id.Entry, out var matchingData))
            {
                // 找到匹配的保存数据（考虑升级状态）
                int? cardUpgradeLevel = card.IsUpgraded ? card.CurrentUpgradeLevel : (int?)null;
                var bestMatch = matchingData.FirstOrDefault(d => d.UpgradeLevel == cardUpgradeLevel)
                             ?? matchingData.First();

                // 恢复当前值和最大值
                card.SetShineMax(bestMatch.ShineMax);
                card.SetShineCurrent(bestMatch.ShineCurrent);
                MainFile.Logger.Info($"[ShineSaveSystem] 恢复卡牌 {card.Id.Entry} 的闪耀值: {bestMatch.ShineCurrent}/{bestMatch.ShineMax}");

                // 从待处理列表移除
                matchingData.Remove(bestMatch);
                if (matchingData.Count == 0)
                {
                    dataLookup.Remove(card.Id.Entry);
                }
            }
        }
    }
}
