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
using ShoujoKagekiAijoKaren.src.Models.Characters;
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
        MainFile.Logger.Info($"[NCreatureClickPatch] Patch triggered for entity: {__instance.Entity?.Name}, IsPlayer: {__instance.Entity?.IsPlayer}");

        // 只处理本地玩家且是 Karen 角色
        if (!LocalContext.IsMe(__instance.Entity))
        {
            MainFile.Logger.Info($"[NCreatureClickPatch] Skipped: Not local player (Entity: {__instance.Entity?.Name})");
            return;
        }

        var player = __instance.Entity.Player;
        MainFile.Logger.Info($"[NCreatureClickPatch] Local player found: {player?.Character?.Id?.Entry}");

        if (player?.Character is not Karen)
        {
            MainFile.Logger.Info($"[NCreatureClickPatch] Skipped: Not Karen character ({player?.Character?.Id?.Entry})");
            return;
        }

        MainFile.Logger.Info($"[NCreatureClickPatch] Connecting click event to Hitbox for Karen");
        MainFile.Logger.Info($"[NCreatureClickPatch] Hitbox is null: {__instance.Hitbox == null}");

        if (__instance.Hitbox == null)
        {
            MainFile.Logger.Error($"[NCreatureClickPatch] Hitbox is null, cannot connect event");
            return;
        }

        MainFile.Logger.Info($"[NCreatureClickPatch] Hitbox MouseFilter before: {__instance.Hitbox.MouseFilter}");

        // 确保 Hitbox 能接收鼠标事件
        __instance.Hitbox.MouseFilter = MouseFilterEnum.Stop;
        MainFile.Logger.Info($"[NCreatureClickPatch] Hitbox MouseFilter after: {__instance.Hitbox.MouseFilter}");

        __instance.Hitbox.Connect(
            Control.SignalName.GuiInput,
            Callable.From<InputEvent>(evt => OnHitboxInput(__instance, evt, player))
        );
        MainFile.Logger.Info($"[NCreatureClickPatch] Click event connected successfully");
    }

    private static void OnHitboxInput(NCreature creature, InputEvent evt, Player player)
    {
        MainFile.Logger.Info($"[NCreatureClickPatch] OnHitboxInput triggered: {evt.GetType().Name}");

        if (evt is not InputEventMouseButton mb)
        {
            MainFile.Logger.Info($"[NCreatureClickPatch] Skipped: Not mouse button event");
            return;
        }

        MainFile.Logger.Info($"[NCreatureClickPatch] Mouse button: {mb.ButtonIndex}, Pressed: {mb.Pressed}");

        if (mb.ButtonIndex != MouseButton.Left || mb.Pressed)
        {
            MainFile.Logger.Info($"[NCreatureClickPatch] Skipped: Not left release (Button: {mb.ButtonIndex}, Pressed: {mb.Pressed})");
            return; // 等鼠标释放
        }

        // 保护1：正在选目标时不触发（避免干扰瞄准卡牌）
        if (NTargetManager.Instance.IsInSelection)
        {
            MainFile.Logger.Info($"[NCreatureClickPatch] Skipped: Target selection in progress");
            return;
        }

        // 保护2：选目标刚结束的同一帧不触发（避免残留点击）
        if (NTargetManager.Instance.LastTargetingFinishedFrame == (long)creature.GetTree().GetFrame())
        {
            MainFile.Logger.Info($"[NCreatureClickPatch] Skipped: Same frame as targeting finished");
            return;
        }

        MainFile.Logger.Info($"[NCreatureClickPatch] Click validated, processing...");

        // 消费输入，阻止穿透
        creature.GetViewport().SetInputAsHandled();

        if (!CombatManager.Instance.IsInProgress)
        {
            MainFile.Logger.Info($"[NCreatureClickPatch] Skipped: Combat not in progress");
            return;
        }

        int count = PromisePileManager.GetCount(player);
        MainFile.Logger.Info($"[NCreatureClickPatch] Promise pile count: {count}");

        if (count == 0)
        {
            MainFile.Logger.Info($"[NCreatureClickPatch] Showing empty bubble");
            if (NCapstoneContainer.Instance?.InUse == true)
                NCapstoneContainer.Instance.Close();
            var msg = new LocString("gameplay_ui", "KAREN_PROMISE_PILE_EMPTY");
            var bubble = NThoughtBubbleVfx.Create(msg.GetFormattedText(), player.Creature, 2.0);
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(bubble);
        }
        else if (NCapstoneContainer.Instance?.CurrentCapstoneScreen is NCardPileScreen)
        {
            MainFile.Logger.Info($"[NCreatureClickPatch] Closing existing screen");
            NCapstoneContainer.Instance.Close();
        }
        else
        {
            MainFile.Logger.Info($"[NCreatureClickPatch] Opening Promise Pile screen");
            PromisePileManager.ShowScreen(player);
        }
    }
}
