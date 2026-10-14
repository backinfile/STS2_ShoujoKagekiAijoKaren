using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.shine;

public sealed class KarenShineTogetherAll() : KarenBaseCardModel(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override bool CanBeGeneratedInCombat => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1)
    ];

    protected override HashSet<CardTag> CanonicalTags =>
    [
        KarenCustomEnum.ShineCardReward
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState ?? Owner?.Creature?.CombatState;
        if (combatState == null) return;
        var effect = NKarenBenefitVfx.Play(Owner, NKarenBenefitVfx.BenefitKind.Shine);
        if (effect != null) await NKarenBenefitVfx.WaitForCue(NKarenBenefitVfx.BenefitKind.Shine);
        foreach (Player player in combatState.Players.Where(p => p?.Creature != null))
        {
            await ExtraRewardCmd.AddShineCardReward(player);
            // The reward has been added to CombatRoom; its marker Power may itself be modified by hooks.
            effect?.ConfirmRecipient(player);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
