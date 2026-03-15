using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;

namespace Sts2Mod.chaosed0.sts2examplemod.src;

public static class CardPoolPatch 
{
    public static CardModel[] InjectSuperStrike(CardModel[] values)
    {
        Log.Info($"GenerateCards Postfix\n{new StackTrace()}");
        return values.Concat([ModelDb.Card<SuperStrike>()]).ToArray();
    }
}

[HarmonyPatch(typeof(NGame), "_Ready")]
public class NGamePatch 
{
    static void Postfix()
    {
        Log.Info($"NGame._Ready");
    }
}

[HarmonyPatch(typeof(NGame), nameof(NGame.StartNewSingleplayerRun))]
public class NGamePatch2
{
    static void Postfix()
    {
        Log.Info($"NGame.StartNewSinglePlayerRun");
    }
}