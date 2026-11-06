using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers.tmpStrength;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;

/// <summary>
/// 小零食 - 2费稀有能力，获得临时力量。回合结束时若在约定牌堆中，变化为 Banana。
/// </summary>
public sealed class KarenSmallSnack : KarenBaseCardModel
{
    public KarenSmallSnack() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    public override IEnumerable<CardTag> Tags => [KarenCustomEnum.TmpStrength, KarenCustomEnum.PromisePileRelated];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<KarenBanana>(IsUpgraded)
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(8m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var effect = NKarenBananaVfx.Play(Owner, NKarenBananaVfx.CastKind.Snack);
        if (effect != null) await NKarenBananaVfx.WaitForCue(NKarenBananaVfx.CastKind.Snack);
        if (GodotObject.IsInstanceValid(effect) && !effect!.IsQueuedForDeletion()) effect.WatchStrength();
        await PowerCmd.Apply<KarenSmallSnackTempStrengthPower>(choiceContext, 
            Owner.Creature,
            DynamicVars.Strength.BaseValue,
            Owner.Creature,
            this
        );
        if (GodotObject.IsInstanceValid(effect) && !effect!.IsQueuedForDeletion()) effect.CheckStrength();
    }

    public override async Task OnTurnEndInPromisePile()
    {
        if (CombatState == null) return;

        var effect = NKarenBenefitVfx.Play(Owner, NKarenBenefitVfx.BenefitKind.SnackTransform);
        effect?.TrackTransformation(this);
        if (effect != null) await NKarenBenefitVfx.WaitForCue(NKarenBenefitVfx.BenefitKind.SnackTransform);

        var banana = CombatState.CreateCard<KarenBanana>(Owner);
        if (IsUpgraded)
        {
            CardCmd.Upgrade(banana);
        }

        await CardCmd.Transform(this, banana, CardPreviewStyle.HorizontalLayout);
        effect?.ConfirmCards([banana]);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Strength.UpgradeValueBy(2m);
    }
}
