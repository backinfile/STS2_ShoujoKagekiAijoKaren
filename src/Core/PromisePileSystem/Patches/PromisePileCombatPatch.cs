using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Runs;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
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
    private static void Postfix(IRunState runState, CombatState? combatState)
    {
        if (combatState == null) return;
        foreach (var player in combatState.Players)
        {
            PromisePileManager.ClearPromisePileInternal(player);
            // 战斗开始时为华恋角色初始化 Power
            _ = PromisePileManager.InitPowerAsync(player);
        }
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
    private static async Task Postfix(CombatState combatState, CombatSide side)
    {
        // 只处理玩家回合结束
        if (side != CombatSide.Player) return;

        foreach (var player in combatState.Players)
        {
            var pile = PromisePileManager.GetPromisePile(player);
            if (pile.Cards.Count > 0)
            {
                var cards = string.Join(", ", pile.Cards.Select(c => $"'{c.Title}'"));
                MainFile.Logger.Info($"[PromisePile] Turn end - {pile.Cards.Count} card(s): {cards}");

                // 触发约定牌堆中卡牌的回合结束扳机（非 Void 模式）
                if (!PromisePileManager.IsVoidMode(player))
                {
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
                MainFile.Logger.Info($"[PromisePile] Turn end - empty");
            }
        }
    }
}
