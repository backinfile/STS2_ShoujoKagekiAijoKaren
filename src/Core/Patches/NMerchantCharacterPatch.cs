using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace ShoujoKagekiAijoKaren.src.KarenMod.Patches;

[HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter.PlayAnimation))]
public class NMerchantCharacterPatch
{
    public static bool Prefix(NMerchantCharacter __instance)
    {
        var node = __instance.GetChild(0);
        return node is MegaSprite;
    }
}