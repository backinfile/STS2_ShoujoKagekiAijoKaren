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
/// 添麻烦了 - 此牌在牌堆之间移动时，获得格挡
/// </summary>
public sealed class KarenCry : KarenBaseCardModel
{
    private decimal _blockAmount;

    public KarenCry() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _blockAmount = DynamicVars.Block.BaseValue;
    }

    public override async Task OnCardGlobalMoved(PlayerChoiceContext choiceContext, CardModel card, PileType fromPile, PileType toPile)
    {
        if (card == this && fromPile != toPile)
        {
            await BlockCmd.Gain(_blockAmount, Owner, this).Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}
