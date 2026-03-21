using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShoujoKagekiAijoKaren.src.Core.SaveSystem;

/// <summary>
/// Karen Mod 局内存档数据结构
/// 以伴随文件形式存储，与 current_run.save 同步写入/读取
/// 文件路径：user://profiles/&lt;id&gt;/saves/karen_current_run.json
/// </summary>
public class KarenRunSaveData
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("shine_data")]
    public List<ShineSaveData> ShineData { get; set; } = new();
}
