using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// 下一个舞台 - 在牌堆之间移动时抽牌
/// </summary>
public sealed class KarenNextStage : KarenBaseCardModel
{
    private int _cardsToDraw;

    public KarenNextStage() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _cardsToDraw = DynamicVars.Cards.IntValue;
    }

    public override async Task OnCardGlobalMoved(PlayerChoiceContext choiceContext, CardModel card, PileType fromPile, PileType toPile)
    {
        if (card == this && fromPile != toPile && _cardsToDraw > 0)
        {
            await CardPileCmd.Draw(choiceContext, _cardsToDraw, Owner);
            _cardsToDraw = 0;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
