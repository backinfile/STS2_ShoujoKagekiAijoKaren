using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace ShoujoKagekiAijoKaren.src.KarenMod.Patches;

[HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter._Ready))]
public class NMerchantCharacterReadyPatch
{
    public static bool Prefix(NMerchantCharacter __instance)
    {
        return HasSpineSprite(__instance);
    }

    internal static bool HasSpineSprite(NMerchantCharacter instance)
    {
        var children = instance.GetChildren();
        return children.Count > 0 && children[0].GetType().Name.Equals(MegaSprite.spineClassName);
    }
}

[HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter.PlayAnimation))]
public class NMerchantCharacterPatch
{
    public static bool Prefix(NMerchantCharacter __instance)
    {
        return NMerchantCharacterReadyPatch.HasSpineSprite(__instance);
    }
}
