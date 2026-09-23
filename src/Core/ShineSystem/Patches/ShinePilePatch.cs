using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
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
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Shine.ShinePatches;

/// <summary>
/// 闪耀牌堆核心流程（实现位于 ShinePatch）
///
/// 原理：
/// 1. OnPlayWrapper 状态机首次执行时递减闪耀，并将 choiceContext 按卡牌存入 SpireField。
/// 2. ModifyCardPlayResultPileTypeAndPosition Prefix 判定是否进入闪耀耗尽牌堆。
/// 3. CardPileCmd.Add Prefix 拦截该牌堆，从 SpireField 取出 ctx 完成耗尽。
/// </summary>
public static class ShinePilePatch
{


}

/// <summary>
/// 支持写法 CardPile.Get(KarenCustomEnum.PromisePile, player)，返回玩家的约定牌堆。
/// </summary>
[HarmonyPatch(typeof(CardPile), nameof(CardPile.Get))]
public static class PromiseGetPatch
{
    [HarmonyPrefix]
    private static bool Prefix(PileType type, Player player, ref CardPile? __result)
    {
        if (type == KarenCustomEnum.ShineDepletePile)
        {
            __result = ShinePileManager.GetShinePile(player);
            return false;
        }
        return true;
    }
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
        int total = pile.Cards.Count;
        int unique = ShinePileManager.GetDisposedShineCardUniqueCount(__instance);

        if (total == 0)
        {
            MainFile.Logger.Info($"[ShinePile] 战斗结束 — 闪耀牌堆为空");
            return;
        }

        var cardList = string.Join(", ", pile.Cards.Select(c => $"{c.Title}({c.GetShineMaxValue()})"));
        MainFile.Logger.Info($"[ShinePile] 战斗结束 — 共 {total} 张（{unique} 种）: {cardList}");
    }
}
