using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;

/// <summary>
/// 我将再次变为我自己 - 1(升0)费技能，从约定牌堆抽4张牌。获得2点能量。消耗。
/// </summary>
public sealed class KarenOnStage : KarenBaseCardModel
{
    protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { KarenCustomEnum.PromisePileRelated };

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public KarenOnStage() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(4),
        new EnergyVar(2),
    ];


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var effect = NKarenStageCueVfx.Play(Owner, NKarenStageCueVfx.CueKind.Duet, this);
        try {
            if (effect != null) await NKarenStageCueVfx.WaitForCue(NKarenStageCueVfx.CueKind.Duet);
            for (int i = 0; i < (int)DynamicVars.Cards.BaseValue; i++) {
                var drawn = await PromisePileCmd.Draw(choiceContext, Owner);
                if (drawn == null) break;
                effect?.ConfirmCards([drawn]);
            }
            int before = Owner.PlayerCombatState?.Energy ?? 0;
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
            effect?.ConfirmEnergy((Owner.PlayerCombatState?.Energy ?? before) - before);
        } finally { effect?.Finish(); }
    }

    protected override void OnUpgrade()
    {
        // 升级后费用变为0
        EnergyCost.UpgradeBy(-1);
    }
}
