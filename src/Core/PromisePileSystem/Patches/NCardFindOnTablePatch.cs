using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Patches;

/// <summary>
/// Patch NCard.FindOnTable 以支持约定牌堆（PromisePile）类型。
///
/// 背景：
/// - NCard.FindOnTable 使用 switch 表达式处理 PileType
/// - 对于未知的 PileType（如 PromisePile = 7），会抛出 ArgumentOutOfRangeException
/// - 约定牌堆是"虚拟牌堆"，没有对应的 UI 节点，应返回 null
/// </summary>
[HarmonyPatch(typeof(NCard), nameof(NCard.FindOnTable))]
internal static class NCardFindOnTablePatch
{
    [HarmonyPrefix]
    private static bool Prefix(CardModel card, ref NCard? __result)
    {
        // 拦截约定牌堆类型：直接返回 null，避免进入原方法的 switch 表达式
        if (card?.Pile?.Type == KarenCustomEnum.PromisePile)
        {
            __result = null;
            return false; // 跳过原方法
        }

        // 其他情况：继续执行原方法
        return true;
    }
}

/// <summary>
/// Patch PileTypeExtensions.GetTargetPosition 以支持约定牌堆（PromisePile）类型。
///
/// 背景：
/// - GetTargetPosition 使用 switch 表达式处理 PileType
/// - 对于未知的 PileType（如 PromisePile = 7），会抛出 ArgumentOutOfRangeException
/// - 约定牌堆没有对应的 UI 节点，返回角色位置作为默认位置
/// </summary>
[HarmonyPatch(typeof(PileTypeExtensions), nameof(PileTypeExtensions.GetTargetPosition))]
internal static class PileTypeExtensionsGetTargetPositionPatch
{
    [HarmonyPrefix]
    private static bool Prefix(PileType pileType, NCard? node, ref Vector2 __result)
    {
        // 拦截约定牌堆类型：返回角色位置
        if (pileType == KarenCustomEnum.PromisePile)
        {
            // 优先从 node 获取玩家角色位置
            if (node?.Model?.Owner?.Creature is { } creature)
            {
                var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
                if (creatureNode != null)
                {
                    __result = creatureNode.VfxSpawnPosition;
                    return false;
                }
            }

            // 备选：屏幕中心
            var viewport = NGame.Instance?.GetViewportRect() ?? new Rect2(0, 0, 1920, 1080);
            __result = new Vector2(viewport.Size.X * 0.5f, viewport.Size.Y * 0.5f);
            return false; // 跳过原方法
        }

        // 其他情况：继续执行原方法
        return true;
    }
}
