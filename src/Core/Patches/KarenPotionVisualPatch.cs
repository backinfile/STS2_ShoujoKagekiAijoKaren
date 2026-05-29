using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Potions;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

[HarmonyPatch(typeof(PotionModel))]
public static class KarenPotionVisualPatch
{
    [HarmonyPatch("PackedImagePath", MethodType.Getter)]
    [HarmonyPrefix]
    private static bool PackedImagePathPrefix(PotionModel __instance, ref string? __result)
    {
        if (__instance is not KarenBasePotion karenPotion)
        {
            return true;
        }

        __result = karenPotion.KarenImagePath;
        return false;
    }

    [HarmonyPatch("PackedOutlinePath", MethodType.Getter)]
    [HarmonyPrefix]
    private static bool PackedOutlinePathPrefix(PotionModel __instance, ref string? __result)
    {
        if (__instance is not KarenBasePotion karenPotion)
        {
            return true;
        }

        __result = karenPotion.KarenOutlinePath;
        return false;
    }
}
