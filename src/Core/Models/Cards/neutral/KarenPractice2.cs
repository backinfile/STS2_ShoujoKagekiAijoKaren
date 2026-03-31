using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// 舞蹈练习 - 获得所有牌闪耀值之和的格挡
/// </summary>
public sealed class KarenPractice2 : KarenBaseCardModel
{
    public KarenPractice2() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new ExtraBlockVar(2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 计算所有牌的闪耀值之和
        var allCards = Owner.CardPiles.DrawPile.Concat(Owner.CardPiles.DiscardPile)
            .Concat(Owner.CardPiles.HandPile).Concat(Owner.CardPiles.ExhaustPile)
            .OfType<KarenBaseCardModel>().ToList();

        var totalShine = allCards.Sum(card => card.GetShineValue());

        var totalBlock = totalShine;
        if (IsUpgraded)
        {
            totalBlock += DynamicVars.ExtraBlock.BaseValue;
        }

        await BlockCmd.Gain(totalBlock, Owner.Creature, this).Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.ExtraBlock.UpgradeValueBy(1m);
    }
}
