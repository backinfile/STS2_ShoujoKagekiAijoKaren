using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

/// <summary>
/// 耀眼的阳光 - 1费10伤，Shine 3
/// 特效：Shine 耗尽时获得10格挡（升级后14）
/// 升级：14伤，耗尽时获得14格挡
/// </summary>
public sealed class KarenSunlight : CardModel
{
    public KarenSunlight() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        this.AddShineMax(3);
        ShinePileManager.OnCardEnteredShinePile += OnShineCardEntered;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(10m, ValueProp.Move),
        new BlockVar(10m, (ValueProp)0)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars.Block.UpgradeValueBy(4m);
    }

    private void OnShineCardEntered(CardModel card)
    {
        if (card != this) return;
        ShinePileManager.OnCardEnteredShinePile -= OnShineCardEntered;

        // 耗尽时获得格挡（fire-and-forget）
        if (Owner != null)
            _ = CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, (ValueProp)0, null);
    }
}
