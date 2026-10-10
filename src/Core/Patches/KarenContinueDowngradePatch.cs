using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Screens;
using ShoujoKagekiAijoKaren.src.Models.Cards;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

// Dampen 默认记录全部等级，并在结束时补回。此牌只被降一级，因此也只应补回一级。
[HarmonyPatch(typeof(DampenPower), nameof(DampenPower.AfterApplied))]
internal static class KarenContinueDampenPatch
{
    private static int GetRemovedUpgradeCount(CardModel card) =>
        card is KarenContinue02 ? Math.Min(1, card.CurrentUpgradeLevel) : card.CurrentUpgradeLevel;

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var getter = AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.CurrentUpgradeLevel));
        int replaced = 0;
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(getter))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(KarenContinueDampenPatch), nameof(GetRemovedUpgradeCount));
                replaced++;
            }
            yield return instruction;
        }
        if (replaced != 1) throw new InvalidOperationException("Dampen upgrade bookkeeping changed; expected one upgrade-level read.");
    }
}

// 原生查看界面的取消升级勾选需要基础版副本；不要把它解释为只降一级。
[HarmonyPatch(typeof(NInspectCardScreen), "UpdateCardDisplay")]
internal static class KarenContinueBasePreviewPatch
{
    private static void DowngradePreview(CardModel card)
    {
        if (card is KarenContinue02 karenCard) karenCard.DowngradeToBaseForPreview();
        else CardCmd.Downgrade(card);
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var downgrade = AccessTools.Method(typeof(CardCmd), nameof(CardCmd.Downgrade));
        int replaced = 0;
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(downgrade))
            {
                instruction.operand = AccessTools.Method(typeof(KarenContinueBasePreviewPatch), nameof(DowngradePreview));
                replaced++;
            }
            yield return instruction;
        }
        if (replaced != 1) throw new InvalidOperationException("Inspect-card downgrade changed; expected one downgrade call.");
    }
}
