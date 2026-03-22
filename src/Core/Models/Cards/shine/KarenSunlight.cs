using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
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

    public KarenSunlight() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        this.AddShineMax(3);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(10m, ValueProp.Move),
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
    }

    public override async Task OnShineExhausted(PlayerChoiceContext ctx, bool inCombat)
    {
        if (Owner == null)
        {
            MainFile.Logger.Error("KarenSunlight.OnShineExhausted called but Owner is null.");
            return;
        }

        // 如果不在战斗中，直接复制一张牌加入牌组
        if (!inCombat)
        {
            // 战斗外无法选择，直接复制一张自己加入牌组（this就是牌组中卡牌的本体）
            if (Owner?.RunState?.CloneCard(this) is CardModel clone)
            {
                // 复制的牌需要重置 Shine 为最大值（如果 CloneCard 保留了当前 Shine 则会导致加入牌组后立即被移除）
                clone.RestoreShineToMax();
                // 加入牌组（CardPileCmd.Add 要求 card.Owner != null）
                CardPileAddResult result = await CardPileCmd.Add(clone, PileType.Deck);
                // 显示预览动画（fire-and-forget，不需要 await）
                CardCmd.PreviewCardPileAdd(result, 1.2f, CardPreviewStyle.MessyLayout);
                MainFile.Logger.Info($"Cloned KarenSunlight for non-combat shine exhaustion and added to deck. Clone ID: {clone.Id}");
            }
            else
            {
                MainFile.Logger.Error($"Failed to clone KarenSunlight for non-combat shine exhaustion. Owner or RunState was null.");
            }
            return;
        }

        { // 在战斗内,创建一张自身的复制让玩家选择
            if (Owner?.RunState?.CloneCard(this) is CardModel clone) {
                clone.RestoreShineToMax();

                var prefs = new CardSelectorPrefs(new LocString("gameplay_ui", "KAREN_SUNLIGHT_OBTAIN_PROMPT"), 0, 1);
                var selected = await CardSelectCmd.FromSimpleGrid(ctx, new List<CardModel> { clone }, Owner!, prefs);
                if (selected.Any())
                {
                    // 需要将这张牌重新放入手牌
                    await CardPileCmd.Add(this, PileType.Hand);
                    {
                        // 然后创建一个牌组中的牌的复制重新加入牌组

                        var deckClone = Owner?.RunState?.CloneCard(this);
                        if (deckClone != null)
                        {
                            deckClone.RestoreShineToMax();
                            CardPileAddResult result = await CardPileCmd.Add(deckClone, PileType.Deck);
                            CardCmd.PreviewCardPileAdd(result, 1.2f, CardPreviewStyle.MessyLayout);
                            MainFile.Logger.Info($"Cloned KarenSunlight for combat shine exhaustion and added to deck. Clone ID: {deckClone.Id}");
                        }
                        else
                        {
                            MainFile.Logger.Error($"Failed to clone KarenSunlight deckVersion for combat shine exhaustion. Owner or RunState was null.");
                        }
                    }
                }
                else
                {
                    // 放弃选择，则直接丢弃那张牌
                    clone.RemoveFromState();
                }
            }
            else
            {
                 MainFile.Logger.Error($"Failed to clone KarenSunlight for combat shine exhaustion. Owner or RunState was null.");
            }
        }

    }


}
