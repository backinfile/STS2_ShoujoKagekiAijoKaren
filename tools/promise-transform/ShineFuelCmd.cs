using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem.Nodes;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

public sealed class ShineFuelCmd : AbstractConsoleCmd
{
    private static ShineFuelProbe? _probe;
    public override string CmdName => "shine_fuel";
    public override string Args => "set <id> <shine> [noexhaust|eternal]|watch|status|stop";
    public override string Description => "Disposable fixture and per-frame VFX observer.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player?.PlayerCombatState == null || args.Length == 0) return new(false, Args);
        if (args[0] == "set")
        {
            var card = player.PlayerCombatState.Hand.Cards.Last(c => c.Id.Entry == args[1]);
            card.SetShineCurrent(int.Parse(args[2]));
            if (args.Length > 3 && args[3] == "noexhaust") card.RemoveKeyword(CardKeyword.Exhaust);
            if (args.Length > 3 && args[3] == "eternal") card.AddKeyword(CardKeyword.Eternal);
            NCard.FindOnTable(card)?.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
            return new(true, "Configured instance only");
        }
        if (args[0] == "watch")
        {
            _probe?.Dispose(); _probe = new ShineFuelProbe(player); return new(true, "Watching");
        }
        if (args[0] == "return-fuel")
        {
            var card = player.PlayerCombatState.DiscardPile.Cards.Last(c=>c.Id.Entry=="KAREN_DROP_FUEL");
            return new(CardPileCmd.Add(card, PileType.Hand), true, "Return the same non-exhausted instance");
        }
        if (args[0] == "stop") _probe?.Dispose();
        return new(true, JsonSerializer.Serialize(new {
            hand = player.PlayerCombatState.Hand.Cards.Select(c => new { id=c.Id.Entry, shine=c.GetShineValue(),
                willWarn=ShineVfx.WillDeplete(c), burn=KarenPromisePilePower.HasBurnDrawEffect(c), keywords=c.Keywords.Select(k=>k.ToString()),
                nodeVisible=NCard.FindOnTable(c)?.Visible, nodeAlpha=NCard.FindOnTable(c)?.Modulate.A,
                bodyVisible=NCard.FindOnTable(c)?.Body.Visible,
                bodyAlpha=NCard.FindOnTable(c)?.Body.Modulate.A, selfAlpha=NCard.FindOnTable(c)?.Body.SelfModulate.A }),
            shineCount=ShinePileManager.GetShinePileCount(player),
            exhaust=player.PlayerCombatState.ExhaustPile.Cards.Select(c=>new { id=c.Id.Entry, shine=c.GetShineValue() }),
            discard=player.PlayerCombatState.DiscardPile.Cards.Select(c=>new { id=c.Id.Entry, shine=c.GetShineValue() }),
            nativeExhaustNodes=ShineFuelProbe.Descend(NRun.Instance).Count(n=>n.GetType().Name is "NExhaustVfx" or "NCardExhaustVfx" or "NCardExhaustQuickVfx"),
            warningNodes=ShineFuelProbe.Descend(NRun.Instance).OfType<NKarenShineWarningVfx>().Count(n=>n.Visible && n.IsVisibleInTree()),
            stats=_probe?.Result()
        }));
    }
}

public sealed class ShineFuelProbe : IDisposable
{
    private readonly Player _player;
    private readonly SceneTree _tree;
    private bool _disposed;
    private double _time;
    private readonly HashSet<ulong> _fuel = new(), _bursts = new(), _deplete = new(), _native = new();
    private readonly List<object> _timeline = new();
    private readonly Dictionary<ulong,float> _endErrors = new();
    private int _visibleBurstFrames, _hiddenBurstFrames, _remaining, _maxWarning;
    private double _nextSample;
    public ShineFuelProbe(Player player)
    {
        _player=player; _tree=(SceneTree)Engine.GetMainLoop(); _tree.ProcessFrame+=Sample;
    }
    public void Dispose() { if (_disposed) return; _disposed=true; _tree.ProcessFrame-=Sample; }
    public static IEnumerable<Node> Descend(Node? n)
    {
        if (n == null || !GodotObject.IsInstanceValid(n)) yield break;
        yield return n;
        foreach(var child in n.GetChildren()) foreach(var c in Descend(child)) yield return c;
    }
    private static T Field<T>(object obj, string name) => (T)obj.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(obj)!;
    private void Sample()
    {
        if (NRun.Instance == null) return;
        _time+=NRun.Instance.GetProcessDeltaTime();
        var nodes=Descend(NRun.Instance).ToArray();
        var fuels=nodes.OfType<NKarenDropFuelVfx>().ToArray();
        var depletes=nodes.OfType<NKarenShineDepleteVfx>().ToArray();
        int warning=nodes.OfType<NKarenShineWarningVfx>().Count(n=>n.Visible && n.IsVisibleInTree());
        _maxWarning=Math.Max(_maxWarning, warning);
        foreach(var f in fuels)
        {
            _fuel.Add(f.GetInstanceId());
            if(Field<float>(f,"_burstAt")>=0)
            {
                _bursts.Add(f.GetInstanceId());
                var card=Field<NCard>(f,"_card");
                var model=Field<CardModel>(f,"_model");
                if(GodotObject.IsInstanceValid(card) && card.Model==model && KarenFuelCardVisuals.IsBurst(card))
                {
                    if(card.Visible) _visibleBurstFrames++; else _hiddenBurstFrames++;
                }
            }
        }
        foreach(var d in depletes)
        {
            _deplete.Add(d.GetInstanceId());
            var trail=Field<List<Vector2>>(d,"_trail");
            var target=Field<Control>(d,"_target");
            if(trail.Count>0 && GodotObject.IsInstanceValid(target))
                _endErrors[d.GetInstanceId()]=trail[^1].DistanceTo(target.GetGlobalTransformWithCanvas()*(target.Size*.5f));
        }
        foreach(var n in nodes.Where(n=>n.GetType().Name is "NExhaustVfx" or "NCardExhaustVfx")) _native.Add(n.GetInstanceId());
        _remaining=fuels.Length+depletes.Length;
        if(_time>=_nextSample)
        {
            _nextSample=_time+.08;
            _timeline.Add(new { t=Math.Round(_time,3), fuel=fuels.Length, deplete=depletes.Length, warning, energy=_player.PlayerCombatState?.Energy,
                visiblePlay=nodes.OfType<NCard>().Count(n=>n.Visible && n.Model?.Pile?.Type==PileType.Play) });
        }
    }
    public object Result() => new { fuel=_fuel.Count, bursts=_bursts.Count, deplete=_deplete.Count, nativeExhaust=_native.Count,
        visibleBurstFrames=_visibleBurstFrames, hiddenBurstFrames=_hiddenBurstFrames, remaining=_remaining, maxWarning=_maxWarning,
        endErrors=_endErrors.Values, timeline=_timeline };
}
