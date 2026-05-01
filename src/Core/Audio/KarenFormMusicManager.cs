using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Runs;

namespace ShoujoKagekiAijoKaren.src.Core.Audio;

/// <summary>
/// 管理 Form 的持续背景音乐，行为上对齐原项目的“切到专属循环 BGM，结束时停止”。
/// </summary>
public static class KarenFormMusicManager
{
    public const string FileName = "Karen_form.MP3";
    private const float FadeOutDuration = 0.35f;

    private static AudioStreamPlayer? _currentPlayer;
    private static int _restoreRequestId;
    private static bool _needsGameMusicRestore;

    public static void PlayLoop(Player? player, float volume = 1f)
    {
        if (!LocalContext.IsMe(player))
            return;

        _restoreRequestId++;
        StopImmediate();
        StopGameMusicEventOnly();
        _currentPlayer = KarenAudioManager.PlayMusicLoop(FileName, volume);
    }

    public static void Stop(Player? ownerPlayer = null)
    {
        if (ownerPlayer != null && !LocalContext.IsMe(ownerPlayer))
            return;

        if (!GodotObject.IsInstanceValid(_currentPlayer))
        {
            _currentPlayer = null;
            return;
        }

        var audioPlayer = _currentPlayer;
        _currentPlayer = null;

        var tween = audioPlayer!.CreateTween();
        tween.TweenProperty(audioPlayer, "volume_linear", 0f, FadeOutDuration);
        tween.Finished += () =>
        {
            if (GodotObject.IsInstanceValid(audioPlayer))
            {
                audioPlayer.Stop();
                audioPlayer.QueueFree();
            }

            RequestRestoreGameMusic();
        };
    }

    public static void StopForCutscene(Player? ownerPlayer = null)
    {
        if (ownerPlayer != null && !LocalContext.IsMe(ownerPlayer))
            return;

        _restoreRequestId++;
        StopImmediate();
        StopGameMusicEventOnly();
        _needsGameMusicRestore = true;
    }

    private static void StopImmediate()
    {
        if (!GodotObject.IsInstanceValid(_currentPlayer))
        {
            _currentPlayer = null;
            return;
        }

        _currentPlayer.Stop();
        _currentPlayer.QueueFree();
        _currentPlayer = null;
    }

    private static void StopGameMusicEventOnly()
    {
        var controller = NRunMusicController.Instance;
        if (!GodotObject.IsInstanceValid(controller))
            return;

        var proxy = Traverse.Create(controller).Field<Node>("_proxy").Value;
        if (!GodotObject.IsInstanceValid(proxy))
            return;

        proxy.Call("stop_music");
        _needsGameMusicRestore = true;
    }

    public static void RestoreGameMusicIfNeeded()
    {
        if (!_needsGameMusicRestore || GodotObject.IsInstanceValid(_currentPlayer))
            return;

        RequestRestoreGameMusic();
    }

    private static void RequestRestoreGameMusic()
    {
        var requestId = ++_restoreRequestId;
        RestoreGameMusicDeferred(requestId);
    }

    private static async void RestoreGameMusicDeferred(int requestId)
    {
        var tree = Engine.GetMainLoop() as SceneTree;
        if (tree != null)
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        if (requestId != _restoreRequestId || GodotObject.IsInstanceValid(_currentPlayer))
            return;

        RestoreGameMusic();
    }

    private static void RestoreGameMusic()
    {
        var controller = NRunMusicController.Instance;
        if (!GodotObject.IsInstanceValid(controller) || !RunManager.Instance.IsInProgress)
            return;

        controller.UpdateMusic();
        controller.UpdateTrack();
        controller.UpdateAmbience();
        _needsGameMusicRestore = false;
    }
}
