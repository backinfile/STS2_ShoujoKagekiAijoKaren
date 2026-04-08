using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token.options;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;

/// <summary>
/// 不再犹豫
/// </summary>
public sealed class KarenNoHesitate : KarenBaseCardModel
{
    public KarenNoHesitate() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        this.AddShineMax(9);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmdEx.SelectOption(choiceContext, cardPlay, Owner, CombatState, [
                    ModelDb.Card<KarenNoHesitateDrawPileOption>(),
                    ModelDb.Card<KarenNoHesitateHandOption>(),
                    ModelDb.Card<KarenNoHesitateDiscardPileOption>()
                    ], IsUpgraded);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
