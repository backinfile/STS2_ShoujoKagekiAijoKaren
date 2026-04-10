using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace ShoujoKagekiAijoKaren.src.Core.ExtraReplaySystem.Patches;

/// <summary>
/// Harmony 补丁：将 CardModel 的 ExtraReplayCountForNextPlay 字段应用到实际重播次数中
/// </summary>
public static class ExtraReplayPatch
{
    /// <summary>
    /// Patch Hook.ModifyCardPlayCount，在返回值上累加卡牌的 ExtraReplayCountForNextPlay，然后清零
    /// </summary>
    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyCardPlayCount))]
    public static class ModifyCardPlayCount_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref int __result, CardModel card)
        {
            if (card == null)
                return;

            int extra = card.GetExtraReplayCountForNextPlay();
            if (extra != 0)
            {
                __result += extra;
                MainFile.Logger.Info($"[ExtraReplayPatch] '{card.Title}' 额外重播 {extra} 次，总播放次数调整为 {__result}");
                card.SetExtraReplayCountForNextPlay(0);
            }
        }
    }

    // 不需要复制
    ///// <summary>
    ///// MutableClone 补丁 - 在卡牌克隆时复制 ExtraReplayCountForNextPlay
    ///// SpireField 不会被 MemberwiseClone 自动复制
    ///// </summary>
    //[HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.MutableClone))]
    //public static class MutableClone_Patch
    //{
    //    [HarmonyPostfix]
    //    public static void Postfix(AbstractModel __instance, AbstractModel __result)
    //    {
    //        if (__instance is not CardModel source || __result is not CardModel clone)
    //            return;

    //        int extraReplay = source.GetExtraReplayCountForNextPlay();
    //        if (extraReplay != 0)
    //        {
    //            clone.SetExtraReplayCountForNextPlay(extraReplay);
    //        }
    //    }
    //}
}
