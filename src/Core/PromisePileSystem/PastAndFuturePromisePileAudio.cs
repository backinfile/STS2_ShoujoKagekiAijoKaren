using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using System.Collections.Generic;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;

public static class PastAndFuturePromisePileAudio
{
    private sealed class AudioState
    {
        public int Index;
        public AudioStreamPlayer? CurrentPlayer;
    }

    private static readonly Dictionary<Player, AudioState> States = new();

    public static void Reset(Player player)
    {
        GetState(player).Index = 0;
    }

    public static void Clear(Player player)
    {
        States.Remove(player);
    }

    public static void TryPlayNext(Player player)
    {
        var state = GetState(player);
        if (state.Index >= 4) return;
        if (state.CurrentPlayer != null && state.CurrentPlayer.Playing) return;

        var audioPlayer = KarenAudioManager.Play(GetFileName(state.Index), volume: 1f);
        if (audioPlayer == null) return;

        state.CurrentPlayer = audioPlayer;
        audioPlayer.Finished += () =>
        {
            if (state.CurrentPlayer == audioPlayer)
                state.CurrentPlayer = null;
        };

        state.Index++;
    }

    private static AudioState GetState(Player player)
    {
        if (!States.TryGetValue(player, out var state))
        {
            state = new AudioState();
            States[player] = state;
        }

        return state;
    }

    private static string GetFileName(int index) => index switch
    {
        0 => KarenSfx.PastAndFutureDraw1,
        1 => KarenSfx.PastAndFutureDraw2,
        2 => KarenSfx.PastAndFutureDraw3,
        _ => KarenSfx.PastAndFutureDraw4,
    };
}
