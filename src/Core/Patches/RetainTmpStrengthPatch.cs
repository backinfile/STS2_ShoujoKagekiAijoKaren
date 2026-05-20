using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// 当玩家身上有KarenRetainTmpStrengthPower时，临时力量不会在回合结束时消失
/// </summary>
[HarmonyPatch(typeof(TemporaryStrengthPower), nameof(TemporaryStrengthPower.AfterTurnEnd))]
public class RetainTmpStrengthPatch
{
    private static bool Prefix(TemporaryStrengthPower __instance, PlayerChoiceContext choiceContext, CombatSide side, ref Task __result)
    {
        // 检查是否是该生物的回合结束
        if (side != __instance.Owner.Side)
        {
            return true; // 不是该生物的回合，让原方法继续执行
        }

        // 检查该生物是否有保留临时力量Power
        var retainPower = __instance.Owner.GetPower<KarenRetainTmpStrengthPower>();
        if (retainPower != null)
        {
            // 有保留临时力量Power，跳过原方法的移除逻辑
            // 不执行原方法，阻止临时力量被移除
            __result = Task.CompletedTask; // 设置返回值为已完成的任务，表示不需要执行原方法
            return false;
        }

        // 没有保留临时力量Power，让原方法正常执行
        return true;
    }
}
