using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// 命运的齿轮 - 复制所有手牌到约定牌堆
/// </summary>
public sealed class KarenGeer : KarenBaseCardModel
{
    public KarenGeer() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = Owner.PlayerCombatState?.Hand;
        if (hand == null) return;

        var handCards = hand.Cards.ToList();

        // 复制所有手牌到约定牌堆
        foreach (var card in handCards)
        {
            var copyCard = card.CreateClone();
            await PromisePileCmd.Add(copyCard);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
