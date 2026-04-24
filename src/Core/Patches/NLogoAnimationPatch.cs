using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using ShoujoKagekiAijoKaren.src.Core.SplashScreen;
using System.Threading;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// 在官方 MegaCrit Slash 动画之前插入 Karen 自定义 Splash。
/// 使用标志位避免递归：第一次拦截并包裹 Splash，第二次放行原方法。
/// </summary>
[HarmonyPatch(typeof(NLogoAnimation), nameof(NLogoAnimation.PlayAnimation))]
public static class NLogoAnimationPatch
{
    private static bool _isInSplash;

    public static bool Prefix(NLogoAnimation __instance, CancellationToken token, ref Task __result)
    {
        if (token.IsCancellationRequested)
        {
            __result = Task.CompletedTask;
            return false;
        }

        if (_isInSplash)
        {
            _isInSplash = false;
            return true; // 放行原方法
        }

        __result = PlayWithSplashAsync(__instance, token);
        return false; // 拦截原方法
    }

    private static async Task PlayWithSplashAsync(NLogoAnimation logoAnimation, CancellationToken token)
    {
        if (KarenModConfig.ShowSplashScreen)
        {
            await KarenSplashScreen.Play(logoAnimation, token);
        }

        _isInSplash = true;
        await logoAnimation.PlayAnimation(token);
    }
}
