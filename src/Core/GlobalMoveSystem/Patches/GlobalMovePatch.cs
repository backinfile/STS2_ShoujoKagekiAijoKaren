using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using System.Collections.Generic;

namespace ShoujoKagekiAijoKaren.src.Core.GlobalMoveSystem.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardChangedPiles))]
internal static class GlobalMovePatch
{
    /// <summary>
    /// 约定牌堆的"离开"操作（Draw/DiscardAll）会先手动 Invoke 正确事件，再调 CardPileCmd.Add。
    /// CardPileCmd.Add 触发的 Hook 携带错误的 oldPile=None，加入此集合以跳过一次。
    /// 经验证 CardPileCmd.Add 对单张卡只触发一次 AfterCardChangedPiles，remove-on-first-hit 安全。
    /// </summary>
    internal static readonly HashSet<CardModel> SuppressOnce = new();

    // Prefix 在 async 状态机启动前同步执行。
    // 此时卡牌已完成物理移动（CardPileCmd.Add 先移牌再调用 Hook），
    // 故 card.Pile?.Type 即为新牌堆，oldPile 参数为旧牌堆。
    [HarmonyPrefix]
    private static void Prefix(
        IRunState runState, CombatState? combatState,
        CardModel card, PileType oldPile, AbstractModel? source)
    {
        if (SuppressOnce.Remove(card)) return;

        PileType newPile;
        // 卡牌进入约定牌堆后 card.Pile 为 null，但 IsInPromisePile 为 true
        if (card.Pile == null && PromisePileManager.IsInPromisePile(card))
            newPile = KarenCustomEnum.PromisePile;
        else
            newPile = card.Pile?.Type ?? PileType.None;

        GlobalMoveSystem.Invoke(card, oldPile, newPile, source);
    }
}
