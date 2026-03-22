using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Helpers;

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
        // 只处理本地玩家且是 Karen 角色
        if (!LocalContext.IsMe(__instance.Entity)) return;
        var player = __instance.Entity.Player;
        if (player?.Character.Id.Entry != "Karen") return;

        __instance.Hitbox.Connect(
            Control.SignalName.GuiInput,
            Callable.From<InputEvent>(evt => OnHitboxInput(__instance, evt, player))
        );
    }

    private static void OnHitboxInput(NCreature creature, InputEvent evt, Player player)
    {
        if (evt is not InputEventMouseButton mb) return;
        if (mb.ButtonIndex != MouseButton.Left || mb.Pressed) return; // 等鼠标释放

        // 保护1：正在选目标时不触发（避免干扰瞄准卡牌）
        if (NTargetManager.Instance.IsInSelection) return;

        // 保护2：选目标刚结束的同一帧不触发（避免残留点击）
        if (NTargetManager.Instance.LastTargetingFinishedFrame == creature.GetTree().GetFrame()) return;

        // 消费输入，阻止穿透
        creature.GetViewport().SetInputAsHandled();

        if (!CombatManager.Instance.IsInProgress) return;

        int count = PromisePileManager.GetCount(player);
        if (count == 0)
        {
            if (NCapstoneContainer.Instance?.InUse == true)
                NCapstoneContainer.Instance.Close();
            var msg = new LocString("gameplay_ui", "KAREN_PROMISE_PILE_EMPTY");
            var bubble = NThoughtBubbleVfx.Create(msg.GetFormattedText(), player.Creature, 2.0);
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(bubble);
        }
        else if (NCapstoneContainer.Instance?.CurrentCapstoneScreen is NCardPileScreen)
        {
            NCapstoneContainer.Instance.Close();
        }
        else
        {
            PromisePileManager.ShowScreen(player);
        }
    }
}
