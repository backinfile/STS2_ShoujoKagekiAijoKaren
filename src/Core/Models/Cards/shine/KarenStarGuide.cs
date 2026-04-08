using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
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

    protected override HashSet<CardTag> CanonicalTags => [KarenCustomEnum.PromisePileRelated];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate, CardKeyword.Exhaust];



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.PlayerCombatState;
        if (combatState == null) return;

        // 获取所有牌堆中的闪耀牌
        var hand = PileType.Hand.GetPile(Owner);
        var drawPile = PileType.DrawPile.GetPile(Owner);
        var discardPile = PileType.DiscardPile.GetPile(Owner);

        var handCards = hand.Cards.Where(c=>c.IsShineCard()).ToList();
        var drawPileCards = drawPile.Cards.Where(c => c.IsShineCard()).ToList();
        var discardPileCards = discardPile.Cards.Where(c => c.IsShineCard()).ToList();
        // 先把这些牌移除原本的牌堆
        {
            foreach(var card in handCards)
            {
                card.RemoveFromPile();
            }
            foreach(var card in drawPileCards)
            {
                card.RemoveFromPile();
            }
            foreach (var card in discardPileCards)
            {
                card.RemoveFromPile();
            }
        }
        // 最后统一加入约定牌堆
        {
            await PromisePileCmd.AddCardsFromPile(Owner, handCards, PileType.Hand);
            await PromisePileCmd.AddCardsFromPile(Owner, drawPileCards, PileType.DrawPile);
            await PromisePileCmd.AddCardsFromPile(Owner, discardPileCards, PileType.DiscardPile);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
