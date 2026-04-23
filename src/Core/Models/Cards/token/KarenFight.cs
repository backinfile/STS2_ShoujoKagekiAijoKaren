using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;

/// <summary>
/// 歌唱吧舞蹈吧互相争斗吧 - 1/0费Token技能牌，Shine 1
/// 从闪耀耗尽牌堆中选择1张牌加入牌组
/// </summary>
public sealed class KarenFight : KarenBaseCardModel
{
    public KarenFight() : base(1, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
        this.AddShineMax(1);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [new HoverTip(
            Tips.KarenFightTipTitle,
            Tips.KarenFightTip.GetFormattedText()
        )];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return;

        var shinePile = ShinePileManager.GetShinePile(Owner);
        var cards = shinePile.Cards
            .Where(c => c is not KarenFight)
            .Select(CombatState.CloneCard)
            .ToList();
        if (cards.Count == 0) return;

        var selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            cards,
            Owner,
            new CardSelectorPrefs(Tips.KarenFightSelectTitle, 0, 1)
        );

        var card = selected.FirstOrDefault();
        if (card == null) return;
        // 删除没有选中的
        foreach (var c in cards) if (c != card) _ = CardPileCmd.RemoveFromCombat(c);

        // 加入牌组
        MainFile.Logger.Info($"cloneToDeck");
        var cloneToDeck = card.CloneSafeForDeck();
        cloneToDeck.RestoreShineToMax();
        CardPileAddResult result = await CardPileCmd.Add(cloneToDeck, PileType.Deck);
        CardCmd.PreviewCardPileAdd(result, 1.2f, CardPreviewStyle.MessyLayout);


        // 需要将这张牌放入手牌
        MainFile.Logger.Info($"cloneToHand");
        card.RestoreShineToMax();
        card.DeckVersion = cloneToDeck; // 关联这两张牌
        await CardPileCmd.Add(card, PileType.Hand);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
