using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;

/// <summary>
/// 新的一天 - 获得格挡，此牌和你打出的下X张牌进入约定牌堆
/// </summary>
public sealed class KarenNewDay : KarenBaseCardModel
{
    public KarenNewDay() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8, ValueProp.Move),
        new CardsVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 应用Power来标记接下来的卡牌进入约定牌堆
        await PowerCmd.Apply<KarenNewDayPower>(Owner.Creature, DynamicVars.Cards.BaseValue, Owner.Creature, this);

        // 此牌本身进入约定牌堆
        await PromisePileCmd.Add(this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
