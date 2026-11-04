using System.Net;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Modding;

// Install only in a disposable game with EnableErrorUpload and EnableDataUpload initially false.
// Requests terminate in an in-process handler; no synthetic reports can reach the public collector.
[ModInitializer(nameof(Initialize))]
public static class Probe
{
    private static readonly List<string> Payloads = new();
    private static readonly BindingFlags Private = BindingFlags.Static | BindingFlags.NonPublic;
    private static Type? _reporter, _config;
    public static void Initialize()
    {
        try
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name!.StartsWith("ShoujoKagekiAijoKaren.") && a.GetType("ShoujoKagekiAijoKaren.MainFile") != null);
            _config = assembly.GetType("ShoujoKagekiAijoKaren.KarenModConfig")!;
            Check(!(bool)_config.GetProperty("EnableErrorUpload")!.GetValue(null)!, "isolated configuration starts disabled");
            _reporter = assembly.GetType("ShoujoKagekiAijoKaren.src.Core.Telemetry.KarenErrorReporter")!;
            Check(_reporter.GetField("_logger", Private)!.GetValue(null) != null, "engine logger registered");
            ((System.Threading.Timer?)_reporter.GetField("_timer", Private)!.GetValue(null))?.Dispose();
            var queueType = assembly.GetType("ShoujoKagekiAijoKaren.src.Core.Patches.RunHistoryUploadQueue")!;
            var queue = Activator.CreateInstance(queueType, BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object?[] { ProjectSettings.GlobalizePath("user://error-probe-queue"), new Uri("https://example.invalid/probe"),
                    new System.Net.Http.HttpClient(new Handler()), (Func<bool>)(() => (bool)_config.GetProperty("EnableErrorUpload")!.GetValue(null)!),
                    (Action<string>)(_ => {}), null, null, 32, 2097152L, 65536, TimeSpan.Zero }, null);
            _reporter.GetField("_queue", Private)!.SetValue(null, queue);
            Callable.From(Run).CallDeferred();
        }
        catch (Exception error) { Finish(false, error.ToString()); }
    }

    private static void Run()
    {
        try
        {
            _config!.GetProperty("EnableErrorUpload")!.SetValue(null, true);
            for (int i=0;i<3;i++) PrintGame(i);
            GD.PushError("Karen error probe engine failure");
            try { throw new InvalidOperationException("Karen error probe managed failure"); }
            catch (Exception error) { _reporter!.GetMethod("CaptureException", Private)!.Invoke(null, new object[] {error}); }
            _reporter!.GetMethod("Tick", Private)!.Invoke(null, null);
            var reports = Payloads.Select(p => JsonDocument.Parse(p).RootElement.Clone()).ToArray();
            Check(reports.Any(p => p.GetProperty("source").GetString() == "game" && p.GetProperty("count").GetInt32() == 3), "native game errors aggregate");
            Check(reports.Any(p => p.GetProperty("source").GetString() == "engine"), "native GD.PushError captured");
            Check(reports.Any(p => p.GetProperty("source").GetString() == "managed" && p.GetProperty("stack").GetString()!.Contains("Probe.Run")), "managed exception retains stack");
            Check(reports.All(p => p.GetProperty("stack").GetString()!.Length > 0), "actual callbacks retain stack frames");
            Check(reports.All(p => !p.GetProperty("message").GetString()!.Contains("PRIVATE_TOKEN")), "callback content sanitized");
            Check(reports.All(p => p.GetProperty("platform").GetString() == "pc" && p.GetProperty("os").GetString() == "Windows"), "actual PC environment captured");
            string expectedVersion = ModManager.Mods.Single(mod => mod.manifest?.id == "ShoujoKagekiAijoKaren").manifest!.version!;
            Check(reports.All(p => p.GetProperty("build").GetString()!.StartsWith("v0.") && p.GetProperty("mod_version").GetString() == expectedVersion && p.GetProperty("game_commit").GetString() != "unknown"), "actual version metadata captured");
            Check(reports.All(p => p.GetProperty("mods").EnumerateArray().Any(mod => mod.GetProperty("id").GetString() == "BaseLib" && mod.GetProperty("version").GetString() != "unknown")), "other mod versions captured");
            Check(reports.All(p => p.GetProperty("mods").EnumerateArray().Any(mod => mod.GetProperty("id").GetString() == "KarenErrorProbe")), "later mod initializers included");
            int before = Payloads.Count;
            _config.GetProperty("EnableErrorUpload")!.SetValue(null, false);
            PrintGame(99);GD.PushError("Disabled error probe");
            _reporter.GetMethod("Tick", Private)!.Invoke(null, null);
            Check(Payloads.Count == before, "disabled setting blocks capture and upload");
            Finish(true, "game error + engine error + stack + dedup + sanitization + disabled setting");
        }
        catch (Exception error) { Finish(false, error.ToString()); }
    }

    private static void PrintGame(int index) => MegaCrit.Sts2.Core.Logging.Log.Error($"Karen error probe failure {index}\nAuthorization: Bearer PRIVATE_TOKEN");
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Finish(bool passed, string detail)
    {
        _config?.GetProperty("EnableErrorUpload")!.SetValue(null, false);
        File.WriteAllText(ProjectSettings.GlobalizePath("user://error-probe-result.json"), JsonSerializer.Serialize(new { passed, detail, reports = Payloads.Select(p => JsonDocument.Parse(p).RootElement.Clone()).ToArray() }));
        GD.Print("[Karen Error Probe] " + (passed ? "PASS" : "FAIL"));
    }

    private sealed class Handler : System.Net.Http.HttpMessageHandler
    {
        protected override async Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Payloads.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return new System.Net.Http.HttpResponseMessage(HttpStatusCode.Created);
        }
    }
}
