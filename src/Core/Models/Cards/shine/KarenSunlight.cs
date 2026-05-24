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
public sealed class KarenSunlight : KarenBaseCardModel
{
    public override bool ShineCardForOtherPlayer => false;

    public KarenSunlight() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        this.AddShineMax(3);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(10m, ValueProp.Move),
    };

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
        DynamicVars.Damage.UpgradeValueBy(4m);
    }

    public override async Task OnShineExhausted(PlayerChoiceContext ctx, bool inCombat, CombatState combatState)
    {
        MainFile.Logger.Info("KarenSunlight.OnShineExhausted triggered. Prompting player to add KarenSunlight back to deck.");
        if (Owner == null)
        {
            MainFile.Logger.Error("KarenSunlight.OnShineExhausted called but Owner is null.");
            return;
        }

        // 战斗已经结束了。没有奖励界面的战斗不能依赖额外奖励，否则耀光会丢到下一场。
        if (!inCombat)
        {
            // 这些战斗没有可靠的额外奖励领取入口：
            // 1. Encounter.ShouldGiveRewards == false 时，NCombatUi 会直接跳过奖励界面；
            // 2. 与本体 RewardsSet.WithRewardsFromRoom 保持一致：最后一层 Boss 不生成奖励。
            if (!HasRewardScreenAfterCombat())
            {
                var deckClone = this.CloneSafeForDeck();
                deckClone.RestoreShineToMax();
                var result = await CardPileCmd.Add(deckClone, PileType.Deck);
                CardCmd.PreviewCardPileAdd(result, 1.2f, CardPreviewStyle.MessyLayout);
                MainFile.Logger.Info($"Added KarenSunlight directly back to deck because this combat has no reward screen. Clone ID: {deckClone.Id}");
                return;
            }

            var player = Owner;
            var reward = new SpecialCardReward(this.CloneSafeForDeck(), player);
            if (Owner.RunState.CurrentRoom is CombatRoom combatRoom)
            {
                // 战斗房间，加入最后的奖励里
                combatRoom.AddExtraReward(Owner, reward);
                MainFile.Logger.Info("Add KarenSunlight reward to combat room's extra rewards.");
            }
            else
            {
                // 非战斗房间，直接发
                await RewardsCmd.OfferCustom(player, [reward]);
                MainFile.Logger.Info("Offered KarenSunlight reward directly to player outside of combat room.");
            }
            return;
        }

        // 还在战斗中，创建一个选择卡牌界面来让玩家选择是否将这张牌加入牌组
        // 创建一个自身的复制，需要有CombatState才能加入手牌
        CardModel clone = combatState?.CloneCard(this)!;
        if (clone == null)
        {
            MainFile.Logger.Error("KarenSunlight.OnShineExhausted failed to clone card for deck addition.");
            return;
        }

        clone.RestoreShineToMax();
        clone.ResetEnchantmentStatus();

        //var selected = await CardSelectCmdEx.FromChooseACardScreen(ctx, [clone], base.Owner, canSkip: true, new LocString("gameplay_ui", "KAREN_SUNLIGHT_OBTAIN_PROMPT"));
        var selected = await CardSelectCmd.FromChooseACardScreen(ctx, [clone], base.Owner, canSkip: true);

        // 选择不加入牌组
        if (selected == null)
        {
            //clone.RemoveFromState(); 
            _ = CardPileCmd.RemoveFromCombat(clone); // 不需要等待
            MainFile.Logger.Info("Player chose to skip adding KarenSunlight back to deck after shine exhaustion.");
            return;
        }

        // 选择加入牌组
        MainFile.Logger.Info("Player chose to add KarenSunlight back to deck after shine exhaustion.");
        {
            // 创建一个牌组中的牌的复制重新加入牌组
            var deckClone = clone.CloneSafeForDeck();
            if (deckClone == null)
            {
                MainFile.Logger.Error($"Failed to clone KarenSunlight deckVersion for combat shine exhaustion. Owner or RunState was null.");
                return;
            }
            deckClone.RestoreShineToMax();
            CardPileAddResult result = await CardPileCmd.Add(deckClone, PileType.Deck);
            CardCmd.PreviewCardPileAdd(result, 1.2f, CardPreviewStyle.MessyLayout);
            MainFile.Logger.Info($"Cloned KarenSunlight for combat shine exhaustion and added to deck. Clone ID: {deckClone.Id}");


            // 需要将这张牌重新放入手牌
            clone.DeckVersion = deckClone; // 关联这两张牌
            await CardPileCmd.Add(clone, PileType.Hand);
        }
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
