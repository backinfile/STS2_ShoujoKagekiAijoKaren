using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.relic;

/// <summary>
/// 骄傲 - 给予自身易伤，战斗结束时获得随机遗物
/// </summary>
public sealed class KarenArrogant : KarenBaseCardModel
{
    public KarenArrogant() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate, CardKeyword.Ethereal];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<VulnerablePower>(2m)
    ];
    public override bool CanBeGeneratedInCombat => false;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var effect = NKarenArrogantVfx.Play(Owner);
        bool completed = false;
        try
        {
            if (effect != null) await NKarenArrogantVfx.WaitForRelease();
            int vulnerableBefore = Owner.Creature.GetPowerAmount<VulnerablePower>();
            var power = await PowerCmd.Apply<VulnerablePower>(choiceContext, Owner.Creature, DynamicVars.Vulnerable.BaseValue, Owner.Creature, this);
            if (power != null) power.SkipNextDurationTick = false;
            if (Owner.Creature.GetPowerAmount<VulnerablePower>() > vulnerableBefore) effect?.ConfirmVulnerable();

            // Confirm only after the real extra reward has been registered.
            await ExtraRewardCmd.AddRelicReward(Owner);
            effect?.ConfirmReward();
            completed = true;
        }
        finally
        {
            if (!completed) effect?.Abort();
        }
    }

    protected override void OnUpgrade()
    {
        // 减少易伤层数
        DynamicVars.Vulnerable.UpgradeValueBy(-1);
    }
}
