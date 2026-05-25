using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using ShoujoKagekiAijoKaren.src.Models.CardPools;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;

/// <summary>
/// 空壳 - 0费 token 技能牌。消耗。
/// </summary>
public sealed class KarenEmptyShell : KarenBaseCardModel
{
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<KarenCardPool>();

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        new HoverTip(
            new LocString("cards", "KAREN_EMPTY_SHELL.tip.title"),
            new LocString("cards", "KAREN_EMPTY_SHELL.tip").GetFormattedText()
        )
    ];

    public KarenEmptyShell() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }
}
