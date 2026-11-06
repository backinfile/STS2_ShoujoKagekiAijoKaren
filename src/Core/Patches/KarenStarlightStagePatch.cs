using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

[HarmonyPatch]
public static class KarenStarlightStagePatch
{
    [HarmonyPatch(typeof(NHandCardHolder), "DoCardHoverEffects")]
    [HarmonyPostfix]
    private static void Hover(NHandCardHolder __instance,bool isHovered)
    {if(__instance.CardNode?.Model is {} card)KarenStarlightStage.Hover(card,isHovered);}

    [HarmonyPatch(typeof(NHandCardHolder), nameof(NHandCardHolder.Clear))]
    [HarmonyPrefix]
    private static void Clear(NHandCardHolder __instance)
    {if(__instance.CardNode?.Model is {} card)KarenStarlightStage.Hover(card,false);}

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.TryManualPlay))]
    [HarmonyPostfix]
    private static void Queued(CardModel __instance,bool __result)
    {if(__result)KarenStarlightStage.Queue(__instance);}

    [HarmonyPatch(typeof(PlayCardAction), "CancelAction")]
    [HarmonyPrefix]
    private static void Cancel(PlayCardAction __instance)
    {if(__instance.NetCombatCard.ToCardModelOrNull() is {} card)KarenStarlightStage.CancelQueue(card);}

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
    [HarmonyPrefix]
    private static void Begin(CardModel __instance,out bool __state) => __state=KarenStarlightStage.Begin(__instance);

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
    [HarmonyPostfix]
    private static void Finish(CardModel __instance,bool __state,ref Task __result)
    {if(__state)__result=HoldThroughSettlement(__result,__instance);}

    private static async Task HoldThroughSettlement(Task play,CardModel card)
    {try{await play;}finally{KarenStarlightStage.End(card);}}

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
    [HarmonyFinalizer]
    private static void FailedBeforeTask(CardModel __instance,bool __state,System.Exception? __exception)
    {if(__state&&__exception!=null)KarenStarlightStage.End(__instance);}
}
