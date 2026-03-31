using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// Power类 - 舞台正在等待着：约定牌堆被清空时，获得能量
/// </summary>
public class KarenStageIsWaitingPower : PowerModel
{
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerType PowerType => PowerType.Buff;

    public override async Task OnPromisePileEmptied(PlayerChoiceContext choiceContext)
    {
        // 获得能量
        await PlayerCmd.GainEnergy((int)Amount, Owner);
        await VfxCmd.EnergyGain(choiceContext, Owner, (int)Amount);
    }
}
