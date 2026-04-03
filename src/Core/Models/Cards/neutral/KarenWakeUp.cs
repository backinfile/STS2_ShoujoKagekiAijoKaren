using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// 唤醒 - 从约定牌堆中选择卡牌放入手牌（升级后）
/// </summary>
public sealed class KarenWakeUp : KarenBaseCardModel
{
    public KarenWakeUp() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

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
            var combatState = Owner.PlayerCombatState;
            if (combatState == null) return;

            var drawPileCards = combatState.DrawPile.Cards;
            if (drawPileCards.Any())
            {
                var selected = await CardSelectCmd.FromSimpleGrid(
                    choiceContext,
                    drawPileCards.ToList(),
                    Owner,
                    new CardSelectorPrefs(
                        new LocString("ui", "SELECT_FROM_DRAW_TO_HAND"),
                        1, 1));

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
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
