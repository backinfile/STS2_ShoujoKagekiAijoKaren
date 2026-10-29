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

        /// <summary>
        /// 离开约定牌堆
        /// 进入约定牌堆
        /// 空虚模式下，约定牌堆计数刷新
        /// </summary>
        [HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardChangedPiles))]
        public static class PromisePileMovePatch
        {

            [HarmonyPostfix]
            public static void Postfix(
                IRunState runState, CombatState? combatState,
                CardModel card, PileType oldPile, [HarmonyArgument(4)] AbstractModel? source,
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
                bool inVoidMode = PromisePileManager.IsInMode(player, PromisePileMode.Void);
                bool inInfMode = PromisePileManager.IsInMode(player, PromisePileMode.InfiniteReinforcement);

                // 离开约定牌堆
                if (oldPile == KarenCustomEnum.PromisePile || (oldPile == PileType.Draw && inVoidMode))
                {
                    await PromisePileHooks.TriggerOnCardRemoved(player, card);
                }

                // 进入约定牌堆
                var curType = card.Pile?.Type ?? PileType.None;
                if (curType == KarenCustomEnum.PromisePile || (curType == PileType.Draw && inVoidMode))
                {
                    await PromisePileHooks.TriggerOnCardAdded(player, card);
                }

                // 空虚模式下，约定牌堆计数刷新
                if (oldPile == PileType.Draw && inVoidMode && inInfMode)
                {
                    await PromisePileManager.UpdatePowerAsync(player);
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
                //MainFile.Logger.Info($"AfterCardChangedPiles card={card.Title} oldPile={oldPile} first");
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
                                cardPileMarked[cardPile] = true;
                                await PromisePileHooks.TriggerPromisePileEmpty(player);
                            }
                        }
                    }
                });
            }
        }

        /// <summary>
        /// 实际补入卡牌后，允许下一次清空触发；覆盖原生命令与直接补牌。
        /// </summary>
        [HarmonyPatch(typeof(CardPile), nameof(CardPile.AddInternal))]
        public static class CardPile_AddInternal_Hook_Patch
        {
            public static void Postfix(CardPile __instance)
            {
                if (__instance.Type == PileType.Draw || __instance.Type == KarenCustomEnum.PromisePile)
                    AfterCardChangedPiles_Hook_Patch.cardPileMarked[__instance] = false;
            }
        }
    }
}
