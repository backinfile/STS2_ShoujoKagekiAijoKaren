using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// Power类 - 燃烧吧燃烧吧：从约定牌堆抽出的牌将被升级，且被打出时消耗
/// </summary>
public class KarenBurnPower : PowerModel
{
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerType PowerType => PowerType.Buff;

    public override async Task OnAfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card)
    {
        // 检查是否来自约定牌堆 - 通过检查卡牌是否曾经进入约定牌堆
        if (card is KarenBaseCardModel karenCard && PromisePileManager.IsInPromisePile(card))
        {
            // 升级这张牌
            if (!card.IsUpgraded)
            {
                card.Upgrade();
            }

            // 标记为消耗
            card.AddKeyword(CardKeyword.Exhaust);
        }
    }
}
