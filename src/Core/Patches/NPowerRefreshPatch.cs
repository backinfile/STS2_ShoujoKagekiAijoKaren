using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// Patch NPower.RefreshAmount 来支持 FakeAmountPower 的 ShowFakeAmount 属性控制
/// </summary>
[HarmonyPatch(typeof(NPower))]
[HarmonyPatch("RefreshAmount")]
public class NPowerRefreshPatch
{
    static void Postfix(NPower __instance)
    {
        if (__instance.Model is FakeAmountPower fakeAmountPower && !fakeAmountPower.ShowFakeAmount)
        {
            // 获取 _amountLabel 字段并清空文本
            var amountLabel = AccessTools.Field(typeof(NPower), "_amountLabel").GetValue(__instance);
            if (amountLabel != null)
            {
                var setTextMethod = AccessTools.Method(amountLabel.GetType(), "SetTextAutoSize", new[] { typeof(string) });
                setTextMethod?.Invoke(amountLabel, new object[] { string.Empty });
            }
        }
    }
}
