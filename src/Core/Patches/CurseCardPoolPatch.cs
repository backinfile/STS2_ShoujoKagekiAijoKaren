using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using ShoujoKagekiAijoKaren.src.Models.Cards;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// 将 Karen 的诅咒牌注入到原版 CurseCardPool，使其参与诅咒卡池抽取逻辑。
/// </summary>
[HarmonyPatch(typeof(CurseCardPool), "GenerateAllCards")]
public static class CurseCardPoolPatch
{
    private static void Postfix(ref CardModel[] __result)
    {
        __result =
        [
            ..__result,
            ModelDb.Card<KarenSleepy>(),
            ModelDb.Card<KarenStageReason>(),
        ];
    }
}
