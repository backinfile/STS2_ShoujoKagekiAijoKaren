using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;

/// <summary>
/// 世界上最空虚的人 - 摧毁抽牌堆，然后以约定牌堆代替抽牌堆
/// </summary>
public sealed class KarenVoid : KarenBaseCardModel
{
    public KarenVoid() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.PlayerCombatState;
        if (combatState == null) return;

        // 消耗抽牌堆
        var drawPileCards = combatState.DrawPile.Cards.ToList();
        foreach (var card in drawPileCards)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }
        // 切换模式
        await PromisePileCmd.EnterMode(Owner, PromisePileMode.Void);


        // 取出约定牌堆中的所有牌
        var pile = PromisePileManager.GetPromisePile(Owner);
        var pileCards = pile.Cards.ToList();
        pile.Clear();
        // 放入约定牌堆
        foreach (var card in pileCards)
        {
            await PromisePileCmd.Add(Owner, card);
        }

    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
