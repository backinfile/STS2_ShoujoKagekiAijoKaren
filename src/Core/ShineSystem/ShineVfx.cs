using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem.Nodes;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.ShineSystem;

public static class ShineVfx
{
    public static bool WillDeplete(CardModel card, bool shineAlreadyDecreased = false) => card.IsShineCard() && !card.IsDupe
        && !card.Keywords.Contains(CardKeyword.Eternal)
        && (card.GetShineValue() <= (shineAlreadyDecreased ? 0 : 1) || card.ShouldEnterShinePileAfterPlay()
            || card.Owner?.Creature.Powers.Any(p => p is KarenStarlight02Power) == true);

    private static readonly SpireField<NCard, NKarenShineWarningVfx?> Warnings = new(() => null);
    private static readonly SpireField<NCard, CardModel?> ReturnSources = new(() => null);

    private static void Refresh(NCard card)
    {
        if (!card.IsNodeReady()) return;
        var old = Warnings.Get(card);
        if (GodotObject.IsInstanceValid(old) && old!.BelongsTo(card.Model)) return;
        Clear(card);
        if (card.Model?.IsShineCard() != true) return;
        var warning = new NKarenShineWarningVfx(card, card.Model);
        card.OverlayContainer.AddChild(warning);
        Warnings.Set(card, warning);
    }

    private static void Clear(NCard card)
    {
        ReturnSources.Set(card, null);
        var old = Warnings.Get(card);
        Warnings.Set(card, null);
        if (!GodotObject.IsInstanceValid(old)) return;
        old!.GetParent()?.RemoveChild(old);
        old.QueueFree();
    }

    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
    private static class VisualPatch { private static void Postfix(NCard __instance) => Refresh(__instance); }

    [HarmonyPatch(typeof(NCard), "ReloadOverlay")]
    private static class OverlayPatch { private static void Postfix(NCard __instance) => Refresh(__instance); }

    [HarmonyPatch(typeof(NCard), nameof(NCard.OnFreedToPool))]
    private static class PoolPatch { private static void Prefix(NCard __instance) => Clear(__instance); }

    // DisplayingPile can still be Hand during a completed play. Capture the
    // actual source container before the native move reparents the card.
    [HarmonyPatch(typeof(CardPileCmd), "MoveCardNodeToNewPileBeforeTween")]
    private static class ReturnSourcePatch
    {
        private static void Prefix(NCard cardNode, PileType newPileType)
        {
            ReturnSources.Set(cardNode, newPileType is PileType.Discard or PileType.Draw
                && NCombatRoom.Instance?.Ui.PlayContainer.IsAncestorOf(cardNode) == true ? cardNode.Model : null);
        }
    }

    [HarmonyPatch(typeof(NCardFlyVfx), nameof(NCardFlyVfx._Ready))]
    private static class ReturnPatch
    {
        private static readonly AccessTools.FieldRef<NCardFlyVfx, NCard> Card = AccessTools.FieldRefAccess<NCardFlyVfx, NCard>("_card");
        private static readonly AccessTools.FieldRef<NCardFlyVfx, bool> Adding = AccessTools.FieldRefAccess<NCardFlyVfx, bool>("_isAddingToPile");
        private static bool Prefix(NCardFlyVfx __instance)
        {
            var card = Card(__instance);
            var source = ReturnSources.Get(card);
            ReturnSources.Set(card, null);
            if (!Adding(__instance) || card.Model is not {} model || !ReferenceEquals(source, model)
                || !model.HasShine() || model.IsDupe || !LocalContext.IsMine(model)
                || WillDeplete(model, shineAlreadyDecreased: true) || NCombatUi.IsDebugHidingPlayContainer
                || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant
                || card.GetParent() is not CanvasItem || NCombatRoom.Instance is not {} room) return true;
            var pile = model.Pile?.Type;
            Control? target = pile switch { PileType.Discard => room.Ui.DiscardPile, PileType.Draw => room.Ui.DrawPile, _ => null };
            if (target == null || KarenPromiseVfxStarManager.IsTowerPile(model.Owner, pile!.Value)) return true;
            target = target.GetNodeOrNull<Control>("Icon") ?? target;
            var completion = new TaskCompletionSource();
            AccessTools.Property(typeof(NCardFlyVfx), nameof(NCardFlyVfx.SwooshAwayCompletion)).SetValue(__instance, completion);
            __instance.AddChild(new NKarenShineReturnVfx(card, target, completion));
            return false;
        }
    }
}
