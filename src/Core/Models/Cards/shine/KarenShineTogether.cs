using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShoujoKagekiAijoKaren.src.Core;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

/// <summary>
/// 一起闪耀吧 - 2费稀有攻击牌，造成30/40点伤害。闪耀3。
/// Shine耗尽时，获得三次闪耀牌奖励。
/// </summary>
public sealed class KarenShineTogether : KarenBaseCardModel
{
    public KarenShineTogether() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        this.AddShineMax(3);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(20m, ValueProp.Move)
    ];

    protected override HashSet<CardTag> CanonicalTags => [KarenCustomEnum.ShineCardReward];

    public override IEnumerable<CardKeyword> CanonicalKeywords => []; // CardKeyword.Exhaust

    public override bool CanBeGeneratedInCombat => false;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(VfxCmd.slashPath)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(10m);
    }

    public override async Task OnShineExhausted(PlayerChoiceContext ctx, bool inCombat, CombatState combatState)
    {
        if (Owner != null)
        {
            for (int i = 0; i < 3; i++)
                await ExtraRewardCmd.AddShineCardReward(Owner);
        }
    }
}
