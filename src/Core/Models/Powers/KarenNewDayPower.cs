using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// 新的一天：此牌和之后打出的下 X 张原本会进入弃牌堆的牌改为进入约定牌堆。
/// </summary>
public class KarenNewDayPower : PowerModel
{
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerType Type => PowerType.Buff;

    internal bool Redirects(CardModel card, PileType pile) =>
        card.Owner.Creature == Owner
        && card is not KarenBaseCardModel { SkipNewDayPower: true }
        && pile == PileType.Discard;

#if STS2_BETA
    public override CardLocation ModifyCardPlayResultLocation(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        CardLocation location)
    {
        if (!Redirects(card, location.pileType))
        {
            return location;
        }

        location.pileType = KarenCustomEnum.PromisePile;
        location.position = CardPilePosition.Bottom;
        return location;
    }

    public override async Task AfterModifyingCardPlayResultLocation(CardModel card, CardLocation location)
#else
    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position)
    {
        if (!Redirects(card, pileType))
        {
            return (pileType, position);
        }
        return (KarenCustomEnum.PromisePile, CardPilePosition.Bottom);
    }

    public override async Task AfterModifyingCardPlayResultPileOrPosition(
        CardModel card,
        PileType pileType,
        CardPilePosition position)
#endif
    {
        if (card.Owner.Creature != base.Owner)
        {
            return;
        }
        Flash();
        await PowerCmd.Decrement(this);
    }

}
