using System;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKageki.Core.V1;

namespace ShoujoKagekiAijoKaren.src.Core.Audio;

/// <summary>Karen supplies tracks and lifetimes; Core owns playback and native music restoration.</summary>
public static class CombatBgmReplacementManager
{
    private static IMusicOverrides Music => CoreApi.Get<IMusicOverrides>();
    private static IDisposable? _track;
    private static IDisposable? _cutscene;
    internal static bool CutscenePriority => _cutscene != null;
    public static void PlayLoop(string fileName, Player? ownerPlayer = null, float volume = 1f)
    {
        if (ownerPlayer != null && !LocalContext.IsMe(ownerPlayer)) return;
        EndCutscene();
        _track?.Dispose();
        _track = Music.AcquireTrack(nameof(CombatBgmReplacementManager), new MusicOverrideRequest
        {
            ResourcePath = $"res://ShoujoKagekiAijoKaren/audio/{fileName}",
            Volume = volume, Lifetime = NCombatRoom.Instance
        });
    }
    internal static void YieldToMusicPlayer() => Music.Refresh();
    public static void Stop(Player? ownerPlayer = null)
    {
        if (ownerPlayer != null && !LocalContext.IsMe(ownerPlayer)) return;
        _track?.Dispose();
        _track = null;
        EndCutscene();
    }
    public static void StopForCutscene(Player? ownerPlayer = null)
    {
        if (ownerPlayer != null && !LocalContext.IsMe(ownerPlayer)) return;
        _cutscene ??= Music.AcquireSilence(nameof(CombatBgmReplacementManager), NCombatRoom.Instance);
    }
    public static void EndCutscene()
    {
        _cutscene?.Dispose();
        _cutscene = null;
    }
    public static void StopForCombatEnd() => Stop();
    public static void StopForRunExit()
    {
        Stop();
        CoreApi.Get<IMusicPlayback>().Stop();
    }
    public static void RestoreGameMusicIfNeeded() => Music.Refresh();
}
