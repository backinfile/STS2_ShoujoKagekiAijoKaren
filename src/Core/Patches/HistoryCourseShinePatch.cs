using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Relics;
using ShoujoKagekiAijoKaren.src.Core.Shine.ShinePatches;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// Keep History Course from turning depleted Shine cards into Empty Shell.
/// </summary>
[HarmonyPatch(typeof(HistoryCourse), nameof(HistoryCourse.AfterAutoPrePlayPhaseEntered))]
public static class HistoryCourseShinePatch
{
    private static void Postfix(ref Task __result)
    {
        var originalTask = __result;
        __result = RunWithDepletedShineCloneAllowed(originalTask);
    }

    private static async Task RunWithDepletedShineCloneAllowed(Task originalTask)
    {
        using var _ = ShinePatch.AllowDepletedShineClone();
        await originalTask;
    }
}
