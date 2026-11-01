using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Saves;

namespace ShoujoKagekiAijoKaren.src.Core.MusicPlayer;

public enum KarenMusicPlayMode { List, Random, RepeatOne }

// Local presentation only: no game RNG, run data or network messages.
internal sealed class KarenMusicPlayback : IDisposable
{
    internal const string Bus = "KarenPlayerMusic";
    internal static KarenMusicPlayback? Current { get; private set; }
    internal static bool OverridesBgm => Current is { IsPlaying: true };
    private readonly AudioStreamPlayer _player;
    private int _request;
    private double? _pausedSeek;
    private string? _forcedPath;
    private bool _disposed, _resumeAfterForm;
    internal bool FormPriority { get; private set; }
    internal readonly List<string> Tracks = new();
    private readonly Dictionary<string, string> _titles = new();
    internal bool UsesBuiltin => string.IsNullOrWhiteSpace(KarenModConfig.MusicDirectory);
    private const string BuiltinRoot = "res://ShoujoKagekiAijoKaren/audio/music_player/";
    internal int Index { get; private set; } = -1;
    internal bool IsPlaying => !_disposed && _player.Playing && !_player.StreamPaused;
    internal bool IsPaused => !_disposed && _player.StreamPaused && _player.Stream != null;
    internal bool Loading { get; private set; }
    internal bool HasStream => _player.Stream != null;
    internal string Status { get; private set; } = "unset";
    internal double Position => _pausedSeek ?? (_player.Stream == null ? 0 : _player.GetPlaybackPosition());
    internal double Duration => _player.Stream?.GetLength() ?? 0;
    internal string Title => _forcedPath != null ? Audio.KarenFormMusicManager.TrackTitle : FormPriority ? Text("awakening") : Index >= 0 && Index < Tracks.Count ? TrackTitle(Tracks[Index]) : Text(Status);
    internal string TrackTitle(string path) => _titles.TryGetValue(path, out var title) ? title : Path.GetFileNameWithoutExtension(path);
    internal event Action? Changed;
    internal event Action<string>? TrackStarted;

    internal KarenMusicPlayback(Node owner)
    {
        Current?.Dispose();
        Current = this;
        EnsureBus();
        _player = new AudioStreamPlayer { Name = "LocalMusicAudio", Bus = Bus };
        owner.AddChild(_player);
        _player.Finished += OnFinished;
        FormPriority = Audio.CombatBgmReplacementManager.CutscenePriority;
        UpdateVolumes();
    }

