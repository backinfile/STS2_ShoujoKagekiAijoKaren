using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using ShoujoKagekiAijoKaren.src.Models.Relics;

namespace ShoujoKagekiAijoKaren.src.KarenMod.Patches;

[HarmonyPatch(typeof(TouchOfOrobas), nameof(TouchOfOrobas.GetUpgradedStarterRelic))]
public static class TouchOfOrobasPatch
{
	[HarmonyPostfix]
	private static void Postfix(RelicModel starterRelic, ref RelicModel __result)
	{
		// 调试日志
		MainFile.Logger.Info($"[TouchOfOrobasPatch] starterRelic.Id={starterRelic.Id}, result={__result?.Id}");

		var karenHairpinId = ModelDb.Relic<KarenHairpinRelic>().Id;
		MainFile.Logger.Info($"[TouchOfOrobasPatch] KarenHairpinRelic.Id={karenHairpinId}");

		// 使用 Id 比较而不是类型检查，确保兼容规范实例和可变实例
		if (starterRelic.Id == karenHairpinId)
		{
			MainFile.Logger.Info("[TouchOfOrobasPatch] Match found! Setting result to KarenHairpin2Relic");
			__result = ModelDb.Relic<KarenHairpin2Relic>().ToMutable();
		}
	}
}
