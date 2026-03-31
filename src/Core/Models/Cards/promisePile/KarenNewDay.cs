using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
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

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m),
        new CardsVar(2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得格挡
        await BlockCmd.Gain(DynamicVars.Block.BaseValue, Owner.Creature, this)
            .Execute(choiceContext);

        // 应用Power来标记接下来的卡牌进入约定牌堆
        await PowerCmd.Apply<KarenNewDayPower>(Owner.Creature, DynamicVars.Cards.BaseValue, Owner.Creature, this);

        // 此牌本身进入约定牌堆
        await PromisePileCmd.Add(this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
