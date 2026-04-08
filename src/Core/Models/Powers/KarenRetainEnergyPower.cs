using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// 保留能量 - 下回合保留未使用的能量
/// </summary>
public sealed class KarenRetainEnergyPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldPlayerResetEnergy(Player player)
    {
        // 只有拥有者且Power数量大于0时才保留能量
        if (player == base.Owner.Player && this.Amount > 0)
        {
            return false;
        }
        return true;
    }

    public override async Task AfterEnergyReset(Player player)
    {
        await PowerCmd.Decrement(this);
    }

}
