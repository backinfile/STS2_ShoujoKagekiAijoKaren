using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ShoujoKagekiAijoKaren.src.Core.Audio;

/// <summary>
/// Replaces native run BGM with a mod-owned loop during combat, then restores native BGM when combat ends.
/// </summary>
public static class CombatBgmReplacementManager
{
    private const float FadeOutDuration = 0.35f;

    private static AudioStreamPlayer? _currentPlayer;
    private static AudioStreamPlayer? _fadingPlayer;
    private static Tween? _fadeTween;
    private static int _restoreRequestId;
    private static bool _needsGameMusicRestore, _cutscenePriority;
    private static NCombatRoom? _owningRoom;
    internal static bool CutscenePriority => _cutscenePriority;

    public static void PlayLoop(string fileName, Player? ownerPlayer = null, float volume = 1f)
    {
        if (ownerPlayer != null && !LocalContext.IsMe(ownerPlayer))
            return;

        _restoreRequestId++;
        _cutscenePriority = false;
        StopImmediate();
        if (KarenModConfig.EnableMusicPlayer && MusicPlayer.KarenMusicPlayback.Current is { } music)
        {
            RestoreGameMusicIfNeeded();
            music.SetFormPriority(false);
            _ = music.ForceTrack(fileName);
            return;
        }
        StopGameMusicEventOnly();
        _currentPlayer = KarenAudioManager.PlayMusicLoop(fileName, volume);
        // A debug fight/room replacement can skip AfterCombatEnd and Power.AfterRemoved.
        // Root-owned audio must still stop when the combat that started it leaves.
        if (GodotObject.IsInstanceValid(_currentPlayer)
            && NCombatRoom.Instance is { } room)
        {
            _owningRoom = room;
            room.TreeExiting += OnCombatRoomExiting;
        }
    }

    internal static void YieldToMusicPlayer()
    {
        if (!GodotObject.IsInstanceValid(_currentPlayer) && !GodotObject.IsInstanceValid(_fadingPlayer)) return;
        _restoreRequestId++;
        StopImmediate();
        RestoreGameMusicIfNeeded();
    }

    public static void Stop(Player? ownerPlayer = null)
    {
        if (ownerPlayer != null && !LocalContext.IsMe(ownerPlayer))
            return;

        StopAndRestoreGameMusic();
    }

    public static void StopForCutscene(Player? ownerPlayer = null)
    {
        if (ownerPlayer != null && !LocalContext.IsMe(ownerPlayer))
            return;

        _restoreRequestId++;
        _cutscenePriority = true;
        StopImmediate();
        MusicPlayer.KarenMusicPlayback.Current?.SetFormPriority(true);
        StopGameMusicEventOnly();
        _needsGameMusicRestore = true;
    }

    public static void EndCutscene()
    {
        if (!_cutscenePriority) return;
        _cutscenePriority = false;
        var state = RunManager.Instance.DebugOnlyGetState();
        var player = state == null ? null : LocalContext.GetMe(state);
        if (KarenModConfig.EnableMusicPlayer && MusicPlayer.KarenMusicPlayback.Current is { HasStream: true })
            RestoreGameMusicIfNeeded();
        else if (GodotObject.IsInstanceValid(NCombatRoom.Instance)
            && player?.Creature.GetPower<Models.Powers.KarenFormPower>() != null)
            PlayLoop(KarenFormMusicManager.FileName, player, 0.5f);
        else
            RestoreGameMusicIfNeeded();
    }

    public static void StopForCombatEnd()
    {
        StopAndRestoreGameMusic();
    }

    public static void StopForRunExit()
    {
        _restoreRequestId++;
        StopImmediate();
        _needsGameMusicRestore = false;
        _cutscenePriority = false;
        MusicPlayer.KarenMusicPlayback.Current?.Stop();
        MusicPlayer.KarenMusicPlayback.Current?.SetFormPriority(false);
    }

