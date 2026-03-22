using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

/// <summary>
/// 武术练习 - 1费获得10格挡，Shine 9
/// 特效：Shine 耗尽时额外回复10HP（升级后14HP）
/// 升级：格挡14，回复14
/// </summary>
public sealed class KarenPractice : CardModel
{
    public KarenPractice() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        this.AddShineMax(9);
        ShinePileManager.OnCardEnteredShinePile += OnShineCardEntered;
    }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(10m, ValueProp.Move),
        new HealVar(10m)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
        DynamicVars.Heal.UpgradeValueBy(4m);
    }

    private void OnShineCardEntered(CardModel card)
    {
        if (card != this) return;
        // 自动取消订阅，避免重复触发
        ShinePileManager.OnCardEnteredShinePile -= OnShineCardEntered;

        // 耗尽时回复 HP（fire-and-forget）
        if (Owner != null)
            _ = CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
    }
}
