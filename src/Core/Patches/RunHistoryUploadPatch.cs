using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
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

    private static readonly System.Collections.Generic.HashSet<string> AllowedCharacterIds = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "ironclad", "silent", "defect", "watcher", "karen"
    };

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

        if (!ShouldUploadForCurrentCharacter())
        {
            return;
        }

        _ = UploadAsync(history);
    }

    private static bool ShouldUploadForCurrentCharacter()
    {
        try
        {
            var state = RunManager.Instance?.DebugOnlyGetState();
            if (state == null)
            {
                MainFile.Logger.Info("[RunHistoryUploadPatch] 无法获取 RunState，跳过上传");
                return false;
            }

            var player = state.Players?.FirstOrDefault();
            if (player == null)
            {
                MainFile.Logger.Info("[RunHistoryUploadPatch] 无法获取玩家，跳过上传");
                return false;
            }

            var charId = player.Character?.Id?.Entry;
            if (string.IsNullOrEmpty(charId))
            {
                MainFile.Logger.Info("[RunHistoryUploadPatch] 无法获取角色 ID，跳过上传");
                return false;
            }

            if (!AllowedCharacterIds.Contains(charId))
            {
                MainFile.Logger.Info($"[RunHistoryUploadPatch] 角色 {charId} 不在上传白名单中，跳过上传");
                return false;
            }

            return true;
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Warn($"[RunHistoryUploadPatch] 检查角色时异常，默认允许上传: {ex.Message}");
            return true;
        }
    }

    private static async System.Threading.Tasks.Task UploadAsync(RunHistory history)
    {
        try
        {
            var endpoint = Endpoint.Value;
            if (endpoint == null)
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
            var bytes = Encoding.UTF8.GetBytes(body);
            var manifest = JsonSerializer.Serialize(new
            {
                sha256 = System.Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
                bytes = bytes.Length
            });
            using var sessionRequest = new HttpRequestMessage(HttpMethod.Post, endpoint.Url.TrimEnd('/') + "/session");
            sessionRequest.Content = new StringContent(manifest, Encoding.UTF8, "application/json");
            using var sessionResponse = await HttpClient.SendAsync(sessionRequest);
            if (!sessionResponse.IsSuccessStatusCode)
            {
                MainFile.Logger.Warn($"[RunHistoryUploadPatch] 申请上传令牌失败: {sessionResponse.StatusCode}");
                return;
            }
            using var session = JsonDocument.Parse(await sessionResponse.Content.ReadAsStringAsync());
            var token = session.RootElement.GetProperty("upload_token").GetString();
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await HttpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                MainFile.Logger.Info($"[RunHistoryUploadPatch] 对局数据上传成功: {response.StatusCode}");
            }
            else
            {
                MainFile.Logger.Warn($"[RunHistoryUploadPatch] 对局数据上传失败: {response.StatusCode}");
            }
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Warn($"[RunHistoryUploadPatch] 上传异常: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
