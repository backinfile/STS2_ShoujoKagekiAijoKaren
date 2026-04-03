using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
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
    public override PowerType Type => PowerType.Buff;

    // TODO: 需要找到正确的扳机方法名
    // 原方法 OnAfterCardPlayed 在基类中不存在
}
