using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.shine;

/// <summary>
/// 星光指引 - 将所有闪耀牌放入约定牌堆
/// </summary>
public sealed class KarenStarGuide : KarenBaseCardModel
{
    public KarenStarGuide() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获取所有牌堆中的闪耀牌
        var allCards = Owner.CardPiles.DrawPile.Concat(Owner.CardPiles.DiscardPile)
            .Concat(Owner.CardPiles.HandPile).OfType<KarenBaseCardModel>().ToList();

        var shineCards = allCards.Where(card => card.IsShineCard()).ToList();

        // 将所有闪耀牌移动到约定牌堆
        foreach (var shineCard in shineCards)
        {
            await PromisePileCmd.Add(shineCard);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
