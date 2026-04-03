using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
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
    public override PowerType Type => PowerType.Buff;

    // TODO: 需要找到正确的扳机方法名
    // 原方法 OnAfterCardDrawn 在基类中不存在
}
