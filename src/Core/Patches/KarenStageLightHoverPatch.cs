using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Models.Cards;
using System.Reflection;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

[HarmonyPatch]
public static class KarenHoverPatch
{
    private static readonly PropertyInfo? HolderProperty = AccessTools.Property(typeof(NCardPlay), "Holder");

    [HarmonyPatch(typeof(NCardPlay), "OnCreatureHover")]
    [HarmonyPostfix]
    private static void OnCreatureHover_Postfix(NCardPlay __instance, NCreature creature)
    {
        if (GetCard(__instance) is KarenBaseCardModel card)
        {
            card.OnCreatureHover(creature);
        }
    }

    [HarmonyPatch(typeof(NCardPlay), "OnCreatureUnhover")]
    [HarmonyPostfix]
    private static void OnCreatureUnhover_Postfix(NCardPlay __instance, NCreature _)
    {
        if (GetCard(__instance) is KarenBaseCardModel card)
        {
            card.OnCreatureUnhover(_);
        }
    }

    [HarmonyPatch(typeof(NCardPlay), "Cleanup")]
    [HarmonyPrefix]
    private static void Cleanup_Prefix(NCardPlay __instance)
    {
        if (GetCard(__instance) is KarenBaseCardModel card)
        {
            foreach (var creature in NCombatRoom.Instance?.CreatureNodes ?? [])
                card.OnCreatureHoverCleanup(creature);
        }
    }

    private static CardModel? GetCard(NCardPlay cardPlay)
    {
        return (HolderProperty?.GetValue(cardPlay) as NHandCardHolder)?.CardNode?.Model;
    }
}
