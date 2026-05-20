using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.PeerInput;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Patches;

/// <summary>
/// 悬停或拖拽带有 PromisePileRelated Tag 的手牌时，使 KarenPromisePilePower 产生脉冲效果
/// </summary>
[HarmonyPatch(typeof(HoveredModelTracker))]
public static class PromisePileHoverPatch
{
    private static KarenPromisePilePower? _pulsing;

    [HarmonyPostfix]
    [HarmonyPatch("OnLocalCardHovered")]
    private static void OnLocalCardHovered_Postfix(CardModel cardModel)
    {
        TryStartPulsing(cardModel);
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnLocalCardUnhovered")]
    private static void OnLocalCardUnhovered_Postfix()
    {
        StopPulsing();
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnLocalCardSelected")]
    private static void OnLocalCardSelected_Postfix(CardModel cardModel)
    {
        TryStartPulsing(cardModel);
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnLocalCardDeselected")]
    private static void OnLocalCardDeselected_Postfix()
    {
        StopPulsing();
    }

    private static void TryStartPulsing(CardModel cardModel)
    {
        if (!cardModel.Tags.Contains(KarenCustomEnum.PromisePileRelated))
            return;

        var power = cardModel.Owner?.Creature?.GetPower<KarenPromisePilePower>();
        if (power == _pulsing)
            return;

        _pulsing?.StopPulsing();
        _pulsing = power;
        _pulsing?.StartPulsing();
    }

    private static void StopPulsing()
    {
        _pulsing?.StopPulsing();
        _pulsing = null;
    }
}
