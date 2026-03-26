using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// 信（Letter）- 每当有卡牌被放入约定牌堆，获得 X 点格挡
/// </summary>
public sealed class KarenLetterPower : KarenBasePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task OnCardAddedToPromisePile(CardModel card)
    {
        // 获得格挡
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
        // 触发Power闪烁效果
        Flash();
    }
}
