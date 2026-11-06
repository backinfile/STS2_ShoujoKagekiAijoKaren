using MegaCrit.Sts2.Core.Models;
using ShoujoKageki.Core.V1;
using System;

namespace ShoujoKagekiAijoKaren.src.Core.ExtraReplaySystem;

/// <summary>Karen names for the shared next-play replay budget.</summary>
public static class ExtraReplayExtension
{
    public static int GetExtraReplayCountForNextPlay(this CardModel card) => CardReplays.GetNext(card);
    public static void SetExtraReplayCountForNextPlay(this CardModel card, int value) => CardReplays.SetNext(card, Math.Max(0, value));
    public static void AddExtraReplayCountForNextPlay(this CardModel card, int amount)
        => CardReplays.SetNext(card, Math.Max(0, CardReplays.GetNext(card) + amount));
}
