using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

[HarmonyPatch(typeof(NCardFlyPowerVfx),nameof(NCardFlyPowerVfx._Ready))]
internal static class KarenTicketCastVfxPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCardFlyPowerVfx __instance)
    {
        if(__instance.CardNode?.Model is not KarenTicket card)return;
        var cue=NKarenStageCueVfx.Play(card.Owner,NKarenStageCueVfx.CueKind.TicketCast,card);
        cue?.Finish();
    }
}
