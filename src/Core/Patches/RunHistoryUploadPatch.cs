using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

[HarmonyPatch(typeof(RunHistoryUtilities), nameof(RunHistoryUtilities.CreateRunHistoryEntry))]
public class RunHistoryUploadPatch
{
    private static readonly HttpClient HttpClient = new() { Timeout = System.TimeSpan.FromSeconds(30) };
    private static readonly System.Lazy<UploadEndpoint?> Endpoint = new(LoadEndpoint);
    private static RunHistoryUploadQueue? _queue;

    private static RunHistoryUploadQueue? Queue()
    {
        var endpoint = Endpoint.Value;
        if (endpoint == null) return null;
        return _queue ??= new RunHistoryUploadQueue(Godot.ProjectSettings.GlobalizePath("user://karen-telemetry/pending"),
            new System.Uri(endpoint.Url), HttpClient, () => KarenModConfig.EnableDataUpload, message => MainFile.Logger.Warn(message));
    }

    internal static void ResumePendingUploads()
    {
        if (!KarenModConfig.EnableDataUpload) return;
        try { var queue = Queue(); if (queue != null) _ = queue.FlushAsync(); }
        catch (System.Exception error) { MainFile.Logger.Warn($"[RunHistoryUploadPatch] 暂无法恢复待上传队列：{error.GetType().Name}"); }
    }

    private sealed class UploadEndpoint
    {
        public string Url { get; set; } = "";
    }

    private static UploadEndpoint? LoadEndpoint()
    {
        try
        {
            using var stream = typeof(RunHistoryUploadPatch).Assembly.GetManifestResourceStream("KarenTelemetryEndpoint.json");
            if (stream == null)
                return null;
            var endpoint = JsonSerializer.Deserialize<UploadEndpoint>(stream);
            return endpoint != null &&
                System.Uri.TryCreate(endpoint.Url, System.UriKind.Absolute, out var uri) && uri.Scheme == "https"
                ? endpoint : null;
        }
        catch (System.Exception)
        {
            MainFile.Logger.Warn("[RunHistoryUploadPatch] 数据上传配置无效，跳过上传");
            return null;
        }
    }

    private static void RemoveShineValues(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            obj.Remove("karen_shine_current");
            obj.Remove("karen_shine_max");
            foreach (var property in obj)
                RemoveShineValues(property.Value);
        }
        else if (node is JsonArray array)
        {
            for (int i = array.Count - 1; i >= 0; i--)
            {
                if (array[i] is JsonObject saved && saved["name"] is JsonValue name &&
                    name.TryGetValue<string>(out var text) && text is "karen_shine_current" or "karen_shine_max")
                    array.RemoveAt(i);
                else
                    RemoveShineValues(array[i]);
            }
        }
    }

    private static void Postfix()
    {
        if (!KarenModConfig.EnableDataUpload)
        {
            MainFile.Logger.Info("[RunHistoryUploadPatch] 数据上传已禁用，跳过上传");
            return;
        }

        var history = RunManager.Instance.History;
        if (history == null)
        {
            MainFile.Logger.Warn("[RunHistoryUploadPatch] RunManager.Instance.History 为 null，跳过上传");
            return;
        }

        if (!RunHistoryUploadQueue.ShouldUploadCharacters(history.Players.Select(player => player.Character.Entry)))
        {
            return;
        }

        Enqueue(history);
    }

    private static void Enqueue(RunHistory history)
    {
        try
        {
            var queue = Queue();
            if (queue == null)
            {
                MainFile.Logger.Info("[RunHistoryUploadPatch] 未配置数据上传服务，跳过上传");
                return;
            }

            // The native contract includes public fields such as was_picked.
            var json = JsonSerializationUtility.ToJson(history);
            json = RunHistoryShineCache.InjectIntoHistoryJson(json);
            var payload = JsonNode.Parse(json);
            RemoveShineValues(payload);
            var body = payload!.ToJsonString();
            if (queue.Enqueue(body)) _ = queue.FlushAsync();
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Warn($"[RunHistoryUploadPatch] 上传异常: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
