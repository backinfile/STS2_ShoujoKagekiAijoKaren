using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers.tmpStrength;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem.Nodes;

// Temporary development fixture. It observes real card plays; stress/restart are
// explicitly visual-only lifecycle checks and do not simulate gameplay success.
public sealed class PastStreamCmd : AbstractConsoleCmd
{
    private static PastStreamProbe? _probe;
    private static Task _operation = Task.CompletedTask;
    public override string CmdName => "past_stream";
    public override string Args => "seed <count>|watch|status|stress|restart|stop";
    public override string Description => "Seed inventory or inspect past-ring continuity and shine carriers.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player?.PlayerCombatState == null || args.Length == 0) return new(false, Args);
        if (args[0] == "seed")
        {
            _operation = Seed(player, Math.Clamp(int.Parse(args[1]), 0, 30));
            return new(_operation, true, "Seeded real cards");
        }
        if (args[0] == "watch") { _probe?.Dispose(); _probe = new(player); }
        if (args[0] == "stop") _probe?.Dispose();
        if (args[0] is "stress" or "restart")
        {
            _operation = Lifecycle(player, args[0] == "restart");
            return new(_operation, true, "Visual-only lifecycle scenario");
        }
        return new(true, JsonSerializer.Serialize(new {
            operating = !_operation.IsCompleted,
            strength = player.Creature.GetPower<KarenPastAndFutureTempStrengthPower>()?.Amount ?? 0,
            promise = PromisePileManager.GetPromisePile(player).Cards.Count,
            draw = player.PlayerCombatState.DrawPile.Cards.Count,
            hand = player.PlayerCombatState.Hand.Cards.Count,
            state = PastStreamProbe.State(player), stats = _probe?.Result()
        }));
    }
    private static async Task Seed(Player player, int count)
    {
        var pile = PromisePileManager.IsVoidMode(player) ? player.PlayerCombatState!.DrawPile : PromisePileManager.GetPromisePile(player);
        await CardPileCmd.Add(pile.Cards.ToArray(), PileType.Exhaust);
        var combat = CombatManager.Instance.DebugOnlyGetState()!;
        var model = ModelDb.AllCards.Single(c => c.Id.Entry == "KAREN_DEFEND");
        var cards = Enumerable.Range(0, count).Select(_ => combat.CreateCard(model, player)).ToArray();
        await CardPileCmd.AddGeneratedCardsToCombat(cards, PromisePileManager.IsVoidMode(player) ? PileType.Draw : KarenCustomEnum.PromisePile, player);
    }
    private static async Task Lifecycle(Player player, bool restart)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        if (restart)
        {
            KarenPastAndFutureRingVfxManager.Pulse(player);
            await tree.ToSignal(tree.CreateTimer(.25), SceneTreeTimer.SignalName.Timeout);
            KarenPastAndFutureRingVfxManager.Stop(player);
            await tree.ToSignal(tree.CreateTimer(.08), SceneTreeTimer.SignalName.Timeout);
            KarenPastAndFutureRingVfxManager.Start(player);
            await tree.ToSignal(tree.CreateTimer(.05), SceneTreeTimer.SignalName.Timeout);
            KarenPastAndFutureRingVfxManager.Pulse(player);
        }
        else for (int i = 0; i < 100; i++)
        {
            KarenPastAndFutureRingVfxManager.Pulse(player);
            await tree.ToSignal(tree.CreateTimer(.02), SceneTreeTimer.SignalName.Timeout);
        }
    }
}

