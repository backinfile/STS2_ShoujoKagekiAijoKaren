using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// Power类 - Banana费南雪的保留力量效果
/// </summary>
public class KarenFinancierPower : PowerModel
{
    private int _turnCounter = 0;

    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerType PowerType => PowerType.Buff;

    public override async Task OnSideTurnStart(PlayerChoiceContext choiceContext, bool isPlayerTurn)
    {
        if (!isPlayerTurn) return;

        _turnCounter++;
        if (_turnCounter % 2 == 0)
        {
            // 应用临时力量（会在回合结束时消失）
            await PowerCmd.Apply<TemporaryStrengthPower>(Owner, 1m, Owner, Applier);
        }
    }
}