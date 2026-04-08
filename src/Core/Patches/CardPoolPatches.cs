using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token.options;
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

/// <summary>
/// 将 Karen 的Token牌注入到原版 TokenCardPool。
/// </summary>
[HarmonyPatch(typeof(TokenCardPool), "GenerateAllCards")]
public static class TokenCardPoolPatch
{
    private static void Postfix(ref CardModel[] __result)
    {
        __result =
        [
            ..__result,
            ModelDb.Card<KarenContinue>(),
            ModelDb.Card<KarenTowerOfPromise>(),
            ModelDb.Card<KarenConfront>(),
            ModelDb.Card<KarenCounter>(),
            ModelDb.Card<KarenSandwitch>(),
            ModelDb.Card<KarenSideways>(),

            ModelDb.Card<KarenWakeUpDrawPileOption>(),
            ModelDb.Card<KarenWakeUpDiscardPileOption>(),
            ModelDb.Card<KarenWakeUpPromisePileOption>(),

            ModelDb.Card<KarenNoHesitateDiscardPileOption>(),
            ModelDb.Card<KarenNoHesitateDrawPileOption>(),
            ModelDb.Card<KarenNoHesitateHandOption>(),

            ModelDb.Card<KarenOldPlaceRetainBlockOption>(),
            ModelDb.Card<KarenOldPlaceRetainEnergyOption>(),
            ModelDb.Card<KarenOldPlaceRetainStrengthOption>(),
        ];
    }
}