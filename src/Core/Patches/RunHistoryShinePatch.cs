using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

internal static class RunHistoryShineCache
{
    private const string ShineExhaustCountKey = "karen_shine_exhaust_count";
    private const string LegacyShineExhaustCardsKey = "karen_shine_exhaust_cards";

    private static readonly Dictionary<long, Dictionary<ulong, int>> Cache = new();

    public static long? CurrentDisplayedRunStartTime { get; set; }

    public static string InjectIntoHistoryJson(string content)
    {
        try
        {
            var state = RunManager.Instance?.DebugOnlyGetState();
            if (state?.Players == null || state.Players.Count == 0)
                return content;

            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return content;
            if (!doc.RootElement.TryGetProperty("players", out var playersElement) || playersElement.ValueKind != JsonValueKind.Array)
                return content;

            using var stream = new MemoryStream(content.Length + 256);
            using var writer = new Utf8JsonWriter(stream);

            writer.WriteStartObject();
            foreach (var property in doc.RootElement.EnumerateObject())
            {
                if (property.NameEquals("players"))
                {
                    WritePlayersWithShineData(writer, playersElement, state.Players);
                    continue;
                }

                property.WriteTo(writer);
            }

            writer.WriteEndObject();
            writer.Flush();
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[RunHistoryShinePatch] 注入历史耗尽数量失败，保留原始 json: {ex.Message}");
            return content;
        }
    }

    public static void UpdateFromHistoryJson(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        try
        {
            using var doc = JsonDocument.Parse(content);
            if (!doc.RootElement.TryGetProperty("start_time", out var startTimeElement) || startTimeElement.ValueKind != JsonValueKind.Number)
                return;
            if (!doc.RootElement.TryGetProperty("players", out var playersElement) || playersElement.ValueKind != JsonValueKind.Array)
                return;

            var perPlayer = new Dictionary<ulong, int>();
            foreach (var playerElement in playersElement.EnumerateArray())
            {
                if (!playerElement.TryGetProperty("id", out var idElement) || idElement.ValueKind != JsonValueKind.Number)
                    continue;

                ulong playerId = idElement.GetUInt64();
                int shineExhaustCount = 0;

                if (playerElement.TryGetProperty(ShineExhaustCountKey, out var countElement) && countElement.ValueKind == JsonValueKind.Number)
                {
                    shineExhaustCount = countElement.GetInt32();
                }
                else if (playerElement.TryGetProperty(LegacyShineExhaustCardsKey, out var legacyCardsElement) && legacyCardsElement.ValueKind == JsonValueKind.Array)
                {
                    shineExhaustCount = legacyCardsElement.GetArrayLength();
                }

                perPlayer[playerId] = shineExhaustCount;
            }

            Cache[startTimeElement.GetInt64()] = perPlayer;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[RunHistoryShinePatch] 读取历史耗尽数量失败: {ex.Message}");
        }
    }

    public static int? GetDisplayedPlayerShineExhaustCount(ulong playerId)
    {
        if (!CurrentDisplayedRunStartTime.HasValue)
            return null;
        if (!Cache.TryGetValue(CurrentDisplayedRunStartTime.Value, out var perPlayer))
            return null;
        if (!perPlayer.TryGetValue(playerId, out var count))
            return null;

        return count;
    }

    private static void WritePlayersWithShineData(Utf8JsonWriter writer, JsonElement playersElement, IReadOnlyList<Player> players)
    {
        writer.WritePropertyName("players");
        writer.WriteStartArray();

        foreach (var playerElement in playersElement.EnumerateArray())
        {
            writer.WriteStartObject();

            ulong? playerId = null;
            foreach (var property in playerElement.EnumerateObject())
            {
                if (property.NameEquals("id") && property.Value.ValueKind == JsonValueKind.Number)
                    playerId = property.Value.GetUInt64();

                if (property.NameEquals(ShineExhaustCountKey))
                    continue;

                property.WriteTo(writer);
            }

            var player = playerId.HasValue ? players.FirstOrDefault(p => p.NetId == playerId.Value) : null;
            int shineExhaustCount = player == null ? 0 : ShinePileManager.GetShinePileCount(player);

            writer.WriteNumber(ShineExhaustCountKey, shineExhaustCount);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }
}

[HarmonyPatch(typeof(GodotFileIo), nameof(GodotFileIo.WriteFile), new[] { typeof(string), typeof(string) })]
internal static class RunHistoryWriteFilePatch
{
    [HarmonyPrefix]
    private static void Prefix(string path, ref string content)
    {
        if (!IsRunHistoryPath(path) || string.IsNullOrWhiteSpace(content))
            return;

        content = RunHistoryShineCache.InjectIntoHistoryJson(content);
    }

    private static bool IsRunHistoryPath(string path)
    {
        return path.EndsWith(".run", StringComparison.OrdinalIgnoreCase)
            && path.Replace('\\', '/').Contains("/history/", StringComparison.OrdinalIgnoreCase);
    }
}

[HarmonyPatch(typeof(GodotFileIo), nameof(GodotFileIo.ReadFile))]
internal static class RunHistoryReadFilePatch
{
    [HarmonyPostfix]
    private static void Postfix(string path, string? __result)
    {
        if (!IsRunHistoryPath(path))
            return;

        RunHistoryShineCache.UpdateFromHistoryJson(__result);
    }

    private static bool IsRunHistoryPath(string path)
    {
        return path.EndsWith(".run", StringComparison.OrdinalIgnoreCase)
            && path.Replace('\\', '/').Contains("/history/", StringComparison.OrdinalIgnoreCase);
    }
}

[HarmonyPatch(typeof(NRunHistory), "DisplayRun")]
internal static class NRunHistoryDisplayRunPatch
{
    [HarmonyPrefix]
    private static void Prefix(RunHistory history)
    {
        RunHistoryShineCache.CurrentDisplayedRunStartTime = history.StartTime;
    }
}

[HarmonyPatch(typeof(NDeckHistory), nameof(NDeckHistory.LoadDeck))]
internal static class NDeckHistoryLoadDeckPatch
{
    [HarmonyPostfix]
    private static void Postfix(NDeckHistory __instance, Player player)
    {
        int? shineExhaustCount = RunHistoryShineCache.GetDisplayedPlayerShineExhaustCount(player.NetId);
        if (!shineExhaustCount.HasValue)
            return;

        var headerLabel = __instance.GetNodeOrNull<MegaRichTextLabel>("Header");
        if (headerLabel == null || string.IsNullOrWhiteSpace(headerLabel.Text))
            return;

        const string suffixMark = "耗尽";
        int existingIndex = headerLabel.Text.LastIndexOf(suffixMark, StringComparison.Ordinal);
        if (existingIndex >= 0)
            return;

        headerLabel.Text += $"，{shineExhaustCount.Value}耗尽";
    }
}
