using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

/// <summary>
/// 占位能力牌
/// 无效果，仅作占位用
/// </summary>
public sealed class KarenPlaceholderPower : CardModel
{
    public KarenPlaceholderPower() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 占位卡牌，无效果
        await Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        // 占位卡牌，升级无效果
    }
}
