using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Saves;
using ShoujoKagekiAijoKaren.src.Core.Utils;
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
    private const string MusicBus = "KarenMusic";

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
        return PlaySfxOnGameBus(fileName, volume, pitchScale);
    }

    /// <summary>Play a calibrated sample through the game's SFX and Master buses.
    /// Those buses already apply the user's volume sliders; do not apply them again here.</summary>
    public static AudioStreamPlayer PlaySfxOnGameBus(string fileName, float volume = 1f, float pitchScale = 1f)
    {
        var stream = PreparePlaybackStream(fileName, loop: false);
        if (stream == null) return null;

        var player = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeLinear = volume,
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
            // The SFX and Master buses apply live settings, including mute/unmute.
            VolumeLinear = volume,
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

        EnsureMusicBus();
        var player = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeLinear = volume,
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
        var stream = KarenResourceLoader.LoadAudioStream(path, fileName, nameof(KarenAudioManager));
        if (stream == null) return null;

        _cache[fileName] = stream;
        return stream;
    }

    public static double GetDuration(string fileName)
    {
        var stream = LoadStream(fileName);
        return stream?.GetLength() ?? 0;
    }

    private static void EnsureMusicBus()
    {
        if (AudioServer.GetBusIndex(MusicBus) < 0)
        {
            AudioServer.AddBus();
            int index = AudioServer.BusCount - 1;
            AudioServer.SetBusName(index, MusicBus);
            AudioServer.SetBusSend(index, "Master");
        }
        UpdateMusicVolume(SaveManager.Instance?.SettingsSave?.VolumeBgm ?? 1f);
    }

    // Keep category gain on its own bus so changing settings never overwrites a player fade.
    internal static void UpdateMusicVolume(float volume)
    {
        int index = AudioServer.GetBusIndex(MusicBus);
        if (index >= 0)
            AudioServer.SetBusVolumeDb(index, Mathf.LinearToDb(Math.Clamp(volume, 0f, 1f)));
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

[HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.SetBgmVol))]
internal static class KarenMusicVolumePatch
{
    private static void Postfix(float volume) => KarenAudioManager.UpdateMusicVolume(volume);
}
