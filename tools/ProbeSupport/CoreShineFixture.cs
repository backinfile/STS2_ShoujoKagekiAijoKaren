using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using ShoujoKageki.Core.V1;

/// <summary>Test-only archive preparation. Assertions and real resolution use Core V1.
/// Fixture mutations must be completed before recording starts.</summary>
internal static class CoreShineFixture
{
    private static readonly ConditionalWeakTable<Player, CardPile> Previews = new();
    private static Type ArchiveType => CoreApi.Get<IShineService>().GetType().Assembly.GetType("ShoujoKageki.Core.Runtime.Modules.Shine.ShineArchive", true)!;
    internal static int Count(Player player) => Shine.GetDepletedCards(player).Count;
    internal static CardPile Pile(Player player)
    {
        var pile = Previews.GetValue(player, _ => new CardPile(PileType.None));
        pile.Clear();
        foreach (var card in Shine.GetDepletedModels(player)) pile.AddInternal(card);
        return pile;
    }
    internal static void Archive(Player player, CardModel card) => ArchiveType.GetMethod("Add", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { player, card });
    internal static void Clear(Player player) => Restore(player, Array.Empty<SerializableCard>());
    internal static void Sync(Player player)
    {
        if (Previews.TryGetValue(player, out var pile)) Restore(player, pile.Cards.Select(card => card.ToSerializable()).ToArray());
    }
    internal static void Restore(Player player, SerializableCard[] cards) => ArchiveType.GetMethod("Restore", BindingFlags.Static | BindingFlags.Public)!.Invoke(null, new object[] { player, cards });
    internal static Task Deplete(CardModel card, PlayerChoiceContext context)
    {
        Shine.DepleteAfterCurrentResolution(card);
        return Shine.ResolveAsync(card, () => Task.CompletedTask, CardResolutionKind.Arrow);
    }
}
