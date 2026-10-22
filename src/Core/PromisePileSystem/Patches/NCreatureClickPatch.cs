using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using static Godot.Control;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Patches;

/// <summary>
/// 为华恋的战斗角色模型（NCreature Hitbox）添加点击事件，点击打开约定牌堆界面
/// </summary>
[HarmonyPatch(typeof(NCreature), "_Ready")]
public static class NCreatureClickPatch
{
    [HarmonyPostfix]
    private static void AddClickToKarenCreature(NCreature __instance)
    {
        if (__instance.Hitbox == null) return;
        // 只处理本地玩家
        if (!LocalContext.IsMe(__instance.Entity)) return;
        var player = __instance.Entity.Player;
        if (player == null) return;
        // 都可以点，但没有约定牌堆能力时点了也没事（不打开界面），所以不需要这个条件了
        // if (player.PlayerCombatState.Powers.All(p => p is not KarenPromisePilePower)) return;

        // 确保 Hitbox 能接收鼠标事件
        __instance.Hitbox.MouseFilter = MouseFilterEnum.Stop;
        KarenPileInput.Bind(__instance.Hitbox,
            () => CanInspectPile(__instance, player),
            () => TryOpenPile(__instance, player),
            () => __instance.ShowHoverTips(player.Creature.GetPower<KarenPromisePilePower>()!.HoverTips),
            __instance.HideHoverTips);
        MainFile.Logger.Info($"Added click event to player {player.Creature.Name} Hitbox");
    }

    internal static bool CanInspectPile(NCreature creature, Player player)
    {
        if (!LocalContext.IsMe(player) || !CombatManager.Instance.IsInProgress) return false;
        if (NCombatRoom.Instance?.Ui.Hand.InCardPlay == true) return false;
        // 保护1：正在选目标时不触发（避免干扰瞄准卡牌）
        if (NTargetManager.Instance.IsInSelection) return false;

        // 保护2：选目标刚结束的同一帧不触发（避免残留点击）
        if (NTargetManager.Instance.LastTargetingFinishedFrame == (long)creature.GetTree().GetFrame()) return false;

        // 唯一条件：玩家身上有约定牌堆能力
        if (!player.Creature.HasPower<KarenPromisePilePower>()) return false;
        // Modal screens handle their own clicks. Do not replace an unrelated screen.
        if (NCapstoneContainer.Instance?.CurrentCapstoneScreen is { } current && current is not NCardPileScreen) return false;

        return true;
    }

    internal static bool TryOpenPile(NCreature creature, Player player)
    {
        if (!CanInspectPile(creature, player)) return false;
        // Void 模式：打开抽牌堆界面
        if (PromisePileManager.IsVoidMode(player))
        {
            // 获取抽牌堆快照（不会影响真实抽牌顺序）
            NCardPileScreen.ShowScreen(PileType.Draw.GetPile(player), []);
            return true;
        }

        // 有 Power 且非 Void 模式时打开/关闭界面
        if (NCapstoneContainer.Instance?.CurrentCapstoneScreen is NCardPileScreen)
        {
            NCapstoneContainer.Instance.Close();
        }
        else
        {
            PromisePileManager.ShowScreen(player);
        }
        return true;
    }
}
