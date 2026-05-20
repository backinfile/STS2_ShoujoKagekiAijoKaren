using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

/// <summary>
/// 存在于舞台的理由 - 诅咒牌，1费，消耗，Shine 3。
/// </summary>
public sealed class KarenStageReason : KarenBaseCardModel
{
    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[]
    {
        CardKeyword.Exhaust
    };

    public KarenStageReason() : base(1, CardType.Curse, CardRarity.Curse, TargetType.Self)
    {
        // 初始化闪耀值
        this.AddShineMax(3);
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 诅咒牌消耗，无正面效果
        return Task.CompletedTask;
    }
}
