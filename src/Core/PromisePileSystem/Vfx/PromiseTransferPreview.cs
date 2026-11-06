using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.ancient;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.basic;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.globalMove;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.relic;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.shine;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.strength;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;
using ShoujoKagekiAijoKaren.src.Models.Cards;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

internal enum PromiseTransferPreview { None, Put, Take, Both, Tower }

/// <summary>Active transfers, including choices and post-play transfers. Related tags also
/// include passive triggers and count-based attacks, so they cannot identify this preview.</summary>
internal static class PromiseTransferPreviews
{
    public static PromiseTransferPreview ForCard(CardModel card)
    {
        if (card.Type == CardType.Power) return PromiseTransferPreview.None;
        var preview = card switch
        {
            KarenTowerOfPromise => PromiseTransferPreview.Tower,
            KarenFall or KarenWhy or KarenOurPromise or KarenMeetAgain or KarenYesterglow
                or KarenNoHesitate or KarenStarGuide or KarenCourageStrike or KarenDodge
                or KarenLanding or KarenNewSituation or KarenNewDay or KarenRevueDuet
                or KarenRun or KarenStretching => PromiseTransferPreview.Put,
            KarenPromiseDraw or KarenOnStage or KarenDance or KarenBridge => PromiseTransferPreview.Take,
            KarenWakeUp when card.IsUpgraded || PromisePileManager.IsVoidMode(card.Owner)
                => PromiseTransferPreview.Take,
            KarenWhosPromise or KarenExchangeFate or KarenRapid => PromiseTransferPreview.Both,
            _ => PromiseTransferPreview.None
        };

        // Void uses the native Draw pile as its visible tower inventory. These
        // active draws therefore share the same take preview as PromisePile.Draw.
        if (PromisePileManager.IsVoidMode(card.Owner) && DrawsFromDrawPile(card))
            preview = AddTake(preview);

        // New Day also redirects ordinary cards, beyond the static card list.
        // Do not invoke native result-pile calculation here: it consumes
        // ExhaustOnNextPlay. Shine depletion overrides the later pile move.
        if (card.Owner?.Creature.GetPower<KarenNewDayPower>()?.Amount > 0
            && card is not KarenBaseCardModel { SkipNewDayPower: true }
            && card is not KarenFightRelay
            && !card.IsDupe && !card.ExhaustOnNextPlay
            && !card.Keywords.Contains(CardKeyword.Exhaust) && !ShineVfx.WillDeplete(card))
        {
            preview = preview switch
            {
                PromiseTransferPreview.None => PromiseTransferPreview.Put,
                PromiseTransferPreview.Take => PromiseTransferPreview.Both,
                _ => preview
            };
        }
        return preview;
    }

    private static bool DrawsFromDrawPile(CardModel card) => card is
        KarenOurPromise or KarenMeetAgain or KarenRun or KarenBananaMuffin
        or KarenDropFuel or KarenReady or KarenStarCrime or KarenWakeUp
        || card is KarenToTheStage { IsUpgraded: true };

    private static PromiseTransferPreview AddTake(PromiseTransferPreview preview) => preview switch
    {
        PromiseTransferPreview.None => PromiseTransferPreview.Take,
        PromiseTransferPreview.Put => PromiseTransferPreview.Both,
        _ => preview
    };
}
