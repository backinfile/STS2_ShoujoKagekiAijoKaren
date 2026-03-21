using Godot;
using HarmonyLib;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Runs;
using System;
using System.Threading.Tasks;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

namespace ShoujoKagekiAijoKaren.src.Core.Shine.ShinePatches;

/// <summary>
/// Harmony补丁：统一处理 Shine 关键字逻辑
/// 使用 SpireField 支持任何卡牌动态添加闪耀
///
/// 工作流程：
/// 1. OnPlayWrapper Postfix 中减少闪耀值
/// 2. ShinePilePatch 拦截 CardPileCmd.Add，检查闪耀值==0并重定向到闪耀牌堆
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
public static class ShinePatch
{
    static void Postfix(CardModel __instance, PlayerChoiceContext choiceContext)
    {
        // 只处理有闪耀值的卡牌
        if (!__instance.HasShine()) return;

        // 减少闪耀值
        var newValue = __instance.DecreaseShine();
        MainFile.Logger.Info($"卡牌 '{__instance.Title}' 闪耀值减少至 {newValue}");
    }
}
