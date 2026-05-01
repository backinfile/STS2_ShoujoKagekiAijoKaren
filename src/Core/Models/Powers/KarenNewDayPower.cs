using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Commands;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// 新的一天：此牌和之后打出的下 X 张原本会进入弃牌堆的牌改为进入约定牌堆。
/// </summary>
public class KarenNewDayPower : PowerModel
{
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerType Type => PowerType.Buff;

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position)
    {
        if (card.Owner.Creature != base.Owner)
        {
            return (pileType, position);
        }

        if (pileType != PileType.Discard)
        {
            return (pileType, position);
        }
        return (KarenCustomEnum.PromisePile, CardPilePosition.Top);
    }

    public override async Task AfterModifyingCardPlayResultPileOrPosition(
        CardModel card,
        PileType pileType,
        CardPilePosition position)
    {
        if (card.Owner.Creature != base.Owner)
        {
            return;
        }
        Flash();
        await PowerCmd.Decrement(this);
    }

    //public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    //{
    //    if (cardPlay.Card?.Owner?.Creature != base.Owner)
    //    {
    //        return;
    //    }
    //    await PowerCmd.Decrement(this);
    //}
}
