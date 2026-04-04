using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
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
                Async.Postfix(ref __result, HandleHooks(card, oldPile));
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
    }
}
