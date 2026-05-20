using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;

/// <summary>
/// 世界上最空虚的人 - 摧毁抽牌堆，然后以约定牌堆代替抽牌堆
/// </summary>
public sealed class KarenVoid : KarenBaseCardModel
{
    public KarenVoid() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override HashSet<CardTag> CanonicalTags => [KarenCustomEnum.PromisePileRelated];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.PlayerCombatState;
        if (combatState == null) return;

        if (PromisePileManager.IsInMode(Owner, PromisePileMode.Void))
        {
            MainFile.Logger.Info($"KarenVoid: Already in Void mode. No action taken.");
            return;
        }

        // 只播放一次统一的消耗表现，然后静默把整副抽牌堆直接移出战斗。
        // 这里故意不走 CardCmd / CardPileCmd，避免触发任何逐张消耗或移牌扳机。
        MainFile.Logger.Info($"KarenVoid: Exhausting all cards in draw pile. Count: {combatState.DrawPile.Cards.Count}");
        var drawPileCards = combatState.DrawPile.Cards.ToList();
        if (drawPileCards.Count > 0)
        {
            PlayDrawPileExhaustVfx(drawPileCards[0]);
            await Cmd.Wait(0.15f);

            var drawPile = combatState.DrawPile;
            foreach (var card in drawPileCards)
            {
                card.RemoveFromCurrentPile(silent: true);
                card.HasBeenRemovedFromState = true;
            }

            drawPile.InvokeContentsChanged();
            PromisePileManager.SetPileCountLabel(drawPile.Cards.Count);
        }

        // 取出约定牌堆中的所有牌，然后重新放入抽牌堆
        await CardPileCmd.Add(PromisePileManager.GetPromisePile(Owner).Cards.ToList(), PileType.Draw);

        // 切换模式
        await PromisePileCmd.EnterMode(Owner, PromisePileMode.Void);
        PromisePileManager.SetPileCountLabel(combatState.DrawPile.Cards.Count);
    }

    private static void PlayDrawPileExhaustVfx(CardModel visualCard)
    {
        var ui = NCombatRoom.Instance?.Ui;
        if (ui == null) return;

        var cardNode = NCard.Create(visualCard);
        if (cardNode == null) return;

        ui.AddChildSafely(cardNode);
        cardNode.UpdateVisuals(PileType.Draw, CardPreviewMode.Normal);
        cardNode.GlobalPosition = PileType.Draw.GetTargetPosition(cardNode) - cardNode.Size * 0.5f;
        cardNode.Visible = false;

        var exhaustVfx = NExhaustVfx.Create(cardNode);
        if (exhaustVfx != null)
            ui.AddChildSafely(exhaustVfx);

        TaskHelper.RunSafely(FreeVisualCardNodeLater(cardNode));
    }

    private static async Task FreeVisualCardNodeLater(NCard cardNode)
    {
        await Task.Delay(2000);
        if (Godot.GodotObject.IsInstanceValid(cardNode))
            cardNode.QueueFreeSafely();
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