    public static void RestoreGameMusicIfNeeded()
    {
        if (!_needsGameMusicRestore || GodotObject.IsInstanceValid(_currentPlayer)
            || GodotObject.IsInstanceValid(_fadingPlayer))
            return;

        _cutscenePriority = false;
        MusicPlayer.KarenMusicPlayback.Current?.SetFormPriority(false);
        RequestRestoreGameMusic();
    }

    private static void StopAndRestoreGameMusic()
    {
        _cutscenePriority = false;
        // Repeated combat-end hooks must not restore the native track halfway through a fade.
        if (GodotObject.IsInstanceValid(_fadingPlayer)) return;
        if (!GodotObject.IsInstanceValid(_currentPlayer))
        {
            _currentPlayer = null;
            MusicPlayer.KarenMusicPlayback.Current?.SetFormPriority(false);
            RestoreGameMusicIfNeeded();
            return;
        }

        var audioPlayer = _currentPlayer;
        _currentPlayer = null;
        _fadingPlayer = audioPlayer;
        var requestId = ++_restoreRequestId;

        var tween = audioPlayer!.CreateTween();
        _fadeTween = tween;
        tween.TweenProperty(audioPlayer, "volume_linear", 0f, FadeOutDuration);
        tween.Finished += () =>
        {
            // Cutscenes, replacement tracks and run exit revoke this fade's ownership.
            if (requestId != _restoreRequestId || _fadingPlayer != audioPlayer) return;
            _fadingPlayer = null;
            _fadeTween = null;
            ReleaseCombatRoom();
            if (GodotObject.IsInstanceValid(audioPlayer))
            {
                audioPlayer.Stop();
                audioPlayer.QueueFree();
            }

            MusicPlayer.KarenMusicPlayback.Current?.SetFormPriority(false);
            RequestRestoreGameMusic();
        };
    }

    private static void StopImmediate()
    {
        ReleaseCombatRoom();
        _fadeTween?.Kill();
        _fadeTween = null;
        if (GodotObject.IsInstanceValid(_fadingPlayer))
        {
            _fadingPlayer!.Stop();
            _fadingPlayer.QueueFree();
        }
        _fadingPlayer = null;
        if (!GodotObject.IsInstanceValid(_currentPlayer))
        {
            _currentPlayer = null;
            return;
        }

        _currentPlayer.Stop();
        _currentPlayer.QueueFree();
        _currentPlayer = null;
    }

    private static void OnCombatRoomExiting()
    {
        // Revoke pending fades before releasing the outgoing room. The incoming
        // room's AfterRoomEntered hook restores its own native music selection.
        _restoreRequestId++;
        StopImmediate();
        _needsGameMusicRestore = true;
        _cutscenePriority = false;
        MusicPlayer.KarenMusicPlayback.Current?.SetFormPriority(false);
    }

    private static void ReleaseCombatRoom()
    {
        if (GodotObject.IsInstanceValid(_owningRoom))
            _owningRoom!.TreeExiting -= OnCombatRoomExiting;
        _owningRoom = null;
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

        if (requestId != _restoreRequestId || GodotObject.IsInstanceValid(_currentPlayer)
            || GodotObject.IsInstanceValid(_fadingPlayer))
            return;

        RestoreGameMusic();
    }

    private static void RestoreGameMusic()
    {
        var controller = NRunMusicController.Instance;
        if (!GodotObject.IsInstanceValid(controller) || !RunManager.Instance.IsInProgress)
            return;

        controller.UpdateMusic();
        // UpdateMusic can keep the same cached track and skip recreating the
        // event that stop_music released. Rebuild only that missing event;
        // the act bank and any already-playing native track stay untouched.
        var proxy = Traverse.Create(controller).Field<Node>("_proxy").Value;
        if (GodotObject.IsInstanceValid(proxy)
            && !GodotObject.IsInstanceValid(proxy.Get("_musicEv").AsGodotObject())
            && Traverse.Create(controller).Field<string>("_currentTrack").Value is { Length: > 0 } track)
            proxy.Call("update_music", track);
        controller.UpdateTrack();
        controller.UpdateAmbience();
        _needsGameMusicRestore = false;
    }
}
