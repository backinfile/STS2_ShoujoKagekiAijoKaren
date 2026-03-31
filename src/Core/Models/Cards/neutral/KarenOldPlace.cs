using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// 明年也要在这里相见 - 获得格挡，保留手牌2回合
/// </summary>
public sealed class KarenOldPlace : KarenBaseCardModel
{
    public KarenOldPlace() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(10m),
        new TurnsVar(2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得格挡
        await BlockCmd.Gain(DynamicVars.Block.BaseValue, Owner.Creature, this)
            .Execute(choiceContext);

        // 应用保留手牌的Power
        await PowerCmd.Apply<RetainHandPower>(Owner.Creature, DynamicVars.Turns.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
        DynamicVars.Turns.UpgradeValueBy(1m);
    }
}
