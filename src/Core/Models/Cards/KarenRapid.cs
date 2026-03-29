using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

/// <summary>
/// 极速下降 - 0费技能，从约定牌堆中选择2张加入手牌，弃置其余
/// </summary>
public sealed class KarenRapid : KarenBaseCardModel
{
    public KarenRapid() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override HashSet<CardTag> CanonicalTags => [KarenCustomEnum.PromisePileRelated];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = DynamicVars.Cards.IntValue;

        // 获取约定牌堆中的卡牌
        var promiseCards = PromisePileManager.GetPromisePile(Owner).Cards.ToList();
        if (promiseCards.Count == 0) return;

        if (promiseCards.Count <= count)
        {
            // 如果卡牌数少于等于要取的数，全部加入手牌
            foreach (var card in promiseCards)
            {
                await PromisePileCmd.Draw(choiceContext, Owner);
            }
        }
        else
        {
            // 选择count张加入手牌
            var prefs = new CardSelectorPrefs(
                new LocString("gameplay_ui", "KAREN_RAPID_SELECT_PROMPT"),
                count, count);

            var selected = await CardSelectCmd.FromSimpleGrid(
                choiceContext, promiseCards, Owner, prefs);

            if (selected != null)
            {
                // 先移除选中的，剩下的弃置
                var toDraw = selected.ToList();
                var toDiscard = promiseCards.Where(c => !toDraw.Contains(c)).ToList();

                // 将选中的加入手牌
                foreach (var card in toDraw)
                {
                    card.RemoveFromCurrentPile();
                    await CardPileCmd.Add(card, PileType.Hand);
                }

                // 弃置其余的
                foreach (var card in toDiscard)
                {
                    await CardCmd.Discard(choiceContext, card);
                }
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
