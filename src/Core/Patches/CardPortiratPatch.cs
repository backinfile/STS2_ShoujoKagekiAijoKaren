using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Patches
{
    internal class CardPortiratPatch
    {


        //[HarmonyPatch(typeof(CardModel), "PortraitPngPath", MethodType.Getter)]
        //class CustomCardPortraitPath
        //{
        //    [HarmonyPrefix]
        //    static bool UseAltTexture(CardModel __instance, ref string? __result)
        //    {
        //        if (__instance is not KarenBaseCardModel customCard) return true;

        //        __result = ImageHelper.GetImagePath($"packed/card_portraits/karen/{}/.png");
        //        return __result == null;
        //    }
        //}
    }
}
