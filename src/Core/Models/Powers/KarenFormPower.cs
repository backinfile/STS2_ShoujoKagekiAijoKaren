using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using System.Linq;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// Power类 - 觉醒形态：每个回合开始时，抽满手牌
/// </summary>
public class KarenFormPower : PowerModel
{
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerType Type => PowerType.Buff;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner.Player is { } player) KarenFormVfxManager.Start(player);
        CombatBgmReplacementManager.PlayLoop(KarenFormMusicManager.FileName, Owner.Player, 0.5f);
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        if (oldOwner.Player is { } player) KarenFormVfxManager.Stop(player);
        CombatBgmReplacementManager.Stop(oldOwner.Player);
        return Task.CompletedTask;
    }

    public override decimal ModifyHandDrawLate(Player player, decimal count)
    {
        if (player == Owner?.Player)
        {
            return CardPile.MaxCardsInHand;
        }
        return base.ModifyHandDrawLate(player, count);
    }

    public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        // Only real start-of-turn draws pulse the wind; previews, full-hand attempts
        // and draws caused by played cards must not trigger it.
        if (fromHandDraw && card.Owner == Owner.Player)
            KarenFormVfxManager.PulseDraw(card.Owner);
        return Task.CompletedTask;
    }
}
