using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using System;

namespace ShoujoKagekiAijoKaren.src.Core.Shine.ShinePatches;

/// <summary>
/// 闪耀牌堆核心补丁 - 简化方案：SpireField + AsyncLocal
///
/// 原理：
/// 1. OnPlayWrapper 状态机 Prefix：将 choiceContext 存入 AsyncLocal
/// 2. ModifyCardPlayResultPileTypeAndPosition Prefix：判定闪耀耗尽，从 AsyncLocal 取 ctx 存入 SpireField
/// 3. CardPileCmd.Add Prefix：拦截 ShineDepletePile，从 SpireField 取 ctx 调用 HandleShineDepletePileAsync
/// </summary>
public static class ShinePilePatch
{

    
}

/// <summary>
/// 战斗结束后打印 Karen 玩家的闪耀牌堆内容（调试日志）
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.AfterCombatEnd))]
public static class Player_AfterCombatEnd_ShinePilePatch
{
    [HarmonyPostfix]
    static void Postfix(Player __instance)
    {
        if (__instance.Character is not Karen) return;

        var pile = ShinePileManager.GetShinePile(__instance);
        int total = pile.Count;
        int unique = ShinePileManager.GetDisposedShineCardUniqueCount(__instance);

        if (total == 0)
        {
            MainFile.Logger.Info($"[ShinePile] 战斗结束 — 闪耀牌堆为空");
            return;
        }

        var cardList = string.Join(", ", pile.Select(c => $"{c.Title}({c.GetShineMaxValue()})"));
        MainFile.Logger.Info($"[ShinePile] 战斗结束 — 共 {total} 张（{unique} 种）: {cardList}");
    }
}
