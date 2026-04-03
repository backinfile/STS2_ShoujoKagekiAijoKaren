using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// Power类 - Position 0：每回合前X次造成未被格挡的攻击伤害时，获得等量格挡
/// </summary>
public class KarenPosition0Power : PowerModel
{
    private int _remainingHits;

    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerType Type => PowerType.Buff;

    // TODO: 需要找到正确的扳机方法名
    // 原方法 OnInitialApplication, OnSideTurnStart, OnAfterDamageGiven 在基类中不存在
}
