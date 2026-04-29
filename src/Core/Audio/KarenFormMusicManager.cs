using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;

namespace ShoujoKagekiAijoKaren.src.Core.Audio;

/// <summary>
/// 管理 Form 的持续背景音乐，行为上对齐原项目的“切到专属循环 BGM，结束时停止”。
/// </summary>
public static class KarenFormMusicManager
{
    public const string FileName = "Karen_form.MP3";
    private const float FadeOutDuration = 0.35f;

    private static AudioStreamPlayer? _currentPlayer;

    public static void PlayLoop(float volume = 1f)
    {
        StopImmediate(restoreGameBgm: false);
        StopGameBgm();
        _currentPlayer = KarenAudioManager.PlayMusicLoop(FileName, volume);
    }

    public static void Stop()
    {
        if (!GodotObject.IsInstanceValid(_currentPlayer))
        {
            _currentPlayer = null;
            return;
        }

        var player = _currentPlayer;
        _currentPlayer = null;

        var tween = player!.CreateTween();
        tween.TweenProperty(player, "volume_linear", 0f, FadeOutDuration);
        tween.Finished += () =>
        {
            if (GodotObject.IsInstanceValid(player))
            {
                player.Stop();
                player.QueueFree();
            }

            RestoreGameBgm();
        };
    }

    public static void StopForCutscene()
    {
        StopImmediate(restoreGameBgm: false);
        StopGameBgm();
    }

    private static void StopImmediate(bool restoreGameBgm)
    {
        if (!GodotObject.IsInstanceValid(_currentPlayer))
        {
            _currentPlayer = null;
            return;
        }

        _currentPlayer.Stop();
        _currentPlayer.QueueFree();
        _currentPlayer = null;

        if (restoreGameBgm)
            RestoreGameBgm();
    }

    private static void StopGameBgm()
    {
        NRunMusicController.Instance?.StopMusic();
    }

    private static void RestoreGameBgm()
    {
        var controller = NRunMusicController.Instance;
        if (controller == null)
            return;

        controller.UpdateMusic();

        var encounter = CombatManager.Instance.DebugOnlyGetState()?.Encounter;
        if (encounter != null && encounter.HasBgm)
        {
            controller.PlayCustomMusic(encounter.CustomBgm);
            return;
        }

        controller.UpdateTrack();
    }
}
