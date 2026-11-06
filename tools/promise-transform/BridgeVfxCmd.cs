using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

// Temporary fixture; actual card plays and recordings are driven through MCP.
public sealed class BridgeVfxCmd : AbstractConsoleCmd
{
    private static BridgeVfxProbe? _probe;
    private static Task _preparation = Task.CompletedTask;
    public override string CmdName => "bridge_vfx";
    public override string Args => "prepare|void|upgrade|energy0|energy2|watch|status";
    public override string Description => "Prepare controlled sources and inspect actual bridge casts and native plays.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player?.PlayerCombatState == null || NCombatRoom.Instance == null || args.Length != 1) return new(false, Args);
        if (args[0] is "prepare" or "void")
        {
            _preparation = Prepare(player, args[0] == "void");
            return new(_preparation, true, "Preparing bridge candidates");
        }
        if (args[0] is "energy0" or "energy2")
            return new(PlayerCmd.SetEnergy(args[0] == "energy0" ? 0 : 2, player), true, "Set fixture energy");
        if (args[0] == "upgrade")
        {
            foreach (var card in PileType.Hand.GetPile(player).Cards.Where(c => c.Id.Entry == "KAREN_BRIDGE")) CardCmd.Upgrade(card);
            return new(true, "Upgraded bridge");
        }
        if (args[0] == "watch")
        {
            _probe?.Dispose();
            _probe = new BridgeVfxProbe(NCombatRoom.Instance);
            return new(true, "Watching bridge nodes on every frame");
        }
        if (args[0] != "status") return new(false, Args);
        var nodes = NCombatRoom.Instance.BackCombatVfxContainer.GetChildren().OfType<NKarenBridgeVfx>()
            .Where(n => !n.IsQueuedForDeletion()).ToArray();
        return new(true, JsonSerializer.Serialize(new {
            preparing = !_preparation.IsCompleted,
            nodeCount = nodes.Length,
            nodes = nodes.Select(n => new { age=n.Age, spot=n.IsSpotlight, hasCards=n.HasCards, fromDraw=n.FromDraw }),
            casts = _probe?.Casts ?? 0, spots = _probe?.Spots ?? 0, emptyCasts = _probe?.EmptyCasts ?? 0,
            drawCasts = _probe?.DrawCasts ?? 0,
            prematureExitFrames = _probe?.PrematureExitFrames ?? 0,
            lightingGapFrames = _probe?.LightingGapFrames ?? 0,
            heldLightFrames = _probe?.HeldLightFrames ?? 0,
            timeline = _probe?.Timeline,
            promise = PromisePileManager.GetPromisePile(player).Cards.Select(c => c.Id.Entry),
            draw = PileType.Draw.GetPile(player).Cards.Select(c => c.Id.Entry),
            discard = PileType.Discard.GetPile(player).Cards.Select(c => c.Id.Entry),
            started = CombatManager.Instance.History.CardPlaysStarted.Select(e => new { id=e.CardPlay.Card.Id.Entry, auto=e.CardPlay.IsAutoPlay }),
            finished = CombatManager.Instance.History.CardPlaysFinished.Select(e => new { id=e.CardPlay.Card.Id.Entry, auto=e.CardPlay.IsAutoPlay })
        }));
    }
    private static async Task Prepare(Player player, bool fromDraw)
    {
        if (fromDraw)
        {
            await PromisePileManager.EnterMode(player, PromisePileMode.Void);
            var old = PileType.Draw.GetPile(player).Cards.Concat(PileType.Discard.GetPile(player).Cards).ToArray();
            await CardPileCmd.Add(old, PileType.Exhaust);
        }
        else
            await CardPileCmd.Add(PromisePileManager.GetPromisePile(player).Cards.ToArray(), PileType.Discard);
        var combat = CombatManager.Instance.DebugOnlyGetState()!;
        string[] ids = ["KAREN_DEFEND", "KAREN_STRIKE", "KAREN_DEFEND"];
        var cards = ids.Select(id => combat.CreateCard(ModelDb.AllCards.Single(c => c.Id.Entry == id), player)).ToArray();
        if (fromDraw)
        {
            await CardPileCmd.AddGeneratedCardsToCombat(cards.Take(2), PileType.Draw, player);
            await CardPileCmd.AddGeneratedCardsToCombat(cards.Skip(2), PileType.Discard, player);
        }
        else await CardPileCmd.AddGeneratedCardsToCombat(cards, KarenCustomEnum.PromisePile, player);
    }
}

