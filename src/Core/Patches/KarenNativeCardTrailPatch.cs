using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.Models.Characters;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.TrailPath), MethodType.Getter)]
internal static class KarenNativeCardTrailPatch
{
    private static void Postfix(CharacterModel __instance, ref string __result)
    {
        if (__instance is Karen && !KarenVfxQuality.Enabled)
            __result = ModelDb.Character<Ironclad>().TrailPath;
    }
}
