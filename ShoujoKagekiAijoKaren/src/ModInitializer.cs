using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Sts2Mod.chaosed0.sts2examplemod.src;

[ModInitializer(nameof(InitializeMod))]
public static class ModInitializer
{
    public static void InitializeMod()
    {
        Harmony harmony = new Harmony("backinfile.ShoujoKagekiAijoKaren");

        // Example of manual patch
        MethodInfo ironcladCardPoolGeneration =
            typeof(IroncladCardPool).GetMethod("GenerateAllCards", BindingFlags.Instance | BindingFlags.NonPublic);
        HarmonyMethod postfix = typeof(CardPoolPatch).GetMethod(nameof(CardPoolPatch.InjectSuperStrike));
        harmony.Patch(ironcladCardPoolGeneration, postfix: postfix);
        
        // Do attribute-style patches too
        harmony.PatchAll();
    }
}