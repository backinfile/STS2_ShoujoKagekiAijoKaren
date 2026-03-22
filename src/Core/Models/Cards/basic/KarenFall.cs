using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

/// <summary>
/// 坠落 - 1费技能，获得8格挡，将1张手牌放入约定牌堆。
/// </summary>
public sealed class KarenFall : KarenBaseCardModel
{
    protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { KarenCustomEnum.PromisePileRelated };

    public KarenFall() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(8m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        var prefs = new CardSelectorPrefs(
            new LocString("gameplay_ui", "KAREN_PROMISE_PILE_SELECT"), 1);

        var selected = await CardSelectCmd.FromHand(
            choiceContext, Owner, prefs, c => c != this, this);

        foreach (var card in selected)
            PromisePileCmd.Add(card);
    }

    protected override void OnUpgrade()
        => DynamicVars.Block.UpgradeValueBy(3m);
}
