using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// CardPileCmd.AutoPlayFromDrawPile 的 Postfix Patch
/// 在自动打出抽牌堆卡牌后更新约定牌堆 Power 显示
/// </summary>
[HarmonyPatch(typeof(CardPileCmd))]
[HarmonyPatch(nameof(CardPileCmd.AutoPlayFromDrawPile))]
public static class CardPileCmdAutoPlayPatch
{
    public static void Postfix(ref Task __result, Player player)
    {
        Async.Postfix(ref __result, AfterAutoPlay(player));
    }

    public static void Prefix(ref Task __result, Player player, int count)
    {
        // 虚空模式，自动打出抽牌堆卡牌，可能会导致约定牌堆变空
        if (PromisePileManager.IsVoidMode(player))
        {
            if (count >= PileType.Draw.GetPile(player).Cards.Count)
            {
                _ = PromisePileHooks.TriggerPromisePileEmpty(player);
            }
        }
    }

    private static async Task AfterAutoPlay(Player player)
    {
        // 更新约定牌堆 Power
        await PromisePileManager.UpdatePowerAsync(player);
    }
}
