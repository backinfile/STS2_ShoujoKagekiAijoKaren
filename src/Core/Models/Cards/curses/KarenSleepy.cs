using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

/// <summary>
/// 困意 - 诅咒牌，不可打出，虚无，灵魂绑定。
/// </summary>
public sealed class KarenSleepy : KarenBaseCardModel
{

    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[]
    {
        CardKeyword.Unplayable, // 不可打出
        CardKeyword.Ethereal, // 虚无
        CardKeyword.Eternal, // 灵魂绑定
    };

    public KarenSleepy() : base(-2, CardType.Curse, CardRarity.Curse, TargetType.None) { }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 诅咒牌不可打出，什么都不做
        return Task.CompletedTask;
    }

}
