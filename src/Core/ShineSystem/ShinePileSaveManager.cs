
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;
using ShoujoKagekiAijoKaren.src.Core.SaveSystem;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.ShineSystem
{
    // ─────────────────────────────────────────────────────────────────────
    // 存档支持
    // ─────────────────────────────────────────────────────────────────────
    internal class ShinePileSaveManager
    {
        /// <summary>
        /// 遍历所有玩家，收集玩家的闪耀牌堆存档数据。
        /// </summary>
        public static Dictionary<int, List<SerializableCard>> CollectAllPlayersShinePileData(IReadOnlyList<Player> players)
        {
            var result = new Dictionary<int, List<SerializableCard>>();
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                var shinePile = ShinePileManager.GetShinePile(player);
                var serializedPile = new List<SerializableCard>();
                foreach (var card in shinePile.Cards)
                {
                    serializedPile.Add(card.ToSerializable());
                }
                if (serializedPile.Count > 0)
                {
                    result[i] = serializedPile;
                    MainFile.Logger.Info($"[ShinePileSaveManager] 收集耗尽卡牌 playerIndex = {i} count = {result.Count}");
                }
            }
            return result;
        }

        /// <summary>
        /// 恢复所有玩家的闪耀牌堆数据。
        /// </summary>
        public static void RestoreAllPlayersShinePileData(IReadOnlyList<Player> players, KarenRunSaveData data)
        {
            if (data.PlayerShinePileData == null || data.PlayerShinePileData.Count == 0) return;

            int totalRestored = 0;
            foreach (var (playerIdx, pileList) in data.PlayerShinePileData)
            {
                if (playerIdx < 0 || playerIdx >= players.Count)
                {
                    MainFile.Logger.Warn($"[ShinePileSaveManager] 无效的玩家索引 {playerIdx}，无法恢复耗尽卡牌");
                    continue;
                }
                var player = players[playerIdx];
                foreach (var serializableCard in pileList)
                {
                    var card = CardModel.FromSerializable(serializableCard);
                    card.Owner = player;
                    ShinePileManager.AddToShinePileInternal(player, card);
                }
                ShinePileManager.UpdateShineCardDisposedCount(player);
                totalRestored += pileList.Count;
            }
            MainFile.Logger.Info($"[ShinePileManager] 恢复 {data.PlayerShinePileData.Count} 名玩家共 {totalRestored} 张耗尽卡牌");
        }

    }
}
