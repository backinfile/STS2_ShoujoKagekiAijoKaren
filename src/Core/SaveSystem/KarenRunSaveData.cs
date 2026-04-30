using MegaCrit.Sts2.Core.Saves.Runs;
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
    /// 闪耀牌堆数据（耗尽卡牌列表）。Key = 玩家在 RunState.Players 中的下标。
    /// </summary>
    [JsonPropertyName("player_disposed_pile_data")]
    public Dictionary<int, List<SerializableCard>> PlayerShinePileData { get; set; } = new();
}
