using ShoujoKagekiAijoKaren.src.Core.Vfx;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using System.Linq;
using System.Collections.Generic;
using System;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public static class KarenPromiseVfxStarManager
{
    private static readonly SpireField<Player, int> TransitionDepth = new(() => 0);
    private static readonly SpireField<Player, NKarenPromiseStarNode?> Nodes = new(() => null);
    private sealed class EntranceCue(NCombatRoom room)
    {
        public readonly NCombatRoom Room = room;
        public float Age;
    }
    private static readonly SpireField<Player, EntranceCue?> Entrances = new(() => null);
    private static NKarenPromiseStarNode? GetOrCreateNode(Player player)
    {
        var creature = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        if (creature == null) return null;
        var node = Nodes.Get(player);
        if (GodotObject.IsInstanceValid(node) && !node!.IsQueuedForDeletion() && node.GetParent() == creature) return node;
        node = new NKarenPromiseStarNode();
        creature.AddChild(node);
        node.Init(creature);
        // Keep the tower above the room background but before all creature visuals.
        creature.MoveChild(node, 0);
        Nodes.Set(player, node);
        // Combat setup can clear/recreate the prop after the entrance Prefix.
        // Attach the same visual clock when normal pile setup creates its node.
        if (Entrances.Get(player) is { } cue && cue.Room == NCombatRoom.Instance)
            node.BeginCombatEntrance(cue.Age);
        return node;
    }
    internal static void BeginCombatEntrance(Player player)
    {
        if (NCombatRoom.Instance is not { } room) return;
        Entrances.Set(player, new EntranceCue(room));
        var node = Nodes.Get(player);
        if (GodotObject.IsInstanceValid(node) && !node!.IsQueuedForDeletion()) node.BeginCombatEntrance(0);
    }
    internal static void AdvanceCombatEntrance(Player player, float age)
    {
        if (Entrances.Get(player) is not { } cue || cue.Room != NCombatRoom.Instance) return;
        cue.Age = age;
        var node = Nodes.Get(player);
        if (GodotObject.IsInstanceValid(node) && !node!.IsQueuedForDeletion()) node.AdvanceCombatEntrance(age);
    }
    internal static void EndCombatEntrance(Player player)
    {
        Entrances.Set(player, null);
        var node = Nodes.Get(player);
        if (GodotObject.IsInstanceValid(node) && !node!.IsQueuedForDeletion()) node.EndCombatEntrance();
    }

    public static Vector2? TrackOutgoingCard(NCard card)
        => card.Model.Owner is { } player
            ? KarenVfxQuality.Enabled ? GetOrCreateNode(player)?.TrackOutgoingCard(card) : GetTowerPosition(player)
            : null;

    public static Vector2? GetTowerPosition(Player player)
    {
        if (KarenVfxQuality.Enabled) return GetOrCreateNode(player)?.TransferGlobalPosition;
        var room = NCombatRoom.Instance;
        if (IsTowerPile(player, PileType.Draw) && room?.Ui.DrawPile is { } draw)
            return draw.GlobalPosition + draw.Size * .5f;
        return room?.GetCreatureNode(player.Creature)?.VfxSpawnPosition;
    }

    internal static float GetTowerVisualAlpha(Player player)
    {
        // Particle shutdown can outlive the tower's cleanup. A visibility
        // query must not recreate a prop after ClearAll has removed it.
        var node = Nodes.Get(player);
        return GodotObject.IsInstanceValid(node) && !node!.IsQueuedForDeletion()
            ? node.TowerVisualAlpha : 0;
    }

    // All effects share the active entrance: the painted deck, or the local
    // Draw icon in Void. Convert between canvases only at the drawing boundary.
    public static Vector2? GetPileCanvasPosition(Player player)
        => KarenVfxQuality.Enabled ? GetOrCreateNode(player)?.TransferCanvasPosition : null;

    public static Vector2? GetPilePosition(Player player, CanvasItem surface)
        => GetPileCanvasPosition(player) is { } point
            ? surface.GetCanvasTransform().AffineInverse() * point : null;

    public static void StartVoidActivation(Player player)
        => GetOrCreateNode(player)?.StartVoidActivation();

    public static void PlayReproductionActivation(Player player)
        => GetOrCreateNode(player)?.PlayReproductionActivation();

    public static IDisposable? BeginTowerRecall(Player player, bool canDraw)
        => GetOrCreateNode(player)?.BeginTowerRecall(canDraw);

    public static IDisposable? BeginTransferResolution(CardModel card)
        => LocalContext.IsMe(card.Owner) && PromiseTransferPreviews.ForCard(card) != PromiseTransferPreview.None
            ? GetOrCreateNode(card.Owner)?.BeginTransferResolution(card) : null;

    public static void SetTransferPreview(CardModel card, bool selected)
    {
        var node = selected ? GetOrCreateNode(card.Owner) : Nodes.Get(card.Owner);
        if (GodotObject.IsInstanceValid(node)) node!.SetTransferPreview(card, selected);
    }

    public static void GuideIntoOrbit(CardModel card, Vector2 sourceCanvas, bool fromCard = false)
        => GetOrCreateNode(card.Owner)?.GuideIntoOrbit(card, sourceCanvas, fromCard);

    public static Vector2? GetStarPosition(CardModel card, CanvasItem surface)
        => KarenVfxQuality.Enabled && GetOrCreateNode(card.Owner) is { } node
            ? surface.GetCanvasTransform().AffineInverse() * node.GetStarCanvasPosition(card) : null;

    public static void BeginPileTransition(Player player)
        => TransitionDepth.Set(player, TransitionDepth.Get(player) + 1);

    public static void EndPileTransition(Player player)
    {
        TransitionDepth.Set(player, System.Math.Max(0, TransitionDepth.Get(player) - 1));
        UpdatePromisePileStarCount(player);
        // Run after the queued authoritative Sync so a slow power-hook batch
        // cannot drop the star transfer before Void is applied.
        var node = Nodes.Get(player);
        Callable.From(() => { if (GodotObject.IsInstanceValid(node)) node!.FinishVoidActivation(); }).CallDeferred();
    }

    public static void Replenish(Player player, IReadOnlyList<CardModel> cards)
    {
        if (cards.Count > 0) GetOrCreateNode(player)?.Replenish(cards);
    }

    public static bool IsTowerPile(Player player, PileType pileType)
        => pileType == KarenCustomEnum.PromisePile ||
           (pileType == PileType.Draw && PromisePileManager.IsVoidMode(player));

    public static void UpdatePromisePileStarCount(Player player)
    {
        var combat = player.PlayerCombatState;
        var room = NCombatRoom.Instance;
        // Read authoritative state when executed, never a stale captured count.
        Callable.From(() =>
        {
            if (TransitionDepth.Get(player) > 0 || combat == null || room == null || player.PlayerCombatState != combat || NCombatRoom.Instance != room) return;
            var power = player.Creature.GetPower<KarenPromisePilePower>();
            PromisePileMode mode = PromisePileMode.None;
            foreach (var flag in new[] { PromisePileMode.Void, PromisePileMode.InfiniteReinforcement, PromisePileMode.Burn, PromisePileMode.PastAndFuture })
                if (power?.IsInMode(flag) == true) mode |= flag;
            var cards = (power?.IsVoidMode == true ? PileType.Draw.GetPile(player) : PromisePileManager.GetPromisePile(player)).Cards.ToArray();
            GetOrCreateNode(player)?.Sync(cards, mode);
        }).CallDeferred();
    }
    public static void ClearAll(Player player)
    {
        // Old deferred cleanup must not erase the next combat's constellation.
        var node = Nodes.Get(player);
        if (GodotObject.IsInstanceValid(node)) { node!.ClearAll(); node.QueueFree(); }
        Nodes.Set(player, null);
    }
}
