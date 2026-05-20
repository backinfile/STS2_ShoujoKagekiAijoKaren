using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using ShoujoKagekiAijoKaren.src.Core.SplashScreen;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// 在官方 MegaCrit Slash 动画之前插入 Karen 自定义 Splash。
/// 通过 HarmonyReversePatch 调用原方法快照，避免递归触发 Prefix，同时保留原版 Splash。
/// </summary>
[HarmonyPatch(typeof(NLogoAnimation), nameof(NLogoAnimation.PlayAnimation))]
public static class NLogoAnimationPatch
{
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPatch(typeof(NLogoAnimation), nameof(NLogoAnimation.PlayAnimation))]
    public static Task OriginalPlayAnimation(NLogoAnimation instance, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public static bool Prefix(NLogoAnimation __instance, CancellationToken token, ref Task __result)
    {
        if (token.IsCancellationRequested)
        {
            __result = Task.CompletedTask;
            return false;
        }

        __result = PlayWithSplashAsync(__instance, token);
        return false;
    }

    private static async Task PlayWithSplashAsync(NLogoAnimation logoAnimation, CancellationToken token)
    {
        if (KarenModConfig.ShowSplashScreen)
        {
            await KarenSplashScreen.Play(logoAnimation, token);
        }

        await OriginalPlayAnimation(logoAnimation, token);
    }
}
