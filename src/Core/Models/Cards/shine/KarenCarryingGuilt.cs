using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

/// <summary>
/// 背负着我们犯下的罪过 - 2费12伤，本局每耗尽过一种不同的闪耀牌+4伤
/// 升级：15伤，每耗尽一种+5伤
/// </summary>
public sealed class KarenCarryingGuilt : KarenBaseCardModel
{
    public KarenCarryingGuilt() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    public override IEnumerable<CardTag> Tags => [KarenCustomEnum.ShineRelated];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(12m),
        new ExtraDamageVar(4m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier((card, _) => ShinePileManager.GetDisposedShineCardUniqueCount(card.Owner))
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;
        ClearAllFocus();
        NKarenStageLightVfx.Play(cardPlay.Target);
        await Task.Delay(250);
        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            //.WithHitFx(VfxCmd.heavyBluntPath)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(3m);
        DynamicVars.ExtraDamage.UpgradeValueBy(1m);
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        var count = IsCanonical ? 0 : ShinePileManager.GetDisposedShineCardUniqueCount(Owner);
        description.Add("ShinePileUniqueCount", count);
    }


    // ============== 以下为特效


    private static readonly SpireField<NCreature, NKarenStageLightVfx?> FocusVfx = new(() => null);
    private static void Clear(NCreature creature)
    {
        var existing = FocusVfx.Get(creature);
        if (existing != null && GodotObject.IsInstanceValid(existing))
            existing.Stop();

        FocusVfx.Set(creature, null);
    }


    public override void OnCreatureHover(NCreature creature)
    {
        if (!LocalContext.IsMe(Owner))
            return;

        var existing = FocusVfx.Get(creature);
        if (existing != null && GodotObject.IsInstanceValid(existing))
            return;

        FocusVfx.Set(creature, NKarenStageLightVfx.StartFocus(creature.Entity));
    }

    public override void OnCreatureUnhover(NCreature creature)
    {
        if (!LocalContext.IsMe(Owner))
            return;

        Clear(creature);
    }

    public override void OnCreatureHoverCleanup(NCreature creature)
    {
        if (!LocalContext.IsMe(Owner))
            return;

        Clear(creature);
    }

    private static void ClearAllFocus()
    {
        foreach (var creature in NCombatRoom.Instance?.CreatureNodes ?? [])
            Clear(creature);
    }
}

