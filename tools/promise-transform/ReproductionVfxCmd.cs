using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

// Temporary fixture: actual activations, drawing and attacks use native MCP play_card.
public sealed class ReproductionVfxCmd : AbstractConsoleCmd
{
    private static ReproductionProbe? _probe;
    private static Task _refill = Task.CompletedTask;
    public override string CmdName => "reproduction_vfx";
    public override string Args => "watch|status|off|void|refill <0-10>";
    public override string Description => "Observe actual reproduction and refill presentation on the player's tower.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        var room = NCombatRoom.Instance;
        if (player?.PlayerCombatState == null || room == null || args.Length < 1) return new(false, Args);
        if (args[0] == "refill" && args.Length == 2 && int.TryParse(args[1], out int count)
            && count is >= 0 and <= 10 && _refill.IsCompleted
            && player.Creature.GetPower<KarenPromisePilePower>()?.IsInfiniteReinforcement == true)
        {
            _refill = Refill(player, count);
            return new(_refill, true, "Remove real source cards and let the normal refill generate replacements");
        }
        if (args.Length != 1) return new(false, Args);
        if (args[0] == "watch")
        {
            _probe?.Dispose();
            _probe = new ReproductionProbe(player, room);
            return new(true, "Watching the owner's existing tower");
        }
        if (args[0] == "off")
        {
            player.Creature.GetPower<KarenPromisePilePower>()?.ExitMode(PromisePileMode.InfiniteReinforcement);
            KarenPromiseVfxStarManager.UpdatePromisePileStarCount(player);
            return new(true, "Mode removed for fade/reentry check");
        }
        if (args[0] == "void") return new(PromisePileManager.EnterMode(player, PromisePileMode.Void), true, "Enter Void");
        if (args[0] != "status") return new(false, Args);
        var creature = room.GetCreatureNode(player.Creature);
        var towers = creature?.GetChildren().OfType<NKarenPromiseStarNode>().Where(n => !n.IsQueuedForDeletion()).ToArray() ?? [];
        return new(true, JsonSerializer.Serialize(new
        {
            towerCount = towers.Length,
            reproductionTargets = towers.Select(n => ReproductionProbe.Target(n, room.Ui.DrawPile.GetNode<TextureRect>("Icon"))),
            refilling = !_refill.IsCompleted,
            flights = towers.SelectMany(ReproductionProbe.Flights).Select(flight => new
            {
                card = ReproductionProbe.FlightValue<CardModel>(flight, "Card").Id.Entry,
                startedAt = ReproductionProbe.FlightValue<float>(flight, "StartedAt"),
                duration = ReproductionProbe.FlightValue<float>(flight, "Duration"),
                launched = ReproductionProbe.FlightValue<bool>(flight, "Launched")
            }),
            voidOrbitLayers = room.Ui.DrawPile.GetNode<TextureRect>("Icon").GetChildren()
                .Count(n => n.Name.ToString().StartsWith("KarenVoidOrbit", StringComparison.Ordinal)),
            towers = towers.Select(n => new { id = n.GetInstanceId(), children = n.GetChildCount(),
                entranceAge = ReproductionProbe.Value<float>(n, "_reproductionAge"),
                clock = ReproductionProbe.Value<float>(n, "_time"),
                visibility = ReproductionProbe.Value<float>(n, "_reproductionVisibility"),
                active = ReproductionProbe.Value<bool>(n, "_reproductionActive") }),
            mode = player.Creature.GetPower<KarenPromisePilePower>()?.IsInfiniteReinforcement == true,
            voidMode = PromisePileManager.IsVoidMode(player),
            promise = PromisePileManager.GetPromisePile(player).Cards.Select(c => c.Id.Entry),
            hand = player.PlayerCombatState.Hand.Cards.Select(c => c.Id.Entry),
            draw = PileType.Draw.GetPile(player).Cards.Select(c => c.Id.Entry),
            started = CombatManager.Instance.History.CardPlaysStarted.Select(e => e.CardPlay.Card.Id.Entry),
            finished = CombatManager.Instance.History.CardPlaysFinished.Select(e => e.CardPlay.Card.Id.Entry),
            entrances = _probe?.Entrances ?? 0,
            generated = _probe?.Generated.Count ?? 0,
            scheduled = _probe?.Scheduled.Count ?? 0,
            shown = _probe?.Shown.Count ?? 0,
            arrived = _probe?.Arrived.Count ?? 0,
            generatedMatchesScheduled = _probe?.Generated.SetEquals(_probe.Scheduled) ?? true,
            scheduledMatchesShown = _probe?.Scheduled.SetEquals(_probe.Shown) ?? true,
            shownMatchesArrived = _probe?.Shown.SetEquals(_probe.Arrived) ?? true,
            maxFlights = _probe?.MaxFlights ?? 0,
            maxTowers = _probe?.MaxTowers ?? 0, maxChildren = _probe?.MaxChildren ?? 0,
            timeline = _probe?.Timeline
        }));
    }
    private static async Task Refill(Player player, int count)
    {
        var pile = PromisePileManager.IsVoidMode(player) ? PileType.Draw.GetPile(player) : PromisePileManager.GetPromisePile(player);
        await CardPileCmd.Add(pile.Cards.Take(count).ToArray(), PileType.Discard);
        await PromisePileManager.UpdatePowerAsync(player);
    }
}

