using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
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
    public override PowerType Type => PowerType.Buff;

    // TODO: 需要找到正确的扳机方法名
    // 原方法 OnSideTurnEnd 在基类中不存在
}
