using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// 将 Karen 的事件遗物注入到原版 EventRelicPool。
/// 要添加新的事件遗物，只需在 <see cref="GetKarenEventRelics"/> 中添加即可。
/// </summary>
[HarmonyPatch(typeof(EventRelicPool), "GenerateAllRelics")]
public static class EventRelicPoolPatch
{
    /// <summary>
    /// 获取所有 Karen 的事件遗物列表。
    /// 添加新事件遗物时，在此处添加对应的 ModelDb.Relic<T>() 调用。
    /// </summary>
    private static IEnumerable<RelicModel> GetKarenEventRelics()
    {
        return [
            ModelDb.Relic<KarenLockRelic>()
        ];
    }

    public static void Postfix(ref IEnumerable<RelicModel> __result)
    {
        MainFile.Logger.Info("[EventRelicPoolPatch] Injecting Karen event relics into EventRelicPool...");
        __result = __result.Concat(GetKarenEventRelics());
    }
}