public sealed class ReproductionProbe : IDisposable
{
    private readonly Player _player;
    private readonly NCombatRoom _room;
    private readonly SceneTree _tree;
    private readonly bool _hoverBlocked;
    private float _lastEntrance = 100;
    private readonly HashSet<CardModel> _initial = new();
    private int _frames;
    private bool _disposed;
    public int Entrances, MaxTowers, MaxChildren, MaxFlights;
    public HashSet<CardModel> Generated { get; } = new();
    public HashSet<CardModel> Scheduled { get; } = new();
    public HashSet<CardModel> Shown { get; } = new();
    public HashSet<CardModel> Arrived { get; } = new();
    public List<object> Timeline { get; } = new();
    public static T Value<T>(NKarenPromiseStarNode node, string field)
        => (T)typeof(NKarenPromiseStarNode).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(node)!;
    private static T Property<T>(NKarenPromiseStarNode node, string name)
        => (T)typeof(NKarenPromiseStarNode).GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(node)!;
    public static object Target(NKarenPromiseStarNode node, TextureRect icon)
    {
        var gate = node.GetGlobalTransformWithCanvas() * Property<Vector2>(node, "ReproductionGate");
        var iconCenter = icon.GetGlobalTransformWithCanvas() * (icon.Size * 0.5f);
        float surge = Property<float>(node, "ReproductionSurgeVisibility") * Value<float>(node, "_reproductionVisibility");
        return new { kind = Property<bool>(node, "UsingVoidIconOrbit") ? "draw_pile" : "tower",
            x = gate.X, y = gate.Y, iconDistance = gate.DistanceTo(iconCenter), surge,
            slots = surge > 0 ? (int)typeof(NKarenPromiseStarNode).GetField("ReproductionSurgeSlots", BindingFlags.Static | BindingFlags.NonPublic)!.GetRawConstantValue()! : 0 };
    }
    public static IEnumerable<object> Flights(NKarenPromiseStarNode node)
        => Value<IEnumerable>(node, "_reproductionFlights").Cast<object>();
    public static T FlightValue<T>(object flight, string field)
        => (T)flight.GetType().GetField(field)!.GetValue(flight)!;
    private IEnumerable<CardModel> Source => PromisePileManager.IsVoidMode(_player)
        ? PileType.Draw.GetPile(_player).Cards : PromisePileManager.GetPromisePile(_player).Cards;
    public ReproductionProbe(Player player, NCombatRoom room)
    {
        _player = player; _room = room; _tree = room.GetTree();
        _initial.UnionWith(Source);
        var tower = room.GetCreatureNode(player.Creature)?.GetChildren().OfType<NKarenPromiseStarNode>().FirstOrDefault();
        if (tower != null) _lastEntrance = Value<float>(tower, "_reproductionAge");
        _hoverBlocked = NHoverTipSet.shouldBlockHoverTips;
        _tree.ProcessFrame += Sample;
        room.TreeExiting += Dispose;
    }
    private void Sample()
    {
        NHoverTipSet.shouldBlockHoverTips = true;
        NHoverTipSet.Clear();
        var towers = _room.GetCreatureNode(_player.Creature)?.GetChildren().OfType<NKarenPromiseStarNode>().Where(n => !n.IsQueuedForDeletion()).ToArray() ?? [];
        MaxTowers = Math.Max(MaxTowers, towers.Length);
        if (towers.Length != 1) return;
        var node = towers[0];
        float entrance = Value<float>(node, "_reproductionAge"), now = Value<float>(node, "_time");
        if (entrance < _lastEntrance - 0.1f) Entrances++;
        _lastEntrance = entrance;
        MaxChildren = Math.Max(MaxChildren, node.GetChildCount());
        Generated.UnionWith(Source.Where(card => card.Id.Entry == "KAREN_CONTINUE" && !_initial.Contains(card)));
        var flights = Flights(node).ToArray();
        MaxFlights = Math.Max(MaxFlights, flights.Length);
        foreach (var flight in flights)
        {
            var card = FlightValue<CardModel>(flight, "Card");
            Scheduled.Add(card);
            float start = FlightValue<float>(flight, "StartedAt"), duration = FlightValue<float>(flight, "Duration");
            if (now >= start) Shown.Add(card);
            if (now >= start + duration) Arrived.Add(card);
        }
        if (_frames++ % 6 == 0) Timeline.Add(new { frame = _frames, entrance, now,
            visibility = Value<float>(node, "_reproductionVisibility"), active = Value<bool>(node, "_reproductionActive"),
            generated = Generated.Count, scheduled = Scheduled.Count, shown = Shown.Count, arrived = Arrived.Count,
            flights = flights.Length, count = Source.Count() });
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        NHoverTipSet.shouldBlockHoverTips = _hoverBlocked;
        if (GodotObject.IsInstanceValid(_tree)) _tree.ProcessFrame -= Sample;
        if (GodotObject.IsInstanceValid(_room)) _room.TreeExiting -= Dispose;
    }
}
