using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.ancient;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.basic;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.globalMove;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.shine;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
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
        return card switch
        {
            KarenTowerOfPromise => PromiseTransferPreview.Tower,
            KarenFall or KarenWhy or KarenOurPromise or KarenMeetAgain or KarenYesterglow
                or KarenNoHesitate or KarenStarGuide or KarenCourageStrike or KarenDodge
                or KarenLanding or KarenNewSituation or KarenNewDay or KarenRevueDuet
                or KarenRun or KarenStretching => PromiseTransferPreview.Put,
            KarenPromiseDraw or KarenOnStage or KarenDance or KarenBridge => PromiseTransferPreview.Take,
            KarenWakeUp when card.IsUpgraded && !PromisePileManager.IsVoidMode(card.Owner)
                => PromiseTransferPreview.Take,
            KarenWhosPromise or KarenExchangeFate or KarenRapid => PromiseTransferPreview.Both,
            _ => PromiseTransferPreview.None
        };
    }
}
