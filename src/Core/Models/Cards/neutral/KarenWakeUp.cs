using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// 唤醒 - 从抽牌堆、弃牌堆或约定牌堆中选择卡牌放入手牌
/// </summary>
public sealed class KarenWakeUp : KarenBaseCardModel
{
    public KarenWakeUp() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override HashSet<CardTag> CanonicalTags => [];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 升级后可以从约定牌堆选择
        if (IsUpgraded)
        {
            await PromisePileCmd.SelectedToHand(choiceContext, Owner, DynamicVars.Cards.IntValue);
        }
        else
        {
            // 从抽牌堆选择
            var drawPile = PileType.Draw.GetPile(Owner);
            if (drawPile.Count > 0)
            {
                var selected = await CardSelectCmd.FromSimpleGrid(
                    choiceContext,
                    drawPile.Cards.ToList(),
                    Owner,
                    new CardSelectorPrefs(Tips.SelectFromDrawToHand, 1, 1));

                if (selected != null)
                {
                    foreach (var card in selected)
                    {
                        await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top, null, false);
                    }
                }
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
