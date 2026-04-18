using System.Net.Http;
using System.Text;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

[HarmonyPatch(typeof(RunHistoryUtilities), nameof(RunHistoryUtilities.CreateRunHistoryEntry))]
public class RunHistoryUploadPatch
{
    private const string UploadUrl = "http://localhost:9210/api/upload";
    private static readonly HttpClient HttpClient = new();

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

        _ = UploadAsync(history);
    }

    private static async System.Threading.Tasks.Task UploadAsync(RunHistory history)
    {
        try
        {
            var json = JsonSerializer.Serialize(history);
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
