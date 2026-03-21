using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ShoujoKagekiAijoKaren.src.Core.GlobalMoveSystem.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardChangedPiles))]
internal static class GlobalMovePatch
{
    // Prefix 在 async 状态机启动前同步执行。
    // 此时卡牌已完成物理移动（CardPileCmd.Add 先移牌再调用 Hook），
    // 故 card.Pile?.Type 即为新牌堆，oldPile 参数为旧牌堆。
    [HarmonyPrefix]
    private static void Prefix(
        IRunState runState, CombatState? combatState,
        CardModel card, PileType oldPile, AbstractModel? source)
    {
        PileType newPile = card.Pile?.Type ?? PileType.None;
        GlobalMoveSystem.Invoke(card, oldPile, newPile, source);
    }
}
