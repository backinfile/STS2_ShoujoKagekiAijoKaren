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

    protected override HashSet<CardTag> CanonicalTags => [KarenCustomEnum.PromisePileRelated];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.PlayerCombatState;
        if (combatState == null) return;

        if (PromisePileManager.IsInMode(Owner, PromisePileMode.Void))
        {
            MainFile.Logger.Info($"KarenVoid: Already in Void mode. No action taken.");
            return;
        }

        // 消耗抽牌堆
        // 一瞬间消耗所有卡牌 去看看本体卡牌的实现
        MainFile.Logger.Info($"KarenVoid: Exhausting all cards in draw pile. Count: {combatState.DrawPile.Cards.Count}");
        var drawPileCards = combatState.DrawPile.Cards.ToList();
        await Task.WhenAll(drawPileCards.Select(card => CardCmd.Exhaust(choiceContext, card)));

        // 取出约定牌堆中的所有牌 然后重新放入抽牌堆
        await CardPileCmd.Add(PromisePileManager.GetPromisePile(Owner).Cards.ToList(), PileType.Draw);

        // 切换模式
        await PromisePileCmd.EnterMode(Owner, PromisePileMode.Void);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
