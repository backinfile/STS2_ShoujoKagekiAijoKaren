using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.SaveSystem;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

/// <summary>
/// 闪耀保存数据 - 用于序列化
/// </summary>
public class ShineSaveData
{
    public string CardId { get; set; } = "";

    public int Index { get; set; }
    public int ShineCurrent { get; set; }
    public int ShineMax { get; set; }
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
        for(int i = 0; i < cards.Count(); i++)
        {
            var card = cards.ElementAt(i);
            var shineCurrent = card.GetShineValue();
            var shineMax = card.GetShineMaxValue();
            if (shineMax > 0)
            {
                result.Add(new ShineSaveData
                {
                    CardId = card.Id.Entry,
                    Index = i,
                    ShineCurrent = shineCurrent,
                    ShineMax = shineMax
                });
                MainFile.Logger.Info($"[ShineSaveSystem] 收集卡牌 {card.Id.Entry} 的闪耀值: {shineCurrent}/{shineMax}");
            }
        }
        return result;
    }

    /// <summary>
    /// 遍历所有玩家，按下标收集 Karen 玩家的 Shine 数据，返回用于写入存档的字典。
    /// </summary>
    public static Dictionary<int, List<ShineSaveData>> CollectAllPlayersShineData(IReadOnlyList<Player> players)
    {
        var result = new Dictionary<int, List<ShineSaveData>>();
        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            var shine = CollectShineData(p.Deck.Cards);
            if (shine.Count > 0)
                result[i] = shine;
        }
        return result;
    }

    /// <summary>
    /// 将存档数据恢复到对应玩家的牌组。
    /// </summary>
    public static void RestoreAllPlayersShineData(IReadOnlyList<Player> players, KarenRunSaveData data)
    {
        int totalRestored = 0;
        foreach (var (playerIdx, shineList) in data.PlayerShineData)
        {
            if (playerIdx < 0 || playerIdx >= players.Count) continue;
            var p = players[playerIdx];
            RestoreShineData(p.Deck.Cards, shineList);
            totalRestored += shineList.Count;
        }
        MainFile.Logger.Info($"[ShineSaveSystem] 恢复 {data.PlayerShineData.Count} 名玩家共 {totalRestored} 条 Shine 数据");
    }

    /// <summary>
    /// 恢复闪耀值到卡牌
    /// </summary>
    public static void RestoreShineData(IEnumerable<CardModel> cards, List<ShineSaveData> saveData)
    {
        if (saveData == null || saveData.Count == 0) return;

        // 根据牌组索引进行匹配
        foreach (var saveEntry in saveData)
        {
            int index = saveEntry.Index;
            if (index >= 0 && index < cards.Count())
            {
                var card = cards.ElementAt(index);
                if (card.Id.Entry == saveEntry.CardId)
                {
                    // 恢复当前值和最大值
                    card.SetShineMax(saveEntry.ShineMax);
                    card.SetShineCurrent(saveEntry.ShineCurrent);
                    MainFile.Logger.Info($"[ShineSaveSystem] 恢复卡牌 {card.Id.Entry} 的闪耀值: {saveEntry.ShineCurrent}/{saveEntry.ShineMax}");
                } else
                {
                    MainFile.Logger.Warn($"[ShineSaveSystem] 牌组索引 {index} 的卡牌 ID {card.Id.Entry} 与保存数据中的 ID {saveEntry.CardId} 不匹配，无法恢复闪耀值{saveEntry.ShineCurrent}/{saveEntry.ShineMax}");
                }
            } else
            {
                MainFile.Logger.Warn($"[ShineSaveSystem] 索引 {index} 超出牌组范围，无法恢复卡牌 {saveEntry.CardId} 的闪耀值 {saveEntry.ShineCurrent}/{saveEntry.ShineMax}");
            }
        }
    }
}