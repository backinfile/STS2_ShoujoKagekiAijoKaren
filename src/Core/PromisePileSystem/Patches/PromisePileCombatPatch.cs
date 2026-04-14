using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Patches;

/// <summary>
/// 约定牌堆战斗生命周期 Patch
/// - 战斗开始时清空（安全保障）
/// - 战斗结束时清空（将滞留卡牌从 CombatState 注销）
/// - 每回合结束时打印约定牌堆内容
/// 
/// 触发Init要尽早，至少要快于遗物的AfterRoomEntered
/// </summary>

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterRoomEntered))]
internal static class PromisePile_BeforeCombatStart_Patch
{

    [HarmonyPrefix]
    private static void Prefix(AbstractRoom room)
    {
        if (room is CombatRoom combatRoom)
        {
            _ = OnCombatStart(combatRoom.CombatState);
        }
    }

    private static async Task OnCombatStart(CombatState? combatState)
    {
        // 清空所有人的约定牌堆，确保战斗开始时没有残留卡牌
        if (combatState != null)
        {
            foreach (var p in combatState.Players)
            {
                PromisePileManager.ClearPromisePileInternal(p);
            }
        }
        // 战斗开始时为华恋角色初始化 Power
        var player = LocalContext.GetMe(combatState);
        if (player != null)
        {
            if (player.Character is Karen)
            {
                await PromisePileManager.InitPowerAsync(player);
            }
        }
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.AfterCombatEnd))]
internal static class PromisePile_AfterCombatEnd_Patch
{
    [HarmonyPostfix]
    private static void Postfix(Player __instance)
    {
        MainFile.Logger.Info($"[PromisePile] AfterCombatEnd Patch triggered for player {__instance.Character?.Id?.Entry}");
        PromisePileManager.ClearPromisePileInternal(__instance);
    }
}

/// <summary>
/// 每回合结束时打印约定牌堆内容，并触发卡牌回合结束扳机
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterTurnEnd))]
internal static class PromisePile_AfterTurnEnd_Patch
{
    [HarmonyPostfix]
    public static void Postfix(CombatState combatState, CombatSide side, ref Task __result)
    {
        Async.Postfix(ref __result, async () =>
        {
            // 只处理玩家回合结束
            if (side != CombatSide.Player) return;
            // 只处理本机玩家
            if (LocalContext.GetMe(combatState) is Player player)
            {
                // 触发约定牌堆中卡牌的回合结束扳机
                await PromisePileHooks.TriggerPromisePileTurnEnd(player);
                PrintSomething(player);
            }
            else
            {
                MainFile.Logger.Warn("[PromisePile] Failed to get player for turn end trigger.");
            }
        });
    }


    private static void PrintSomething(Player player)
    {
        // 回合结束时打印约定牌堆内容
        MainFile.Logger.Info("[PromisePile] Current promise pile contents at turn end:");
        var pile = KarenCustomEnum.PromisePile.GetPile(player);
        foreach (var card in pile.Cards)
        {
            MainFile.Logger.Info($"[PromisePile] Card in promise pile: {card.Title}");
        }

        // 回合结束打印手牌
        MainFile.Logger.Info("[PromisePile] Current hand contents at turn end:");
        var hand = PileType.Hand.GetPile(player);
        foreach (var card in hand.Cards)
        {
            MainFile.Logger.Info($"[PromisePile] Card in hand: {card.Title}");
        }
    }
}
