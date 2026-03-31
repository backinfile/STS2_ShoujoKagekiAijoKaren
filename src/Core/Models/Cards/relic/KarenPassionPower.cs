using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.relic;

/// <summary>
/// Power类，战斗结束时获得随机遗物
/// </summary>
public class KarenPassionPower : PowerModel
{
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerType PowerType => PowerType.Buff;

    public override async Task OnCombatEnd(PlayerChoiceContext choiceContext)
    {
        // 获得随机遗物 - 使用RewardCmd或类似机制
        // TODO: 实现获得遗物的逻辑
        await Task.CompletedTask;
    }
}
