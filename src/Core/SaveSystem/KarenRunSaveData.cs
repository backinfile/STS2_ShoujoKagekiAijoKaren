using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShoujoKagekiAijoKaren.src.Core.SaveSystem;

/// <summary>
/// Karen Mod 局内存档数据结构，嵌入游戏存档 JSON 的 "karen_mod_data" 字段。
/// 按玩家下标分组，兼容单机和联机。
///</summary>
public class KarenRunSaveData
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; set; } = 2;

    /// <summary>
    /// Key = 玩家在 RunState.Players 中的下标（0-based），支持联机多 Karen 玩家。
    /// </summary>
    [JsonPropertyName("player_shine_data")]
    public Dictionary<int, List<ShineSaveData>> PlayerShineData { get; set; } = new();

    /// <summary>
    /// 闪耀牌堆数据（耗尽卡牌列表）。Key = 玩家在 RunState.Players 中的下标。
    /// ShineSaveData.ShineCurrent 恒为 0（已耗尽），ShineMax 保留原始值。
    /// ShineSaveData.Index 为该卡牌在 Deck.Cards 中的下标（-1 表示已从 Deck 移出）。
    /// </summary>
    [JsonPropertyName("player_shine_pile_data")]
    public Dictionary<int, List<ShineSaveData>> PlayerShinePileData { get; set; } = new();
}
