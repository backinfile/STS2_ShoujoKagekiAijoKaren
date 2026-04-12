using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Patches
{
    public class PromisePileHookPatch
    {
        [HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardChangedPiles))]
        public static class PromisePileMovePatch
        {

            [HarmonyPostfix]
            public static void Postfix(
                IRunState runState, CombatState? combatState,
                CardModel card, PileType oldPile, AbstractModel? source,
                ref Task __result)
            {
                Async.Postfix(ref __result, () => HandleHooks(card, oldPile));
            }

            public static async Task HandleHooks(CardModel card, PileType oldPile)
            {
                var player = card.Owner;
                if (player == null)
                {
                    MainFile.Logger.Error("PromisePileHookPatch: Card owner is null, cannot trigger hooks.");
                    return;
                }

                // 离开约定牌堆
                if (oldPile == KarenCustomEnum.PromisePile || (oldPile == PileType.Draw && PromisePileManager.IsInMode(player, PromisePileMode.Void)))
                {
                    await PromisePileHooks.TriggerOnCardRemoved(player, card);
                }


                // 进入约定牌堆
                var curType = card.Pile?.Type ?? PileType.None;
                if (curType == KarenCustomEnum.PromisePile || (curType == PileType.Draw && PromisePileManager.IsInMode(player, PromisePileMode.Void)))
                {
                    await PromisePileHooks.TriggerOnCardAdded(player, card);
                }
            }
        }

        /// <summary>
        /// 触发牌库变空扳机
        /// </summary>
        [HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardChangedPiles))]
        public static class AfterCardChangedPiles_Hook_Patch
        {
            public static readonly SpireField<CardPile, bool> cardPileMarked = new(() => false);
            public static void Postfix(CardModel card, PileType oldPile, ref Task __result)
            {
                MainFile.Logger.Info($"AfterCardChangedPiles card={card.Title} oldPile={oldPile} first");
                if (oldPile != KarenCustomEnum.PromisePile && oldPile != PileType.Draw) return;

                Async.Postfix(ref __result, async () =>
                {
                    if (card.Owner is Player player)
                    {
                        if (player.PlayerCombatState == null) return;

                        var cardPile = oldPile.GetPile(player);
                        if (cardPileMarked[cardPile])
                        {
                            MainFile.Logger.Info($"AfterCardChangedPiles card={card.Title} oldPile={oldPile} empty={cardPile.IsEmpty} skip");
                            return;
                        }
                        MainFile.Logger.Info($"AfterCardChangedPiles card={card.Title} oldPile={oldPile} empty={cardPile.IsEmpty}");
                        if (cardPile.IsEmpty) // 牌库变空
                        {
                            var inVoid = PromisePileManager.IsVoidMode(player);
                            if ((cardPile.Type == KarenCustomEnum.PromisePile && !inVoid)
                                || (cardPile.Type == PileType.Draw && inVoid))
                            {
                                await PromisePileHooks.TriggerPromisePileEmpty(player);
                                cardPileMarked[cardPile] = true;
                            }
                        }
                    }
                });
            }
        }

        /// <summary>
        /// 重置标记
        /// </summary>
        [HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
        public static class CardModel_OnPlayWrapper_Hook_Patch
        {

            public static void Prefix(CardModel __instance)
            {
                if (__instance.Owner is Player player)
                {
                    AfterCardChangedPiles_Hook_Patch.cardPileMarked[PileType.Draw.GetPile(player)] = false;
                    AfterCardChangedPiles_Hook_Patch.cardPileMarked[KarenCustomEnum.PromisePile.GetPile(player)] = false;
                }
            }
        }
    }
}
