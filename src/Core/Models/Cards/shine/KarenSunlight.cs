using ShoujoKagekiAijoKaren.src.Core.Rewards;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Models.Cards;

/// <summary>
/// 耀眼的阳光 - 1费10伤，Shine 3
/// 特效：Shine 耗尽时，玩家可选择将此牌加入牌组。
/// 升级：14伤
/// </summary>
[ShoujoKageki.Core.V1.ShineCard(3)]
public sealed class KarenSunlight : KarenBaseCardModel
{
    public override bool ShineCardForOtherPlayer => false;

    public KarenSunlight() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(10m, ValueProp.Move),
    };

    public override bool CanBeGeneratedInCombat => false;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromPlayedCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx(VfxCmd.slashPath)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }

    public override async Task OnShineExhausted(PlayerChoiceContext ctx, bool inCombat, ICombatState combatState)
    {
        if (Owner == null) return;
        if (!inCombat)
        {
            if (!HasRewardScreenAfterCombat())
            {
                var result = await ShoujoKageki.Core.V1.CardCopies.Acquire(this, Owner);
                CardCmd.PreviewCardPileAdd(result, 1.2f, CardPreviewStyle.MessyLayout);
                return;
            }
            var preview = ShoujoKageki.Core.V1.CardCopies.Preview(this, ShoujoKageki.Core.V1.CardCopyPurpose.Reacquire);
            var reward = new KarenShineCardReward(preview, Owner);
            if (Owner.RunState.CurrentRoom is CombatRoom room) room.AddExtraReward(Owner, reward);
            else await RewardsCmd.OfferCustom(Owner, [reward]);
            return;
        }
        var option = ShoujoKageki.Core.V1.CardCopies.Preview(this, ShoujoKageki.Core.V1.CardCopyPurpose.Reacquire);
        option.ResetEnchantmentStatus();
        var selected = await ShoujoKageki.Core.V1.CardSelection.Choose(ctx, [option], Owner, canSkip: true);
        if (selected == null) return;
        var acquired = await ShoujoKageki.Core.V1.CardCopies.Acquire(this, Owner, addToHand: true);
        CardCmd.PreviewCardPileAdd(acquired, 1.2f, CardPreviewStyle.MessyLayout);
    }

    private bool HasRewardScreenAfterCombat()
    {
        if (Owner?.RunState?.CurrentRoom is CombatRoom combatRoom)
        {
            MainFile.Logger.Info(
                $"KarenSunlight reward-screen check: encounter={combatRoom.Encounter.Id.Entry}, " +
                $"shouldGiveRewards={combatRoom.Encounter.ShouldGiveRewards}, roomType={combatRoom.RoomType}, " +
                $"actIndex={Owner.RunState.CurrentActIndex}, actCount={Owner.RunState.Acts.Count}");

            if (!combatRoom.Encounter.ShouldGiveRewards)
            {
                return false;
            }

            // 模仿本体 RewardsSet.WithRewardsFromRoom：
            // if (room.RoomType == RoomType.Boss && Player.RunState.CurrentActIndex >= Player.RunState.Acts.Count - 1)
            //     return this;
            // 即最后一层 Boss 不生成奖励，额外奖励也没有可靠领取入口。
            // TODO: 如果本体新增第四 Act 或调整 Boss 奖励流程，需要重新确认这里是否仍然成立。
            if (combatRoom.RoomType == RoomType.Boss && Owner.RunState.CurrentActIndex >= Owner.RunState.Acts.Count - 1)
            {
                return false;
            }

            return combatRoom.Encounter.ShouldGiveRewards;
        }

        return true;
    }
}
