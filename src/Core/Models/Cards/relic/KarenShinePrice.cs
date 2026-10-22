using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.relic;

/// <summary>
/// 双星：禁用 X 个遗物，造成 5（升级 6）点伤害 2X 次。
/// </summary>
public sealed class KarenShinePrice : KarenDisableRelicBaseCardModel
{
    public KarenShinePrice() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override bool HasEnergyCostX => true;

    protected override HashSet<CardTag> CanonicalTags => [KarenCustomEnum.DisableRelicRelated];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new RepeatVar(2),
    ];

    // Use prospective energy while in hand; CapturedXValue is only set when resources are spent.
    public override int DisableRelicCount => IsCanonical || CombatState == null ? 0
        : Hook.ModifyXValue(CombatState, this, EnergyCost.GetAmountToSpend());

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Capture the played X, including its modifiers, before disabling relics.
        var x = ResolveEnergyXValue();
        // Auto-plays and replays bypass CanPlay; never attack with an unpaid relic cost.
        if (DisableRelicCmd.GetDisableableRelicCount(Owner) < x) return;
        var hitCount = x * DynamicVars.Repeat.IntValue;
        var target = cardPlay.Target;
        NKarenTwinStarsVfx? effect = null;
        int hitIndex = 0;
        try
        {
            // Capture actual relic holders before replacement; each relic feeds one twin-star pair.
            if (target != null) effect = NKarenTwinStarsVfx.Begin(Owner, target, x);
            await DisableRelicCmd.DisableRelic(Owner, x,
                NDisableRelicAbsorbVfx.TargetKind.None);
            if (target == null) return;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(hitCount)
                .FromPlayedCard(this, cardPlay)
                .Targeting(target)
                .OnlyPlayAnimOnce()
                .WithHitVfxNode(victim => { effect?.Launch(victim, hitIndex++); return null; })
                .BeforeDamage(async () => { if (effect != null) await effect.TravelToContact(); })
                .Execute(choiceContext);
        }
        finally
        {
            effect?.Finish();
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
    }
}
