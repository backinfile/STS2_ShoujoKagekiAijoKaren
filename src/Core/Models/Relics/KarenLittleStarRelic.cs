using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers.tmpStrength;

namespace ShoujoKagekiAijoKaren.src.Models.Relics;

public sealed class KarenLittleStarRelic : KarenBaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<KarenTowerOfPromise>(false), MechanicTip("KAREN_TMP_STRENGTH")];

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || cardPlay.Card is not KarenTowerOfPromise) return;
        Flash();
        await PowerCmd.Apply<KarenLittleStarTempStrengthPower>(choiceContext, Owner.Creature,
            DynamicVars.Strength.BaseValue, Owner.Creature, cardPlay.Card);
    }
}