public sealed class BridgeVfxProbe : IDisposable
{
    private readonly HashSet<ulong> _seen = new();
    private readonly NCombatRoom _room;
    private readonly SceneTree _tree;
    private bool _disposed;
    private readonly bool _originalHoverBlock;
    public BridgeVfxProbe(NCombatRoom room)
    {
        _room = room;
        _tree = room.GetTree();
        // Keep incidental desktop-pointer hover tips out of the test recording.
        _originalHoverBlock = NHoverTipSet.shouldBlockHoverTips;
        NHoverTipSet.shouldBlockHoverTips = true;
        NHoverTipSet.Clear();
        _tree.ProcessFrame += Sample;
        room.TreeExiting += Dispose;
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        NHoverTipSet.shouldBlockHoverTips = _originalHoverBlock;
        if (GodotObject.IsInstanceValid(_tree)) _tree.ProcessFrame -= Sample;
        if (GodotObject.IsInstanceValid(_room)) _room.TreeExiting -= Dispose;
    }
    public int Casts { get; private set; }
    public int Spots { get; private set; }
    public int EmptyCasts { get; private set; }
    public int DrawCasts { get; private set; }
    public int PrematureExitFrames { get; private set; }
    public int LightingGapFrames { get; private set; }
    public int HeldLightFrames { get; private set; }
    public List<object> Timeline { get; } = new();
    private CardModel? _rootCard;
    private int _frames;
    private void Sample()
    {
        NHoverTipSet.shouldBlockHoverTips = true;
        NHoverTipSet.Clear();
        var nodes = _room.BackCombatVfxContainer.GetChildren().OfType<NKarenBridgeVfx>()
            .Where(n => !n.IsQueuedForDeletion()).ToArray();
        int stages = nodes.Count(n => !n.IsSpotlight && n.HasCards);
        if (stages > 0 && _rootCard == null)
            _rootCard = CombatManager.Instance.History.CardPlaysStarted.LastOrDefault(e => e.CardPlay.Card.Id.Entry == "KAREN_BRIDGE")?.CardPlay.Card;
        bool settled = _rootCard != null && CombatManager.Instance.History.CardPlaysFinished.Any(e => ReferenceEquals(e.CardPlay.Card, _rootCard));
        if (_rootCard != null && !settled && stages == 0) PrematureExitFrames++;
        foreach (var stage in nodes.Where(n => !n.IsSpotlight && n.HasCards && n.IsHolding && n.Age >= 0.7f))
        {
            if (stage.MainLightStrength < 0.95f) LightingGapFrames++;
            else HeldLightFrames++;
        }
        if (_rootCard != null && _frames++ % 3 == 0)
            Timeline.Add(new { frame = _frames, stages, settled,
                light = nodes.Where(n => !n.IsSpotlight).Select(n => n.MainLightStrength).DefaultIfEmpty().Max(),
                started = CombatManager.Instance.History.CardPlaysStarted.Count(),
                finished = CombatManager.Instance.History.CardPlaysFinished.Count() });
        foreach (var node in nodes)
        {
            if (!_seen.Add(node.GetInstanceId())) continue;
            if (node.IsSpotlight) Spots++;
            else
            {
                Casts++;
                if (!node.HasCards) EmptyCasts++;
                if (node.FromDraw) DrawCasts++;
            }
        }
    }
}
