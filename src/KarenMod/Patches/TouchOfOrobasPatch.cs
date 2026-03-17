using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using ShoujoKagekiAijoKaren.src.Models.Relics;

[HarmonyPatch(typeof(TouchOfOrobas), nameof(TouchOfOrobas.GetUpgradedStarterRelic))]
public static class TouchOfOrobasPatch
{
	private static void Postfix(RelicModel starterRelic, ref RelicModel __result)
	{
		if (starterRelic.Id == ModelDb.Relic<StageHeart>().Id) __result = ModelDb.Relic<StageHeart>().ToMutable();
	}
}
