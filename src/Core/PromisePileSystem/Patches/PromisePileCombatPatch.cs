using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Runs;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Patches;

/// <summary>
/// 约定牌堆战斗生命周期 Patch
/// - 战斗开始时清空（安全保障）
/// - 战斗结束时清空（将滞留卡牌从 CombatState 注销）
/// - 每回合结束时打印约定牌堆内容
/// </summary>

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCombatStart))]
internal static class PromisePile_BeforeCombatStart_Patch
{
    [HarmonyPostfix]
    private static void Postfix(IRunState runState, CombatState? combatState, ref Task __result)
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
                __result = PromisePileManager.InitPowerAsync(player);
                return;
            }
        }

        __result = Task.CompletedTask;
        return;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.AfterCombatEnd))]
internal static class PromisePile_AfterCombatEnd_Patch
{
    [HarmonyPostfix]
    private static void Postfix(Player __instance)
    {
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
        // 只处理玩家回合结束
        if (side != CombatSide.Player) return;

        __result = HandlePromisePileTurnEndTrigger(combatState);
    }

    private static async Task HandlePromisePileTurnEndTrigger(CombatState combatState)
    {
        if (LocalContext.GetMe(combatState) is Player player)
        {
            MainFile.Logger.Info("[PromisePile] Handling turn end trigger for player's promise pile...");
            // 触发约定牌堆中卡牌的回合结束扳机（非 Void 模式）
            if (!PromisePileManager.IsVoidMode(player)) 
            {
                var pile = PromisePileManager.GetPromisePile(player);
                foreach (var card in pile.Cards.ToList())
                {
                    if (card is KarenBaseCardModel karenCard)
                    {
                        await karenCard.OnTurnEndInPromisePile();
                    }
                }
            }
            else // 空虚模式，处理抽牌堆中的牌
            {
                var pile = PileType.Draw.GetPile(player);
                foreach (var card in pile.Cards.ToList())
                {
                    if (card is KarenBaseCardModel karenCard)
                    {
                        await karenCard.OnTurnEndInPromisePile();
                    }
                }
            }
        }
        else
        {
            MainFile.Logger.Warn("[PromisePile] Failed to get player for turn end trigger.");
        }
    }
}