    internal void Reload()
    {
        Stop();
        Tracks.Clear();
        _titles.Clear();
        Index = -1;
        string directory = KarenModConfig.MusicDirectory.Trim().Trim('"');
        Status = "unset";
        if (directory.Length == 0)
        {
            try
            {
                using var catalog = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(BuiltinRoot + "catalog.json"));
                foreach (var track in catalog.RootElement.EnumerateArray())
                {
                    var path = BuiltinRoot + track.GetProperty("file").GetString();
                    Tracks.Add(path);
                    _titles[path] = track.GetProperty("title").GetString() ?? "";
                }
                Status = Tracks.Count > 0 ? "ready" : "empty";
                if (Tracks.Count > 0) Index = 0;
            }
            catch (Exception error)
            {
                Status = "empty";
                MainFile.Logger.Info($"[MusicPlayer] Built-in catalog unavailable ({error.GetType().Name}).");
            }
        }
        else
        {
            try
            {
                directory = Path.GetFullPath(System.Environment.ExpandEnvironmentVariables(directory));
                if (!Directory.Exists(directory)) Status = "missing";
                else
                {
                    Tracks.AddRange(Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
                        .Where(path => Path.GetExtension(path).ToLowerInvariant() is ".mp3" or ".ogg" or ".wav")
                        .OrderBy(path => Path.GetFileName(path), StringComparer.CurrentCultureIgnoreCase));
                    Status = Tracks.Count == 0 ? "empty" : "ready";
                    if (Tracks.Count > 0) Index = 0;
                }
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                Status = "missing";
                MainFile.Logger.Info($"[MusicPlayer] Directory unavailable ({error.GetType().Name}).");
            }
        }
        Changed?.Invoke();
    }

    internal Task Play(int index) => index < 0 || index >= Tracks.Count ? Task.CompletedTask : PlayTrack(index, Tracks[index]);
    internal Task ForceTrack(string fileName) => PlayTrack(-1, "res://ShoujoKagekiAijoKaren/audio/" + fileName, true);

    private async Task PlayTrack(int index, string path, bool forced = false)
    {
        if (_disposed || FormPriority) return;
        Audio.CombatBgmReplacementManager.YieldToMusicPlayer();
        int request = ++_request;
        _forcedPath = forced ? path : null;
        _pausedSeek = null;
        _player.Stop();
        _player.StreamPaused = false;
        _player.Stream = null;
        Index = index;
        Loading = true;
        Status = "loading";
        UpdateVolumes();
        Changed?.Invoke();
        try
        {
            // Only the selected file is read; decoding resumes on the Godot context.
            AudioStream? stream;
            if (path.StartsWith("res://", StringComparison.Ordinal))
                stream = KarenResourceLoader.LoadAudioStream(path, Path.GetFileName(path), nameof(KarenMusicPlayback))?.Duplicate() as AudioStream;
            else
            {
                byte[] bytes = await System.IO.File.ReadAllBytesAsync(path);
                if (_disposed || request != _request) return;
                stream = Path.GetExtension(path).ToLowerInvariant() switch
                {
                    ".mp3" => new AudioStreamMP3 { Data = bytes },
                    ".ogg" => AudioStreamOggVorbis.LoadFromBuffer(bytes),
                    ".wav" => AudioStreamWav.LoadFromBuffer(bytes),
                    _ => null
                };
            }
            if (stream is AudioStreamOggVorbis ogg) ogg.Loop = false;
            if (stream is AudioStreamMP3 mp3) mp3.Loop = false;
            if (stream == null || stream.GetLength() <= 0) throw new InvalidDataException("Unsupported audio data");
            _player.Stream = stream;
            _player.VolumeLinear = Math.Clamp(KarenModConfig.MusicPlayerVolume, 0, 1);
            _player.Play();
            _player.StreamPaused = FormPriority;
            Status = FormPriority ? "awakening" : "playing";
        }
        catch (Exception error)
        {
            if (_disposed || request != _request) return;
            Status = "error";
            MainFile.Logger.Info($"[MusicPlayer] Track could not be played ({error.GetType().Name}).");
        }
        if (_disposed || request != _request) return;
        Loading = false;
        UpdateVolumes();
        Changed?.Invoke();
        if (Status == "playing") TrackStarted?.Invoke(Title);
    }

    internal void Toggle()
    {
        if (FormPriority) return;
        if (Loading) { Stop(); return; }
        if (_player.Stream == null) { _ = _forcedPath != null ? PlayTrack(-1, _forcedPath, true) : Play(Index); return; }
        _player.StreamPaused = !_player.StreamPaused;
        if (!_player.StreamPaused && _pausedSeek is { } seek)
        {
            _player.Seek((float)seek);
            _pausedSeek = null;
        }
        if (!_player.Playing && !_player.StreamPaused) _player.Play();
        Status = _player.StreamPaused ? "paused" : "playing";
        UpdateVolumes();
        Changed?.Invoke();
    }

    internal void Step(int direction)
    {
        if (FormPriority || Tracks.Count == 0) return;
        int index = Index < 0 ? (direction > 0 ? 0 : Tracks.Count - 1) : (Index + direction + Tracks.Count) % Tracks.Count;
        if (KarenModConfig.MusicPlayMode == KarenMusicPlayMode.Random && Tracks.Count > 1)
        {
            index = Random.Shared.Next(Index < 0 ? Tracks.Count : Tracks.Count - 1);
            if (Index >= 0 && index >= Index) index++;
        }
        _ = Play(index);
    }

    internal void Seek(double seconds)
    {
        if (FormPriority || _player.Stream == null) return;
        double target = Math.Clamp(seconds, 0, Math.Max(0, Duration - .02));
        // The audio thread reports the old position while paused; retain the user's target until resume.
        _pausedSeek = _player.StreamPaused ? target : null;
        _player.Seek((float)target);
    }

    internal void Stop()
    {
        _request++;
        _pausedSeek = null;
        _forcedPath = null;
        if (Index < 0 && Tracks.Count > 0) Index = 0;
        _resumeAfterForm = false;
        Loading = false;
        _player.Stop();
        _player.StreamPaused = false;
        _player.Stream = null;
        Status = Tracks.Count > 0 ? "ready" : "unset";
        UpdateVolumes();
        Changed?.Invoke();
    }

    private void OnFinished()
    {
        if (KarenModConfig.MusicPlayMode == KarenMusicPlayMode.RepeatOne) _ = _forcedPath != null ? PlayTrack(-1, _forcedPath, true) : Play(Index);
        else if (Tracks.Count > 0) Step(1);
        else Stop();
    }

    internal void SetFormPriority(bool active)
    {
        if (_disposed || FormPriority == active) return;
        if (active)
        {
            _resumeAfterForm = IsPlaying || Loading;
            FormPriority = true;
            if (_player.Stream != null) _player.StreamPaused = true;
            Status = "awakening";
        }
        else
        {
            FormPriority = false;
            if (_resumeAfterForm && KarenModConfig.EnableMusicPlayer && _player.Stream != null)
            {
                _player.StreamPaused = false;
                Status = "playing";
            }
            else Status = IsPaused ? "paused" : Tracks.Count > 0 ? "ready" : "unset";
            _resumeAfterForm = false;
        }
        UpdateVolumes();
        Changed?.Invoke();
        if (active || IsPlaying) TrackStarted?.Invoke(Title);
    }

    internal static void RefreshVolumes() => Current?.UpdateVolumes();

    internal void UpdateVolumes()
    {
        if (_disposed) return;
        EnsureBus();
        float bgm = SaveManager.Instance?.SettingsSave?.VolumeBgm ?? 1f;
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex(Bus), Mathf.LinearToDb(Math.Max(.00001f, bgm)));
        _player.VolumeLinear = Math.Clamp(KarenModConfig.MusicPlayerVolume, 0, 1);
        // Change only the native music bus, retaining ambience, SFX and live settings.
        var proxy = NAudioManager.Instance?.GetNodeOrNull<Node>("Proxy");
        if (GodotObject.IsInstanceValid(proxy)) proxy!.Call("set_bgm_volume", IsPlaying ? 0f : bgm * bgm);
        Audio.KarenAudioManager.UpdateMusicVolume(bgm);
    }

    private static void EnsureBus()
    {
        if (AudioServer.GetBusIndex(Bus) >= 0) return;
        AudioServer.AddBus();
        int index = AudioServer.BusCount - 1;
        AudioServer.SetBusName(index, Bus);
        AudioServer.SetBusSend(index, "Master");
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _disposed = true;
        _player.Finished -= OnFinished;
        if (GodotObject.IsInstanceValid(_player)) _player.QueueFree();
        if (Current == this) Current = null;
        Changed = null;
        TrackStarted = null;
    }

    internal static string Text(string key) => new MegaCrit.Sts2.Core.Localization.LocString("settings_ui", "KAREN_MUSIC." + key).GetFormattedText();
}
