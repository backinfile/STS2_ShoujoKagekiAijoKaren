using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// 世界上最空虚的人 - 摧毁抽牌堆，然后以约定牌堆代替抽牌堆
/// </summary>
public sealed class KarenVoid : KarenBaseCardModel
{
    public KarenVoid() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 消耗抽牌堆
        var drawPileCards = Owner.CardPiles.DrawPile.ToList();
        foreach (var card in drawPileCards)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        // 将约定牌堆变成新的抽牌堆
        var promisePileCards = PromisePileManager.GetPromisePile(Owner).Cards.ToList();
        foreach (var card in promisePileCards)
        {
            await PromisePileCmd.Add(card);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
