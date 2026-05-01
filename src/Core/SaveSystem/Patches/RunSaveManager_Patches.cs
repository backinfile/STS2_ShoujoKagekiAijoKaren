using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ShoujoKagekiAijoKaren.src.Core.SaveSystem;

/// <summary>
/// 在 GodotFileIo 最底层拦截文件读写，将 Mod 数据嵌入游戏存档 JSON 的 players[*].extra_fields。
///
/// 写入流程：
///   RunSaveManager.SaveRun → GodotFileIo.WriteFile(path, bytes)
///   ↳ [Prefix] 将闪耀牌堆注入 players[*].extra_fields → 写入文件
///
/// 读取流程：
///   MigrationManager.LoadSaveFromPath → GodotFileIo.ReadFile(path) → string
///   ↳ [Postfix] 从 players[*].extra_fields 提取闪耀牌堆 → KarenExtraFieldsSaveBuffer
///
/// 恢复时机：在 RunManager.SetUpSavedSinglePlayer/SetUpSavedMultiPlayer Postfix 中消费，
/// 此时 RunState 和卡组 CardModel 实例均已就绪，无需等待战斗开始。
/// 时机与游戏完全一致，数据嵌入同一个 .save 文件，无需伴随文件。
/// System.Text.Json 默认忽略未知字段，游戏的反序列化不受影响。
/// </summary>
[HarmonyPatch]
internal static class RunSaveManager_Patches
{
    private const string PlayersKey = "players";
    private const string ExtraFieldsKey = "extra_fields";

    /// <summary>
    /// 判断路径是否为任意局内存档（单机或联机，含 .backup 后缀）
    /// </summary>
    private static bool IsRunSavePath(string path)
        => path.Contains(RunSaveManager.runSaveFileName)
        || path.Contains(RunSaveManager.multiplayerRunSaveFileName);

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
            if (state == null) return originalBytes;

            using var doc = JsonDocument.Parse(originalBytes);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return originalBytes;
            if (!doc.RootElement.TryGetProperty(PlayersKey, out var playersElement) || playersElement.ValueKind != JsonValueKind.Array)
                return originalBytes;

            using var ms = new MemoryStream(originalBytes.Length + 256);
            using var writer = new Utf8JsonWriter(ms);

            writer.WriteStartObject();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.NameEquals(PlayersKey))
                {
                    WritePlayersWithInjectedFields(writer, playersElement, state.Players);
                    continue;
                }

                prop.WriteTo(writer);
            }
            writer.WriteEndObject();
            writer.Flush();

            MainFile.Logger.Info("[SaveSystem] 已将 extra_fields 扩展字段注入存档");
            return ms.ToArray();
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
            if (!doc.RootElement.TryGetProperty(PlayersKey, out var playersElement) || playersElement.ValueKind != JsonValueKind.Array)
                return;

            int playerIndex = 0;
            foreach (var playerElement in playersElement.EnumerateArray())
            {
                if (playerElement.TryGetProperty(ExtraFieldsKey, out var extraFieldsElement) && extraFieldsElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var handler in PlayerExtraFieldsHandlers.All)
                        handler.ReadPlayerField(extraFieldsElement, playerIndex);
                }
                playerIndex++;
            }

            KarenExtraFieldsSaveBuffer.MarkPending();
            MainFile.Logger.Info("[SaveSystem] 已从 extra_fields 提取扩展字段，等待恢复");
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
        if (!KarenExtraFieldsSaveBuffer.HasPending) return;

        var state = RunManager.Instance.DebugOnlyGetState();
        if (state == null) return;

        if (!KarenExtraFieldsSaveBuffer.Consume())
        {
            MainFile.Logger.Info("[SaveSystem] 无 Mod 数据可供恢复");
            return;
        }

        foreach (var handler in PlayerExtraFieldsHandlers.All)
            handler.RestoreToRunState(state.Players);
    }

    private static void WritePlayersWithInjectedFields(
        Utf8JsonWriter writer,
        JsonElement playersElement,
        IReadOnlyList<Player> players)
    {
        writer.WritePropertyName(PlayersKey);
        writer.WriteStartArray();

        int playerIndex = 0;
        foreach (var playerElement in playersElement.EnumerateArray())
        {
            writer.WriteStartObject();
            foreach (var prop in playerElement.EnumerateObject())
            {
                if (prop.NameEquals(ExtraFieldsKey))
                {
                    WriteExtraFields(writer, prop.Value, players[playerIndex], playerIndex);
                    continue;
                }

                prop.WriteTo(writer);
            }

            if (!playerElement.TryGetProperty(ExtraFieldsKey, out _))
                WriteExtraFields(writer, default, players[playerIndex], playerIndex);

            writer.WriteEndObject();
            playerIndex++;
        }

        writer.WriteEndArray();
    }

    private static void WriteExtraFields(
        Utf8JsonWriter writer,
        JsonElement extraFieldsElement,
        Player player,
        int playerIndex)
    {
        writer.WritePropertyName(ExtraFieldsKey);
        writer.WriteStartObject();

        if (extraFieldsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in extraFieldsElement.EnumerateObject())
            {
                if (IsManagedExtraField(prop))
                    continue;

                prop.WriteTo(writer);
            }
        }

        foreach (var handler in PlayerExtraFieldsHandlers.All)
            handler.WritePlayerField(writer, player);

        writer.WriteEndObject();
    }

    private static bool IsManagedExtraField(JsonProperty prop)
    {
        foreach (var handler in PlayerExtraFieldsHandlers.All)
        {
            if (prop.NameEquals(handler.FieldName))
                return true;
        }

        return false;
    }
}
