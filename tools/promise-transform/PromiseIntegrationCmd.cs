using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

// Disposable probe: observes real MCP card plays; never ships in the Karen package.
public sealed class PromiseIntegrationCmd : AbstractConsoleCmd
{
    private static IntegrationWatch? _watch;
    public override string CmdName => "promise_integration";
    public override string Args => "watch|status|off|shine1";
    public override string Description => "Inspect existing VFX interaction with the painted tower and Void HUD.";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? player, string[] args)
    {
        var room = NCombatRoom.Instance;
        if (player == null || room == null || args.Length != 1) return new(false, Args);
        if (args[0] == "shine1")
        {
            var card = PileType.Hand.GetPile(player).Cards.First(c => c.Id.Entry == "KAREN_SHINE_STRIKE");
            card.SetShineCurrent(1);
            return new(true, "Fixture sets one remaining Shine; exhaust through an actual play.");
        }
        if (args[0] is "watch" or "off")
        {
            _watch?.Dispose();
            _watch = args[0] == "watch" ? new IntegrationWatch(player, room) : null;
            return new(true, args[0]);
        }
        if (args[0] != "status") return new(false, Args);
        return new(true, JsonSerializer.Serialize((_watch ?? new IntegrationWatch(player, room, false)).Status()));
    }
}

internal sealed class IntegrationWatch : IDisposable
{
    private readonly Player _player;
    private readonly NCombatRoom _room;
    private readonly SceneTree _tree;
    private readonly HashSet<CardModel> _entries = new(), _flights = new();
    private bool _disposed;
    private int _frames, _bridgeFrames, _mismatches;
    private float _anchorError, _bridgeError;
    private static object? Field(object instance, string name) => instance.GetType()
        .GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance);
    private NKarenPromiseStarNode Tower => _room.GetCreatureNode(_player.Creature)!
        .GetChildren().OfType<NKarenPromiseStarNode>().Single();
    private object[] Stars => ((IDictionary)Field(Tower, "_stars")!).Values.Cast<object>().ToArray();
    private CardModel[] Cards => (PromisePileManager.IsVoidMode(_player)
        ? PileType.Draw.GetPile(_player) : PromisePileManager.GetPromisePile(_player)).Cards.ToArray();

    public IntegrationWatch(Player player, NCombatRoom room, bool observe = true)
    {
        _player = player; _room = room; _tree = room.GetTree();
        if (observe) { _tree.ProcessFrame += Sample; room.TreeExiting += Dispose; }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (GodotObject.IsInstanceValid(_tree)) _tree.ProcessFrame -= Sample;
        if (GodotObject.IsInstanceValid(_room)) _room.TreeExiting -= Dispose;
    }
    private float AnchorError()
    {
        var tower = Tower;
        Vector2 expected;
        if (PromisePileManager.IsVoidMode(_player))
        {
            var icon = _room.Ui.DrawPile.GetNode<Control>("Icon");
            expected = icon.GetGlobalTransformWithCanvas() * (icon.Size * 0.5f);
        }
        else
        {
            const float deckY = -15 + 170 + (18 - 363) * 1.5f + (718 - 19) * 0.3475f;
            expected = tower.GetGlobalTransformWithCanvas() * new Vector2(-95, deckY);
        }
        return expected.DistanceTo(tower.TransferCanvasPosition);
    }
    private void Sample()
    {
        _frames++;
        _anchorError = Math.Max(_anchorError, AnchorError());
        foreach (var star in Stars)
        {
            var model = (CardModel)Field(star, "Card")!;
            if (Field(star, "Entry") != null) _entries.Add(model);
            if (Field(star, "Flight") is NCard flight && GodotObject.IsInstanceValid(flight))
            {
                _flights.Add(model);
                if (flight.Model != model) _mismatches++;
            }
        }
        foreach (var bridge in _room.BackCombatVfxContainer.GetChildren().OfType<NKarenBridgeVfx>()
            .Where(n => !n.IsSpotlight && !n.IsQueuedForDeletion()))
        {
            _bridgeFrames++;
            var source = bridge.GetGlobalTransformWithCanvas() * (Vector2)Field(bridge, "_source")!;
            _bridgeError = Math.Max(_bridgeError, source.DistanceTo(Tower.TransferCanvasPosition));
        }
    }
    public object Status()
    {
        var stars = Stars;
        var present = stars.Where(s => (bool)Field(s, "Present")!).ToArray();
        var cards = Cards;
        return new {
            frames = _frames, bridgeFrames = _bridgeFrames, maxBridgeSourceError = _bridgeError,
            maxAnchorError = _anchorError, anchorError = AnchorError(), flightModelMismatchFrames = _mismatches,
            seenEntries = _entries.Select(c => c.Id.Entry).ToArray(), seenFlights = _flights.Select(c => c.Id.Entry).ToArray(),
            voidMode = PromisePileManager.IsVoidMode(_player),
            modelCount = cards.Length, starCount = present.Length,
            identitiesMatch = cards.ToHashSet().SetEquals(present.Select(s => (CardModel)Field(s,"Card")!)),
            entryCount = stars.Count(s => Field(s, "Entry") != null),
            flightCount = stars.Count(s => Field(s, "Flight") != null),
            promise = PromisePileManager.GetPromisePile(_player).Cards.Select(c => c.Id.Entry).ToArray(),
            draw = PileType.Draw.GetPile(_player).Cards.Select(c => c.Id.Entry).ToArray(),
            hand = PileType.Hand.GetPile(_player).Cards.Select(c => c.Id.Entry).ToArray(),
            shine = ShinePileManager.GetShinePile(_player).Cards.Select(c => c.Id.Entry).ToArray(),
            discard = PileType.Discard.GetPile(_player).Cards.Select(c => c.Id.Entry).ToArray()
        };
    }
}
