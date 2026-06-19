using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Models.Characters;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

[HarmonyPatch(typeof(CharacterModel), "AttackSfx", MethodType.Getter)]
public static class KarenAttackSfxPatch
{
    private static bool Prefix(CharacterModel __instance, ref string __result)
    {
        if (__instance is not Karen) return true;

        __result = Karen.OriginalCharacterAttackSfx;
        return false;
    }
}

[HarmonyPatch(typeof(CharacterModel), "CastSfx", MethodType.Getter)]
public static class KarenCastSfxPatch
{
    private static bool Prefix(CharacterModel __instance, ref string __result)
    {
        if (__instance is not Karen) return true;

        __result = Karen.OriginalCharacterCastSfx;
        return false;
    }
}

[HarmonyPatch(typeof(CharacterModel), "DeathSfx", MethodType.Getter)]
public static class KarenDeathSfxPatch
{
    private static bool Prefix(CharacterModel __instance, ref string __result)
    {
        if (__instance is not Karen) return true;

        __result = Karen.OriginalCharacterDeathSfx;
        return false;
    }
}
