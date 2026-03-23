using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
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

    public override async Task OnShineExhausted(PlayerChoiceContext ctx)
    {
        if (Owner == null)
        {
            MainFile.Logger.Error("KarenSunlight.OnShineExhausted called but Owner is null.");
            return;
        }

        CardModel clone = this.CloneSafeForDeck();
        clone.RestoreShineToMax();

        var selected = await CardSelectCmdEx.FromChooseACardScreen(ctx, [clone], base.Owner, canSkip: true, new LocString("gameplay_ui", "KAREN_SUNLIGHT_OBTAIN_PROMPT"));
        if (selected == null)
        {
            clone.RemoveFromState();
            MainFile.Logger.Info("Player chose to skip adding KarenSunlight back to deck after shine exhaustion.");
            return;
        }

        // 需要将这张牌重新放入手牌
        await CardPileCmd.Add(this, PileType.Hand);
        { // 然后创建一个牌组中的牌的复制重新加入牌组
            var deckClone = Owner?.RunState?.CloneCard(clone);
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
}
