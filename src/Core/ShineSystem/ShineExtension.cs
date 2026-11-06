using MegaCrit.Sts2.Core.Models;
using ShoujoKageki.Core.V1;
using System;

namespace ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

/// <summary>Compatibility names used by Karen-specific card effects.</summary>
public static class ShineExtension
{
    public const string ShineVarName = "KarenShine";
    public const string ShineMaxVarName = "KarenShineMax";

    public static bool IsShineCard(this CardModel card) => Shine.IsShineCard(card);
    public static int GetShineValue(this CardModel card) => Shine.GetState(card)?.Remaining ?? -1;
    public static int GetShineValueRounded(this CardModel card) => Math.Max(0, card.GetShineValue());
    public static int GetShineMaxValue(this CardModel card) => Shine.GetState(card)?.Maximum ?? -1;
    public static bool HasShine(this CardModel card) => card.GetShineValue() > 0;
    public static bool ShouldEnterShinePileAfterPlay(this CardModel card) => Shine.GetState(card)?.PendingDepletion == true;
    public static void AddShineMax(this CardModel card, int value) => Shine.ChangeMaximum(card, value);
    public static void RestoreShineToMax(this CardModel card) => Shine.RestoreToMaximum(card);
    public static void ConsumeAllShine(this CardModel card) => card.SetShineCurrent(0);

    public static void SetShineCurrent(this CardModel card, int value)
    {
        var state = Shine.GetState(card);
        if (state == null && value < 0) return;
        Shine.SetState(card, Math.Max(0, value), state?.Maximum ?? Math.Max(0, value), state?.Temporary ?? true);
    }

    public static void SetShineMax(this CardModel card, int value)
    {
        if (value < 0) return;
        var state = Shine.GetState(card);
        Shine.SetState(card, state?.Remaining ?? value, value, state?.Temporary ?? false);
    }

    public static void SetEnterShinePileAfterPlay(this CardModel card, bool value = true)
    {
        if (value) Shine.DepleteAfterCurrentResolution(card);
    }

    public static int DecreaseShine(this CardModel card)
    {
        card.SetShineCurrent(Math.Max(0, card.GetShineValue() - 1));
        return card.GetShineValueRounded();
    }
}