public sealed class PastStreamProbe : IDisposable
{
    private readonly Player _player;
    private readonly NCombatRoom _room;
    private readonly SceneTree _tree;
    private bool _disposed;
    private double _time;
    private int _maxActive, _maxScheduled, _maxNodes, _visibleDepleteFrames;
    private int _visibleDepleteFlightFrames, _gatherIntermediateFrames;
    private float _maxGatherPositionError, _gatherMinAlpha = 1, _gatherMaxAlpha;
    private float _maxOriginError, _maxFrontError, _maxScaleError;
    private float _minAmbientVisibility = 1f;
    private double _lastActive;
    private readonly List<object> _timeline = new();
    public PastStreamProbe(Player player)
    {
        _player = player; _room = NCombatRoom.Instance!; _tree = _room.GetTree();
        _tree.ProcessFrame += Sample; _room.TreeExiting += Dispose;
    }
    public void Dispose()
    {
        if (_disposed) return; _disposed = true;
        _tree.ProcessFrame -= Sample;
        if (GodotObject.IsInstanceValid(_room)) _room.TreeExiting -= Dispose;
    }
    private static object? Field(object obj, string name) => obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)?.GetValue(obj);
    private static float Number(object obj, string name) => Convert.ToSingle(Field(obj, name) ?? 0);
    private static NKarenPastAndFutureRingVfx[] Rings() => ShineFuelProbe.Descend(NRun.Instance).OfType<NKarenPastAndFutureRingVfx>().ToArray();
    private static int[] Counts(NKarenPastAndFutureRingVfx node)
    {
        if (Field(node, "_travelling") is IEnumerable travelling)
        {
            var values = travelling.Cast<object>().ToArray();
            return [values.Count(r => Number(r, "Age") > 0 && Number(r, "Age") < Number(r, "Duration")), values.Length];
        }
        // Baseline binary, before continuity optimization.
        var sequences = ((IEnumerable)Field(node, "_sequences")!).Cast<float>().ToArray();
        float[] starts = [0, .16f, .53f, .69f, .88f, 1.09f, 1.24f, 1.46f];
        float[] durations = [1.15f, .88f, 1.83f, .91f, 1.36f, .83f, 1.22f, 1.24f];
        return [sequences.Sum(t => starts.Select((s, i) => t * 1.5f > s && t * 1.5f < s + durations[i] ? 1 : 0).Sum()), sequences.Length * 8];
    }
    public static object State(Player player)
    {
        var nodes = Rings();
        return new { nodes = nodes.Length,
            fronts = ShineFuelProbe.Descend(NRun.Instance).Count(n => n.Name == "PastAndFutureRingFront"),
            active = nodes.Sum(n => Counts(n)[0]), scheduled = nodes.Sum(n => Counts(n)[1]),
            streaming = nodes.Any(n => Number(n, "_streamRemaining") > 0),
            lights = ShineFuelProbe.Descend(NRun.Instance).OfType<NKarenPromiseStarNode>().Count(),
            depletes = ShineFuelProbe.Descend(NRun.Instance).OfType<NKarenShineDepleteVfx>().Count()
        };
    }
    private void Sample()
    {
        _time += _room.GetProcessDeltaTime();
        var nodes = Rings(); int active = 0, scheduled = 0;
        var creature = _room.GetCreatureNode(_player.Creature)!;
        foreach (var node in nodes)
        {
            var count = Counts(node); active += count[0]; scheduled += count[1];
            if (count[0] > 0) _minAmbientVisibility = Math.Min(_minAmbientVisibility, Number(node, "_ambientVisibility"));
            _maxOriginError = Math.Max(_maxOriginError, node.GlobalPosition.DistanceTo(creature.VfxSpawnPosition));
            if (Field(node, "_front") is Node2D front)
                _maxFrontError = Math.Max(_maxFrontError, front.GlobalPosition.DistanceTo(node.GlobalPosition));
            var visual = creature.Visuals.GlobalTransform; var actual = node.GlobalTransform;
            _maxScaleError = Math.Max(_maxScaleError, new Vector2(visual.X.Length(), visual.Y.Length()).DistanceTo(new Vector2(actual.X.Length(), actual.Y.Length())));
        }
        foreach (var node in ShineFuelProbe.Descend(NRun.Instance).OfType<NKarenShineDepleteVfx>())
        {
            if (Field(node, "_card") is not MegaCrit.Sts2.Core.Nodes.Cards.NCard card || !GodotObject.IsInstanceValid(card)
                || (bool)Field(node, "_released")!) continue;
            bool gathering = Number(node, "_age") < Number(node, "_gatherDuration");
            if (card.Visible)
            {
                _visibleDepleteFrames++;
                if (!gathering) _visibleDepleteFlightFrames++;
                else
                {
                    _gatherMinAlpha = Math.Min(_gatherMinAlpha, card.Modulate.A);
                    _gatherMaxAlpha = Math.Max(_gatherMaxAlpha, card.Modulate.A);
                    if (card.Modulate.A is > .02f and < .98f) _gatherIntermediateFrames++;
                    var source = (Transform2D)Field(node, "_source")!;
                    _maxGatherPositionError = Math.Max(_maxGatherPositionError,
                        card.GetGlobalTransformWithCanvas().Origin.DistanceTo(source.Origin));
                }
            }
        }
        _maxActive = Math.Max(_maxActive, active); _maxScheduled = Math.Max(_maxScheduled, scheduled);
        _maxNodes = Math.Max(_maxNodes, nodes.Length); if (active > 0) _lastActive = _time;
        _timeline.Add(new { t = Math.Round(_time, 3), active, scheduled,
            strength = _player.Creature.GetPower<KarenPastAndFutureTempStrengthPower>()?.Amount ?? 0 });
    }
    public object Result() => new { maxActive = _maxActive, maxScheduled = _maxScheduled, maxNodes = _maxNodes,
        visibleDepleteFrames = _visibleDepleteFrames, maxOriginError = _maxOriginError, maxFrontError = _maxFrontError,
        visibleDepleteFlightFrames = _visibleDepleteFlightFrames, gatherIntermediateFrames = _gatherIntermediateFrames,
        maxGatherPositionError = _maxGatherPositionError, gatherMinAlpha = _gatherMinAlpha, gatherMaxAlpha = _gatherMaxAlpha,
        maxScaleError = _maxScaleError, minAmbientVisibility = _minAmbientVisibility, lastActive = _lastActive, timeline = _timeline };
}
