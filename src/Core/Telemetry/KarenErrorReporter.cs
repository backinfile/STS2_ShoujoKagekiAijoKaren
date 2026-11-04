using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Modding;
using ShoujoKagekiAijoKaren.src.Core.Patches;

namespace ShoujoKagekiAijoKaren.src.Core.Telemetry;

/// <summary>The callback only captures bounded strings; a single background timer persists them.</summary>
internal static class KarenErrorReporter
{
    private static readonly System.Net.Http.HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(15), MaxResponseContentBufferSize = 4096 };
    private static ClientErrorBuffer? _buffer;
    private static RunHistoryUploadQueue? _queue;
    private static KarenErrorLogger? _logger;
    private static System.Threading.Timer? _timer;
    private static Task _upload = Task.CompletedTask;
    private static int _ticking;
    [ThreadStatic] private static bool _capturing;

    internal static void Initialize()
    {
        if (_timer != null) return;
        try
        {
            var endpoint = RunHistoryUploadPatch.ErrorEndpoint;
            if (endpoint == null) return;
            using var manifest = typeof(KarenErrorReporter).Assembly.GetManifestResourceStream("KarenErrorManifest.json");
            using var manifestJson = manifest == null ? null : JsonDocument.Parse(manifest);
            string version = manifestJson?.RootElement.GetProperty("version").GetString() ?? "unknown";
            string os = OS.GetName();
#if KAREN_ANDROID
            const string platform = "android";
#else
            string platform = os == "Android" ? "android" : "pc";
#endif
            var release = ReleaseInfoManager.Instance.ReleaseInfo;
            // Discovery and disabled-mod filtering precede ModInitializer callbacks. Include mods
            // scheduled after Karen and failed initializers, not just those already fully loaded.
            var mods = ModManager.Mods.Where(mod => mod.manifest?.id != null && mod.manifest.id != MainFile.ModId &&
                    mod.state is ModLoadState.None or ModLoadState.Loaded or ModLoadState.Failed)
                .Select(mod => new ClientErrorBuffer.ModInfo(mod.manifest!.id!, mod.manifest.name ?? mod.manifest.id!, mod.manifest.version ?? "unknown"))
                .OrderBy(mod => mod.id, StringComparer.Ordinal).Take(129).ToArray();
            _buffer = new ClientErrorBuffer(release?.Version ?? "unknown", version, platform,
                os: os, gameCommit: release?.Commit ?? "unknown", mods: mods);
            _queue = new RunHistoryUploadQueue(ProjectSettings.GlobalizePath("user://karen-telemetry/errors"),
                endpoint, Client, () => KarenModConfig.EnableErrorUpload, _ => { },
                maxItems: 32, maxBytes: 2 * 1024 * 1024, maxPayloadBytes: 64 * 1024,
                requestInterval: TimeSpan.FromMinutes(1));
            _logger = new KarenErrorLogger();
            OS.AddLogger(_logger);
            // Do not mark unobserved exceptions as handled or alter normal crash behavior.
            TaskScheduler.UnobservedTaskException += (_, e) => CaptureException(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, e) => CaptureException(e.ExceptionObject);
            _timer = new System.Threading.Timer(_ => Tick(), null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1));
        }
        catch (Exception)
        {
            // Reporting must never become another source of reports.
        }
    }

    internal static void ConfigurationChanged()
    {
        if (!KarenModConfig.EnableErrorUpload) _buffer?.Clear();
    }

    internal static void CaptureException(object? error)
    {
        if (!KarenModConfig.EnableErrorUpload || _buffer == null || _capturing) return;
        try
        {
            string text = error?.ToString() ?? "Unhandled exception";
            if (text.Length > 16384) text = text[..16384];
            int newline = text.IndexOf('\n');
            Capture("managed", newline < 0 ? text : text[..newline], newline < 0 ? "" : text[(newline + 1)..]);
        }
        catch (Exception) { }
    }

    internal static void Capture(string source, string message, string stack, Func<string>? stackFactory = null)
    {
        if (_capturing || !KarenModConfig.EnableErrorUpload || _buffer == null) return;
        try { _capturing = true; _buffer.Capture(source, message, stack, stackFactory); }
        catch (Exception) { }
        finally { _capturing = false; }
    }

    private static void Tick()
    {
        if (Interlocked.Exchange(ref _ticking, 1) != 0) return;
        try
        {
            if (!KarenModConfig.EnableErrorUpload || _buffer == null || _queue == null) return;
            _buffer.Persist(payload => KarenModConfig.EnableErrorUpload && _queue.Enqueue(payload));
            // Keep at most one upload worker, including while backing off for an unavailable server.
            if (_upload.IsCompleted) _upload = _queue.FlushAsync();
        }
        catch (Exception) { }
        finally { Volatile.Write(ref _ticking, 0); }
    }
}

