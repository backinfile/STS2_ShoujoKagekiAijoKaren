using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using ShoujoKagekiAijoKaren.Core;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// 在 NEnergyCounter.OnEnergyChanged 中，当能量增加时，
/// 如果实例是 SKEnergyCounter，也触发其子类自定义的 _myBackVfx / _myFrontVfx。
/// </summary>
[HarmonyPatch]
public class NEnergyCounterOnEnergyChangedPatch
{
    static MethodBase TargetMethod()
    {
        var method = AccessTools.Method(typeof(NEnergyCounter), "OnEnergyChanged", new[] { typeof(int), typeof(int) });
        MainFile.Logger.Info($"[NEnergyCounterOnEnergyChangedPatch] TargetMethod resolved: {method?.FullDescription() ?? "NULL"}");
        return method;
    }

    public static void Postfix(NEnergyCounter __instance, int oldEnergy, int newEnergy)
    {
        MainFile.Logger.Info($"[NEnergyCounterOnEnergyChangedPatch] Postfix called, old={oldEnergy}, new={newEnergy}, instanceType={__instance.GetType().Name}");
        if (oldEnergy >= newEnergy) return;
        if (__instance is SNEnergyCounter counter)
        {
            MainFile.Logger.Info("========== OnEnergyChanged");
            counter._myBackVfx?.Restart();
            counter._myFrontVfx?.Restart();
        }
    }
}
