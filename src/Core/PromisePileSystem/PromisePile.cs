using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Models;
namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
public sealed class PromisePile() : CustomPile(KarenCustomEnum.PromisePile)
{
    public override bool CardShouldBeVisible(CardModel card) => false;
    public override Vector2 GetTargetPosition(CardModel card, Vector2 size)
        => KarenPromiseVfxStarManager.GetTowerPosition(card.Owner) ?? new Vector2(size.X * .5f, size.Y * .5f);
}

// BaseLib assigns CustomEnum values during ModelDb.Init, after mod initializers.
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.InitIds))]
internal static class PromisePileRegistration
{
    [HarmonyPostfix]
    private static void Postfix() => CustomPiles.RegisterCustomPile(KarenCustomEnum.PromisePile, () => new PromisePile());
}
