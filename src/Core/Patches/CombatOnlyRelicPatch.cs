using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// 让五轮书和招财异鱼的效果只在战斗外生效。
/// 在战斗中（Owner.Creature.CombatState != null）时，跳过原方法。
/// 同时在描述末尾追加"(仅在战斗外有效)"提示。
/// </summary>
public static class CombatOnlyRelicPatch
{
    [HarmonyPatch(typeof(BookOfFiveRings), nameof(BookOfFiveRings.AfterCardChangedPiles))]
    public static class BookOfFiveRingsPatch
    {
        private static bool Prefix(BookOfFiveRings __instance, ref Task __result)
        {
            if (__instance.Owner?.Creature == null || CombatManager.Instance?.IsInProgress == true)
                return Async.Prefix(ref __result);
            return true;
        }
    }

    [HarmonyPatch(typeof(LuckyFysh), nameof(LuckyFysh.AfterCardChangedPiles))]
    public static class LuckyFyshPatch
    {
        private static bool Prefix(LuckyFysh __instance, ref Task __result)
        {
            if (__instance.Owner?.Creature == null || CombatManager.Instance?.IsInProgress == true)
                return Async.Prefix(ref __result);
            return true;
        }
    }

    [HarmonyPatch(typeof(RelicModel), nameof(RelicModel.HoverTip), MethodType.Getter)]
    public static class RelicHoverTipPatch
    {
        private static void Postfix(RelicModel __instance, ref HoverTip __result)
        {
            if (__instance is BookOfFiveRings || __instance is LuckyFysh)
            {
                string suffix = " (仅在战斗外有效)";
                var newTip = new HoverTip(__instance.Title, __result.Description + suffix, __result.Icon)
                {
                    Id = __result.Id,
                    IsSmart = __result.IsSmart,
                    IsDebuff = __result.IsDebuff,
                    IsInstanced = __result.IsInstanced,
                    ShouldOverrideTextOverflow = __result.ShouldOverrideTextOverflow
                };
                if (__result.CanonicalModel != null)
                    newTip.SetCanonicalModel(__result.CanonicalModel);
                __result = newTip;
            }
        }
    }
}
