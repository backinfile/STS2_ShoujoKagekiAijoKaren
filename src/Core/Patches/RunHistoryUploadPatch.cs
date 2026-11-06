using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

[HarmonyPatch(typeof(RunHistoryUtilities), nameof(RunHistoryUtilities.CreateRunHistoryEntry))]
public class RunHistoryUploadPatch
{
    private static readonly string? UploadUrl = LoadUploadUrl();
    private static readonly HttpClient HttpClient = new();

    private static readonly System.Collections.Generic.HashSet<string> AllowedCharacterIds = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "ironclad", "silent", "defect", "watcher", "karen"
    };

    private static string? LoadUploadUrl()
    {
        try
        {
            using var stream = typeof(RunHistoryUploadPatch).Assembly
                .GetManifestResourceStream("KarenTelemetryEndpoint.json");
            if (stream == null)
                return null;

            using var document = JsonDocument.Parse(stream);
            var url = document.RootElement.GetProperty("Url").GetString();
            return System.Uri.TryCreate(url, System.UriKind.Absolute, out var uri) && uri.Scheme == "https"
                ? url : null;
        }
        catch (System.Exception)
        {
            return null;
        }
    }

    private static void Postfix()
    {
        if (!KarenModConfig.EnableDataUpload || UploadUrl == null)
        {
            MainFile.Logger.Info("[RunHistoryUploadPatch] 数据上传已禁用或未配置，跳过上传");
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
            var json = JsonSerializer.Serialize(history);
            json = RunHistoryShineCache.InjectIntoHistoryJson(json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await HttpClient.PostAsync(UploadUrl, content);

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
