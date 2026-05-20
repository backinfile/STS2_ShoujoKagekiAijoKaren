using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.Core;
using ShoujoKagekiAijoKaren.src.Models.Characters;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

[HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
public static class KarenDeathTransitionPatch
{
    private static void Postfix(NCreature __instance)
    {
        if (__instance.Entity.Player?.Character is not Karen) return;
        if (__instance.Visuals is not SNCreatureVisuals karenVisuals) return;

        karenVisuals.PlayKarenDeathTransition();
    }
}

[HarmonyPatch(typeof(Creature), nameof(Creature.HealInternal))]
public static class KarenHealInternalReviveTransitionPatch
{
    private static void Prefix(Creature __instance, ref bool __state)
    {
        __state = __instance.IsDead;
    }

    private static void Postfix(Creature __instance, bool __state)
    {
        KarenReviveVisualReset.ResetIfKarenRevived(__instance, __state);
    }
}

[HarmonyPatch(typeof(Creature), nameof(Creature.SetCurrentHpInternal))]
public static class KarenSetCurrentHpInternalReviveTransitionPatch
{
    private static void Prefix(Creature __instance, ref bool __state)
    {
        __state = __instance.IsDead;
    }

    private static void Postfix(Creature __instance, bool __state)
    {
        KarenReviveVisualReset.ResetIfKarenRevived(__instance, __state);
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.AfterCombatEnd))]
public static class KarenAfterCombatEndReviveTransitionPatch
{
    private static void Prefix(Player __instance)
    {
        KarenReviveVisualReset.ResetIfKarenAlive(__instance.Creature);
    }
}

public static class KarenReviveVisualReset
{
    public static void ResetIfKarenRevived(Creature creature, bool wasDead)
    {
        if (!wasDead || creature.IsDead) return;
        ResetIfKarenAlive(creature);
    }

    public static void ResetIfKarenAlive(Creature creature)
    {
        if (creature.IsDead) return;
        if (creature.Player?.Character is not Karen) return;
        if (NCombatRoom.Instance?.GetCreatureNode(creature)?.Visuals is not SNCreatureVisuals karenVisuals) return;

        karenVisuals.ResetKarenDeathTransition();
    }
}
