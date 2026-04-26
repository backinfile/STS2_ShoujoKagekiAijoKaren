using Godot;
using MegaCrit.Sts2.Core.Saves;
using System;
using System.Collections.Generic;

namespace ShoujoKagekiAijoKaren.src.Core.Audio;

/// <summary>
/// Karen Mod 专属音频管理器。
/// 音效与音乐分开走不同入口，并分别跟随游戏 SFX / BGM 音量设置。
/// </summary>
public static class KarenAudioManager
{
    private const string SfxBus = "SFX";
    private const string MusicBus = "Master";

    private static readonly Dictionary<string, AudioStream> _cache = new();

    public static AudioStreamPlayer Play(string fileName, float volume = 1f, float pitchScale = 1f)
    {
        return PlaySfx(fileName, volume, pitchScale);
    }

    public static AudioStreamPlayer PlayLoop(string fileName, float volume = 1f)
    {
        return PlaySfxLoop(fileName, volume);
    }

    public static AudioStreamPlayer PlaySfx(string fileName, float volume = 1f, float pitchScale = 1f)
    {
        var stream = PreparePlaybackStream(fileName, loop: false);
        if (stream == null) return null;

        var player = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeLinear = volume * GetEffectiveVolume(isMusic: false),
            PitchScale = pitchScale,
            Bus = SfxBus
        };

        var tree = Engine.GetMainLoop() as SceneTree;
        tree?.Root.AddChild(player);
        player.Play();
        player.Finished += () => player.QueueFree();
        return player;
    }

    public static AudioStreamPlayer PlaySfxLoop(string fileName, float volume = 1f)
    {
        var stream = PreparePlaybackStream(fileName, loop: true);
        if (stream == null) return null;

        var player = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeLinear = volume * GetEffectiveVolume(isMusic: false),
            Bus = SfxBus
        };

        var tree = Engine.GetMainLoop() as SceneTree;
        tree?.Root.AddChild(player);
        player.Play();
        return player;
    }

    public static AudioStreamPlayer PlayMusicLoop(string fileName, float volume = 1f)
    {
        var stream = PreparePlaybackStream(fileName, loop: true);
        if (stream == null) return null;

        var player = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeLinear = volume * GetEffectiveVolume(isMusic: true),
            Bus = MusicBus
        };

        var tree = Engine.GetMainLoop() as SceneTree;
        tree?.Root.AddChild(player);
        player.Play();
        return player;
    }

    private static AudioStream PreparePlaybackStream(string fileName, bool loop)
    {
        var stream = LoadStream(fileName);
        if (stream == null) return null;

        var playbackStream = (AudioStream)stream.Duplicate();
        SetLoop(playbackStream, loop);
        return playbackStream;
    }

    private static AudioStream LoadStream(string fileName)
    {
        if (_cache.TryGetValue(fileName, out var cached))
            return cached;

        var path = $"res://ShoujoKagekiAijoKaren/audio/{fileName}";
        if (!FileAccess.FileExists(path))
        {
            GD.PrintErr($"[KarenAudioManager] 音频文件不存在: {path}");
            return null;
        }

        var bytes = FileAccess.GetFileAsBytes(path);
        if (bytes.Length == 0)
        {
            GD.PrintErr($"[KarenAudioManager] 音频文件读取失败: {path}");
            return null;
        }

        AudioStream stream;
        if (fileName.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
        {
            stream = AudioStreamOggVorbis.LoadFromBuffer(bytes);
        }
        else if (fileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
        {
            stream = new AudioStreamMP3 { Data = bytes };
        }
        else if (fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
        {
            stream = new AudioStreamWav { Data = bytes };
        }
        else
        {
            GD.PrintErr($"[KarenAudioManager] 不支持的音频格式: {fileName}");
            return null;
        }

        if (stream == null)
        {
            GD.PrintErr($"[KarenAudioManager] 音频解码失败: {path}");
            return null;
        }

        _cache[fileName] = stream;
        return stream;
    }

    public static double GetDuration(string fileName)
    {
        var stream = LoadStream(fileName);
        return stream?.GetLength() ?? 0;
    }

    private static float GetEffectiveVolume(bool isMusic)
    {
        var settings = SaveManager.Instance?.SettingsSave;
        if (settings == null)
            return 1f;

        float master = settings.VolumeMaster;
        float category = isMusic ? settings.VolumeBgm : settings.VolumeSfx;
        return Math.Clamp(master * category, 0f, 1f);
    }

    private static void SetLoop(AudioStream stream, bool loop)
    {
        switch (stream)
        {
            case AudioStreamMP3 mp3:
                mp3.Loop = loop;
                break;
            case AudioStreamOggVorbis ogg:
                ogg.Loop = loop;
                break;
            case AudioStreamWav wav:
                wav.LoopMode = loop ? AudioStreamWav.LoopModeEnum.Forward : AudioStreamWav.LoopModeEnum.Disabled;
                break;
        }
    }

    public static void ClearCache() => _cache.Clear();
}
