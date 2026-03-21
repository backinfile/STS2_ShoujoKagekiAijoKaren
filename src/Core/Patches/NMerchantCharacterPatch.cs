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
#pragma warning disable CS0184
        return node is MegaSprite;
#pragma warning restore CS0184
    }
}