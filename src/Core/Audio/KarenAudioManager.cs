using Godot;
using System.Collections.Generic;

namespace ShoujoKagekiAijoKaren.src.Core.Audio;

/// <summary>
/// Karen Mod 专属音效管理器。使用 Godot 原生 AudioStreamPlayer 播放 Mod 音频文件。
/// 音频文件放在插件目录的 audio/ 子文件夹下，如 ShoujoKagekiAijoKaren/audio/xxx.ogg
/// <para>
/// 音量自动跟随游戏设置：Bus = "SFX"，游戏本体在调整音量时已同步设置 Godot SFX 总线。
/// </para>
/// </summary>
public static class KarenAudioManager
{
    private static readonly Dictionary<string, AudioStream> _cache = new();

    /// <summary>
    /// 播放短音效。返回 AudioStreamPlayer 实例，可用于检查 IsPlaying。
    /// 播放完毕后自动释放（无需手动 QueueFree）。
    /// </summary>
    public static AudioStreamPlayer Play(string fileName, float volume = 1f, float pitchScale = 1f)
    {
        var stream = LoadStream(fileName);
        if (stream == null) return null;

        var player = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeLinear = volume,
            PitchScale = pitchScale,
            Bus = "SFX"   // 跟随游戏 SFX 音量设置
        };

        var tree = Engine.GetMainLoop() as SceneTree;
        tree?.Root.AddChild(player);
        player.Play();
        player.Finished += () => player.QueueFree();
        return player;
    }

    /// <summary>播放音效并返回 player（需要手动 Stop + QueueFree）</summary>
    public static AudioStreamPlayer PlayLoop(string fileName, float volume = 1f)
    {
        var stream = LoadStream(fileName);
        if (stream == null) return null;

        var player = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeLinear = volume,
            Bus = "SFX"   // 跟随游戏 SFX 音量设置
        };

        var tree = Engine.GetMainLoop() as SceneTree;
        tree?.Root.AddChild(player);
        player.Play();
        return player;
    }

    private static AudioStream LoadStream(string fileName)
    {
        if (_cache.TryGetValue(fileName, out var cached))
            return cached;

        var path = $"res://ShoujoKagekiAijoKaren/audio/{fileName}";

        // 使用 FileAccess 直接读取字节，绕过 Godot 导入系统
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
        if (fileName.EndsWith(".ogg"))
        {
            stream = AudioStreamOggVorbis.LoadFromBuffer(bytes);
        }
        else if (fileName.EndsWith(".mp3"))
        {
            stream = new AudioStreamMP3 { Data = bytes };
        }
        else if (fileName.EndsWith(".wav"))
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

    /// <summary>获取音频时长（秒），加载失败返回 0</summary>
    public static double GetDuration(string fileName)
    {
        var stream = LoadStream(fileName);
        return stream?.GetLength() ?? 0;
    }

    public static void ClearCache() => _cache.Clear();
}
