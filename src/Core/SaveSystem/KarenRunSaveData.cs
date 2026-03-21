using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShoujoKagekiAijoKaren.src.Core.SaveSystem;

/// <summary>
/// Karen Mod 局内存档数据结构，嵌入游戏存档 JSON 的 "karen_mod_data" 字段。
///
/// v2 起使用 PlayerShineData（按玩家下标分组），兼容单机和联机。
/// v1 遗留字段 ShineData 在读取时自动升级为玩家 0 的数据。
/// </summary>
public class KarenRunSaveData
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; set; } = 2;

    /// <summary>
    /// Key = 玩家在 RunState.Players 中的下标（0-based），支持联机多 Karen 玩家。
    /// </summary>
    [JsonPropertyName("player_shine_data")]
    public Dictionary<int, List<ShineSaveData>> PlayerShineData { get; set; } = new();

    /// <summary>v1 遗留字段，仅在读取旧存档时存在，写入时始终为 null。</summary>
    [JsonPropertyName("shine_data")]
    public List<ShineSaveData>? ShineData { get; set; }
}
