using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// Power类 - 新的一天：跟踪接下来的卡牌进入约定牌堆
/// </summary>
public class KarenNewDayPower : PowerModel
{
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerType PowerType => PowerType.Buff;

    public override async Task OnAfterCardPlayed(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (Amount > 0 && card != null && card != SourceCard)
        {
            // 将卡牌移动到约定牌堆
            await PromisePileCmd.Add(card);
            ChangeAmount(-1);

            if (Amount <= 0)
            {
                RemovePower();
            }
        }
    }
}
