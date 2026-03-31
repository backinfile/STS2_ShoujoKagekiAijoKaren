using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// Power类 - Banana午餐的回合结束效果
/// </summary>
public class KarenBananaLunchPower : PowerModel
{
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerType PowerType => PowerType.Buff;

    public override async Task OnSideTurnEnd(PlayerChoiceContext choiceContext, bool isPlayerTurn)
    {
        if (!isPlayerTurn) return;

        // 添加三明治到约定牌堆
        for (int i = 0; i < Amount; i++)
        {
            await PromisePileCmd.AddToken<KarenSandwitch>(Owner, CombatState, 1);
        }
    }
}