using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// 修复 STS2 本体在放弃游戏（Abandon）时，NGameOverScreen 因 _runState.CurrentRoom 为 null
/// 导致 MoveCreaturesToDifferentLayerAndDisableUi 抛出 NullReferenceException 的崩溃。
/// 该方法仅负责视觉表现（移动角色节点、禁用 UI），失败不影响游戏状态。
/// </summary>
[HarmonyPatch(typeof(NGameOverScreen), "MoveCreaturesToDifferentLayerAndDisableUi")]
public class GameOverScreenCrashPatch
{
    private static Exception Finalizer(Exception __exception)
    {
        if (__exception != null)
        {
            MainFile.Logger.Error($"[GameOverScreenCrashPatch] 已抑制 NGameOverScreen 崩溃: {__exception}");
        }
        return null;
    }
}
