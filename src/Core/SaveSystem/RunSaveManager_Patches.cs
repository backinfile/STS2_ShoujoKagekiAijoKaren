using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ShoujoKagekiAijoKaren.src.Core.SaveSystem;

/// <summary>
/// 在 GodotFileIo 最底层拦截文件读写，将 Mod 数据嵌入游戏存档 JSON。
///
/// 写入流程：
///   RunSaveManager.SaveRun → GodotFileIo.WriteFile(path, bytes)
///   ↳ [Prefix] 将 "karen_mod_data" 字段注入 bytes → 写入文件
///
/// 读取流程：
///   MigrationManager.LoadSaveFromPath → GodotFileIo.ReadFile(path) → string
///   ↳ [Postfix] 从 JSON 字符串提取 "karen_mod_data" → KarenModSaveBuffer
///
/// 恢复时机：在 RunManager.SetUpSavedSinglePlayer/SetUpSavedMultiPlayer Postfix 中消费，
/// 此时 RunState 和卡组 CardModel 实例均已就绪，无需等待战斗开始。
/// 时机与游戏完全一致，数据嵌入同一个 .save 文件，无需伴随文件。
/// System.Text.Json 默认忽略未知字段，游戏的反序列化不受影响。
/// </summary>
[HarmonyPatch]
internal static class RunSaveManager_Patches
{
    private const string ModDataKey = "karen_mod_data";

    /// <summary>
    /// 判断路径是否为单人局存档（current_run.save 或其 .backup 后缀）
    /// 注意：current_run_mp.save 不含 "current_run.save" 子串，不会误匹配
    /// </summary>
    private static bool IsRunSavePath(string path)
        => path.Contains(RunSaveManager.runSaveFileName);

    // ─────────────────────────────────────────────────────────────────────
    // 写入：同步版本
    // WriteFile(string, string) 内部会调用 WriteFile(string, byte[])，只需 patch byte[] 版本
    // ─────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(GodotFileIo), nameof(GodotFileIo.WriteFile),
        new[] { typeof(string), typeof(byte[]) })]
    [HarmonyPrefix]
    private static void WriteFile_Prefix(string path, ref byte[] bytes)
    {
        if (!IsRunSavePath(path)) return;
        bytes = InjectModData(bytes);
    }

    // ─────────────────────────────────────────────────────────────────────
    // 写入：异步版本（SaveRun 默认走此分支）
    // async Task 方法打 Prefix 在方法体执行前运行，ref 修改对 bytes 有效
    // ─────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(GodotFileIo), nameof(GodotFileIo.WriteFileAsync),
        new[] { typeof(string), typeof(byte[]) })]
    [HarmonyPrefix]
    private static void WriteFileAsync_Prefix(string path, ref byte[] bytes)
    {
        if (!IsRunSavePath(path)) return;
        bytes = InjectModData(bytes);
    }

    // ─────────────────────────────────────────────────────────────────────
    // 读取：MigrationManager.LoadSaveFromPath 调用 ReadFile(path) 获取 JSON 字符串
    // Postfix 从结果中提取 Mod 数据存入缓冲区，__result 保持不变（游戏忽略未知字段）
    // ─────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(GodotFileIo), nameof(GodotFileIo.ReadFile))]
    [HarmonyPostfix]
    private static void ReadFile_Postfix(string path, ref string? __result)
    {
        if (!IsRunSavePath(path)) return;
        if (string.IsNullOrWhiteSpace(__result)) return;
        ExtractAndBuffer(__result);
    }

    // ─────────────────────────────────────────────────────────────────────
    // 实现：注入
    // ─────────────────────────────────────────────────────────────────────
    private static byte[] InjectModData(byte[] originalBytes)
    {
        try
        {
            var state = RunManager.Instance.DebugOnlyGetState();
            var player = state?.Players.FirstOrDefault(p => p.Character is Karen);
            if (player == null) return originalBytes; // 非 Karen 局，透传不修改

            var shineData = ShineSaveSystem.CollectShineData(player.Deck.Cards);
            var modData = new KarenRunSaveData { ShineData = shineData };

            using var doc = JsonDocument.Parse(originalBytes);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return originalBytes;

            using var ms = new MemoryStream(originalBytes.Length + 256);
            using var writer = new Utf8JsonWriter(ms);

            writer.WriteStartObject();
            foreach (var prop in doc.RootElement.EnumerateObject())
                prop.WriteTo(writer);
            writer.WritePropertyName(ModDataKey);
            JsonSerializer.Serialize(writer, modData);
            writer.WriteEndObject();
            writer.Flush();

            var result = ms.ToArray();
            MainFile.Logger.Info($"[SaveSystem] 注入 {shineData.Count} 条 Shine 数据到存档");
            return result;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[SaveSystem] 注入 Mod 数据失败，写入原始数据: {ex.Message}");
            return originalBytes; // 保证不损坏存档
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // 实现：提取
    // ─────────────────────────────────────────────────────────────────────
    private static void ExtractAndBuffer(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(ModDataKey, out var modElement)) return;

            var modData = JsonSerializer.Deserialize<KarenRunSaveData>(modElement.GetRawText());
            if (modData == null) return;

            KarenModSaveBuffer.Store(modData);
            MainFile.Logger.Info($"[SaveSystem] 从存档提取 {modData.ShineData.Count} 条 Shine 数据，等待战斗开始时恢复");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[SaveSystem] 提取 Mod 数据失败: {ex.Message}");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // 恢复：SerializableRun 加载进 RunManager 完成时消费缓冲区
    // SetUpSavedSinglePlayer / SetUpSavedMultiPlayer 调用后 State 和卡组均已就绪
    // ─────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpSavedSinglePlayer))]
    [HarmonyPostfix]
    private static void SetUpSavedSinglePlayer_Postfix()
        => ConsumeAndRestore();

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpSavedMultiPlayer))]
    [HarmonyPostfix]
    private static void SetUpSavedMultiPlayer_Postfix()
        => ConsumeAndRestore();

    private static void ConsumeAndRestore()
    {
        if (!KarenModSaveBuffer.HasPending) return;

        var state = RunManager.Instance.DebugOnlyGetState();
        var player = state?.Players.FirstOrDefault(p => p.Character is Karen);
        if (player == null) return;

        var data = KarenModSaveBuffer.Consume();
        if (data == null) return;

        ShineSaveSystem.RestoreShineData(player.Deck.Cards, data.ShineData);
        MainFile.Logger.Info($"[SaveSystem] 恢复 {data.ShineData.Count} 条 Shine 数据到牌组");
    }
}
