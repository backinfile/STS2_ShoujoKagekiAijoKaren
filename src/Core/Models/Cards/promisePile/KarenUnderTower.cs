using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;

/// <summary>
/// 东京塔下 - 1费技能，获得7(升11)点格挡。将1张*约定之塔*放入手牌。消耗。
/// </summary>
public sealed class KarenUnderTower : KarenBaseCardModel
{
    protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { KarenCustomEnum.PromisePileRelated };

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public KarenUnderTower() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<KarenTowerOfPromise>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7m, ValueProp.Move),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 生成一张约定之塔
        CardModel towerCard = ModelDb.Card<KarenTowerOfPromise>();

        // 加入手牌
        await CardPileCmd.AddGeneratedCardToCombat(towerCard, PileType.Hand, addedByPlayer: true);
    }

    protected override void OnUpgrade()
        => DynamicVars.Block.UpgradeValueBy(4m);
}
