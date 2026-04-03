using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
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
    public override PowerType Type => PowerType.Buff;

    // TODO: 需要找到正确的扳机方法名
    // 原方法 OnSideTurnStart 在基类中不存在
}
