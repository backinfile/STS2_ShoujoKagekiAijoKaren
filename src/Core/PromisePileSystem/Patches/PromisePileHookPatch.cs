using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using ShoujoKageki.Core.V1;
using System;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Patches;

public static class PromisePileHookPatch
{
    private static IDisposable? _subscription;
    internal static void Initialize() => _subscription ??= CoreApi.Get<ICardMovementPipeline>().Register(PromisePileManager.HandleMovement);
    [HarmonyPatch(typeof(CardPile), nameof(CardPile.AddInternal))]
    private static class Refilled
    {
        private static void Postfix(CardPile __instance) => PromisePileManager.PilePopulated(__instance);
    }
}
