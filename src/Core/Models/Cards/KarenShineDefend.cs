using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

/// <summary>
/// 闪耀防御 - 1费获得8格挡，Shine 5
/// 打出5次后从卡组中移除
///
/// 注意：闪耀显示由全局补丁自动处理（ShineGlobalPatch）
/// 只需在构造函数中初始化闪耀值即可
/// </summary>
public sealed class KarenShineDefend : CardModel
{
    public KarenShineDefend() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        // 初始化闪耀值 - 全局补丁会自动检测并显示
        this.AddShineMax(5);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(8m, ValueProp.Move)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
