using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token.options;
using ShoujoKagekiAijoKaren.src.Core.Commands;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// 明年也要在这里相见 - 获得格挡，保留手牌2回合
/// </summary>
public sealed class KarenOldPlace : KarenBaseCardModel
{
    public KarenOldPlace() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5, ValueProp.Move),
        new DynamicVar("Turns", 1)
    ];

    public override IEnumerable<CardTag> Tags => [KarenCustomEnum.RetainTmpStrength];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);


        await CardPileCmdEx.SelectOption(choiceContext, cardPlay, Owner, CombatState, [
            ModelDb.Card<KarenOldPlaceRetainBlockOption>(),
            ModelDb.Card<KarenOldPlaceRetainEnergyOption>(),
            ModelDb.Card<KarenOldPlaceRetainStrengthOption>(),
            ModelDb.Card<KarenOldPlaceRetainHandOption>(),
            ], IsUpgraded);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Turns"].UpgradeValueBy(1);
    }
}
