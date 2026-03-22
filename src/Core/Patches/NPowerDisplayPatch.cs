using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Combat;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// Patch NPower.RefreshAmount 以支持 FakeAmountPower 的自定义数值显示
/// </summary>
[HarmonyPatch(typeof(NPower), "RefreshAmount")]
public static class NPowerDisplayPatch
{
    private static void Postfix(NPower __instance)
    {
        var model = __instance.Model;
        if (model is FakeAmountPower fakeAmountPower)
        {
            var amountLabel = AccessTools.Field(typeof(NPower), "_amountLabel").GetValue(__instance) as MegaLabel;
            if (amountLabel != null)
            {
                amountLabel.SetTextAutoSize(fakeAmountPower.FakeAmount.ToString());
            }
        }
    }
}
