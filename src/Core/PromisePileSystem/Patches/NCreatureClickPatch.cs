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
        __instance.Hitbox.Connect(
            Control.SignalName.GuiInput,
            Callable.From<InputEvent>(evt => OnHitboxInput(__instance, evt, player))
        );
        MainFile.Logger.Info($"Added click event to player {player.Creature.Name} Hitbox");
    }

    private static void OnHitboxInput(NCreature creature, InputEvent evt, Player player)
    {
        if (evt is not InputEventMouseButton mb) return;
        if (mb.ButtonIndex != MouseButton.Left || mb.Pressed) return;

        // 保护1：正在选目标时不触发（避免干扰瞄准卡牌）
        if (NTargetManager.Instance.IsInSelection) return;

        // 保护2：选目标刚结束的同一帧不触发（避免残留点击）
        if (NTargetManager.Instance.LastTargetingFinishedFrame == (long)creature.GetTree().GetFrame()) return;

        // 消费输入，阻止穿透
        creature.GetViewport().SetInputAsHandled();

        if (!CombatManager.Instance.IsInProgress) return;

        // 唯一条件：玩家身上有约定牌堆能力
        if (!player.Creature.HasPower<KarenPromisePilePower>()) return;

        // Void 模式：打开抽牌堆界面
        if (PromisePileManager.IsVoidMode(player))
        {
            // 获取抽牌堆快照（不会影响真实抽牌顺序）
            NCardPileScreen.ShowScreen(PileType.Draw.GetPile(player), []);
            return;
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
    }
}
