using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using ShoujoKagekiAijoKaren.Core;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// 在 NEnergyCounter.OnEnergyChanged 中，当能量增加时，
/// 如果实例是 SKEnergyCounter，也触发其子类自定义的 _myBackVfx / _myFrontVfx。
/// </summary>
[HarmonyPatch(typeof(NEnergyCounter), "OnEnergyChanged")]
public class NEnergyCounterOnEnergyChangedPatch
{
    private static void Postfix(NEnergyCounter __instance, int oldEnergy, int newEnergy)
    {
        if (oldEnergy >= newEnergy) return;
        if (__instance is SKEnergyCounter counter)
        {
            counter._myBackVfx?.Restart();
            counter._myFrontVfx?.Restart();
        }
    }
}
