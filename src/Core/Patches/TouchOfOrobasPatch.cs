using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using ShoujoKagekiAijoKaren.src.Models.Relics;

namespace ShoujoKagekiAijoKaren.src.KarenMod.Patches;

[HarmonyPatch(typeof(TouchOfOrobas), nameof(TouchOfOrobas.GetUpgradedStarterRelic))]
public static class TouchOfOrobasPatch
{
	private static void Postfix(RelicModel starterRelic, ref RelicModel __result)
	{
		if (starterRelic is KarenHairpinRelic) __result = ModelDb.Relic<KarenHairpin2Relic>().ToMutable();
	}
}
