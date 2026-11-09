using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using ShoujoKageki.Core.V1;
using System;
using System.Threading.Tasks;
namespace ShoujoKagekiAijoKaren.src.Core.GlobalMoveSystem.Patches;

/// <summary>Collapses the native Play transit into Karen's semantic pile-to-pile movement.</summary>
internal static class GlobalMovePatch
{
    private static readonly SpireField<CardModel, PileType?> InPlay = new(() => null);
    private static IDisposable? _subscription;
    internal static void Initialize() => _subscription ??= CoreApi.Get<ICardMovementPipeline>().Register(Handle);
    private static Task Handle(CardMovement movement)
    {
        var card = movement.Card; var oldPile = movement.OldPile; var next = movement.NewPile;
        if (card.IsCanonical) return Task.CompletedTask;
        if (next is PileType.None or PileType.Deck) { InPlay.Set(card, null); return Task.CompletedTask; }
        if (next == PileType.Play) { InPlay.Set(card, oldPile); return Task.CompletedTask; }
        if (oldPile == PileType.Play)
        {
            if (InPlay.Get(card) is not { } origin) return Task.CompletedTask;
            oldPile = origin; InPlay.Set(card, null);
        }
        return card is ShoujoKagekiAijoKaren.src.Core.Models.Cards.KarenBaseCardModel karenCard
            ? karenCard.OnGlobalMove(oldPile, next, movement.Source) : Task.CompletedTask;
    }
}
