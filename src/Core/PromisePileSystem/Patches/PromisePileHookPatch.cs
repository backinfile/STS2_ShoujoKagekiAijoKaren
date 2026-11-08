using BaseLib.Utils;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using ShoujoKageki.Core.V1;
using System;
using System.Threading.Tasks;
namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Patches;

public static class PromisePileHookPatch
{
    private static readonly SpireField<CardPile, bool> EmptyNotified = new(() => false);
    private static IDisposable? _subscription;
    internal static void Initialize() => _subscription ??= CoreApi.Get<ICardMovementPipeline>().Register(Handle);
    private static async Task Handle(CardMovement movement)
    {
        var card = movement.Card;
        if (card.Owner is not { PlayerCombatState: not null } player) return;
        var oldPile = movement.OldPile; var next = movement.NewPile;
        bool inVoid = PromisePileManager.IsVoidMode(player);
        bool inInfinite = PromisePileManager.IsInMode(player, PromisePileMode.InfiniteReinforcement);
        if (oldPile == KarenCustomEnum.PromisePile || oldPile == PileType.Draw && inVoid)
            await PromisePileHooks.TriggerOnCardRemoved(player, card);
        if (next == KarenCustomEnum.PromisePile || next == PileType.Draw && inVoid)
            await PromisePileHooks.TriggerOnCardAdded(player, card);
        if (oldPile == PileType.Draw && inVoid && inInfinite) await PromisePileManager.UpdatePowerAsync(player);
        if (player.PlayerCombatState == null || oldPile != KarenCustomEnum.PromisePile && oldPile != PileType.Draw) return;
        var pile = oldPile.GetPile(player);
        if (!pile.IsEmpty || EmptyNotified[pile]) return;
        inVoid = PromisePileManager.IsVoidMode(player);
        if (pile.Type == KarenCustomEnum.PromisePile && !inVoid || pile.Type == PileType.Draw && inVoid)
        {
            EmptyNotified[pile] = true;
            await PromisePileHooks.TriggerPromisePileEmpty(player);
        }
    }
    [HarmonyPatch(typeof(CardPile), nameof(CardPile.AddInternal))]
    private static class Refilled
    {
        private static void Postfix(CardPile __instance)
        {
            if (__instance.Type == PileType.Draw || __instance.Type == KarenCustomEnum.PromisePile) EmptyNotified[__instance] = false;
        }
    }
}
