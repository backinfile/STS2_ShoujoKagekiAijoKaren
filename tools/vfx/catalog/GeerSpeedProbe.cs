using System;
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
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

// Temporary isolated observer. Rules are driven by actual MCP card/turn/selection actions.
[ModInitializer(nameof(Initialize))]
public static class GeerSpeedProbe
{
    private sealed class Seen(NKarenGeerVfx node, double start)
    {
        public readonly NKarenGeerVfx Node = node;
        public readonly bool FullScreen = node.FullScreen;
        public readonly bool OwnerCast = (bool)Field(node, "_castOnOwner")!;
        public readonly double Started = start;
        public double Ended;
        public int Frames, VisibleInstantFrames, SoundInstantFrames;
        public float MaxAge;
        public bool Exited, ParentCorrect;
    }
    private static readonly List<Seen> Nodes = new();
    private static readonly List<AudioStreamPlayer> Sounds = new();
    private static Player? _player;
    private static NCombatRoom? _room;
    private static bool _watch;
    private static double _start, _phaseAt, _powerAt = -1, _selectionAt = -1, _switchedAt = -1;
    private static float _switchAge = -1;
    private static int _peak, _selectionPeak, _selectionFrames;
    private static Vector2 _offset, _position, _visualPosition;
    private static int _z;
    private static float _offsetError, _positionError, _visualError;
    private static int _zError;
    private static object? Field(object o, string name) => o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(o);
    private static double Now => Time.GetTicksMsec() / 1000.0 - _start;
    private static NKarenGeerVfx[] Live(NCombatRoom room, NCreature? owner) => room.BackCombatVfxContainer.GetChildren().OfType<NKarenGeerVfx>()
        .Concat(owner?.GetChildren().OfType<NKarenGeerVfx>() ?? []).Where(n => !n.IsQueuedForDeletion()).ToArray();
    public static void Initialize() => ((SceneTree)Engine.GetMainLoop()).ProcessFrame += () => Callable.From(Sample).CallDeferred();
    internal static void Watch(Player p)
    {
        _player=p; _room=NCombatRoom.Instance; _watch=true; _start=Time.GetTicksMsec()/1000.0;
        _phaseAt=0; _powerAt=_selectionAt=_switchedAt=-1; _switchAge=-1; _peak=_selectionPeak=_selectionFrames=0;
        _offsetError=_positionError=_visualError=0; _zError=0; Nodes.Clear(); Sounds.Clear();
        var owner=_room!.GetCreatureNode(p.Creature)!;
        _position=owner.Position; _visualPosition=owner.Visuals.Position;
        _offset=(owner.Body as Sprite2D)?.Offset ?? Vector2.Zero; _z=(owner.Body as Sprite2D)?.ZIndex ?? 0;
    }
    private static void Sample()
    {
        if (!_watch || _player==null || _room==null || NCombatRoom.Instance!=_room) return;
        var owner=_room.GetCreatureNode(_player.Creature);
        var live=Live(_room,owner); _peak=Math.Max(_peak,live.Length);
        if (_powerAt<0 && _player.Creature.GetPowerAmount<KarenGeerPower>()>0) _powerAt=Now;
        if (owner!=null)
        {
            _positionError=Math.Max(_positionError,owner.Position.DistanceTo(_position));
            _visualError=Math.Max(_visualError,owner.Visuals.Position.DistanceTo(_visualPosition));
            if (owner.Body is Sprite2D body) { _offsetError=Math.Max(_offsetError,body.Offset.DistanceTo(_offset)); _zError=Math.Max(_zError,Math.Abs(body.ZIndex-_z)); }
        }
        bool instant=SaveManager.Instance.PrefsSave.FastMode==FastModeType.Instant;
        foreach (var node in live)
        {
            var seen=Nodes.FirstOrDefault(n=>n.Node==node);
            if (seen==null)
            {
                seen=new(node,Now); Nodes.Add(seen); var captured=seen;
                node.TreeExiting+=()=>{captured.Exited=true;captured.Ended=Now;};
            }
            seen.Frames++; seen.MaxAge=Math.Max(seen.MaxAge,node.Age);
            seen.ParentCorrect|=seen.FullScreen ? node.GetParent()==_room.BackCombatVfxContainer : node.GetParent()==owner && node.ShowBehindParent;
            if (instant && node.IsVisibleInTree()) seen.VisibleInstantFrames++;
            if (Field(node,"_engageSound") is AudioStreamPlayer sound && GodotObject.IsInstanceValid(sound))
            {
                if (!Sounds.Contains(sound)) Sounds.Add(sound);
                if (instant && sound.Playing) seen.SoundInstantFrames++;
            }
        }
        if (NOverlayStack.Instance?.Peek() is NSimpleCardSelectScreen)
        {
            if (_selectionAt<0) _selectionAt=Now;
            _selectionPeak=Math.Max(_selectionPeak,live.Count(n=>n.IsVisibleInTree())); _selectionFrames++;
        }
        if (_switchAge>=0 && live.Any(n=>n.Age>=_switchAge))
        {
            SaveManager.Instance.PrefsSave.FastMode=FastModeType.Instant; _switchedAt=Now; _switchAge=-1;
        }
    }
    internal static void Switch(float at) => _switchAge=at;
    internal static void Phase() { _phaseAt=Now; _selectionAt=-1; _selectionPeak=0; _selectionFrames=0; }
    internal static void Cancel()
    {
        if (NCombatRoom.Instance is not {} room || _player==null) return;
        foreach(var node in Live(room,room.GetCreatureNode(_player.Creature))) node.QueueFree();
    }
    internal static object Status(Player p) => new {
        nativeMode=SaveManager.Instance.PrefsSave.FastMode.ToString(), now=Now, phaseAt=_phaseAt, powerAt=_powerAt,
        selectionAt=_selectionAt, switchAt=_switchedAt, peak=_peak, selectionPeak=_selectionPeak, selectionFrames=_selectionFrames,
        live=Nodes.Count(n=>GodotObject.IsInstanceValid(n.Node)&&n.Node.IsInsideTree()&&!n.Node.IsQueuedForDeletion()),
        activeSounds=Sounds.Count(n=>GodotObject.IsInstanceValid(n)&&n.Playing),
        power=p.Creature.GetPowerAmount<KarenGeerPower>(),sourceCount=PromisePileManager.GetCount(p),
        offsetError=_offsetError,positionError=_positionError,visualError=_visualError,zError=_zError,
        nodes=Nodes.Select(n=>new {fullScreen=n.FullScreen,ownerCast=n.OwnerCast,started=n.Started,ended=n.Ended,
            duration=n.Ended-n.Started,frames=n.Frames,age=n.MaxAge,exited=n.Exited,parentCorrect=n.ParentCorrect,
            instantFrames=n.VisibleInstantFrames,instantSoundFrames=n.SoundInstantFrames})
    };
    internal static async Task Seed(Player p, int casts, bool empty)
    {
        var combat=CombatManager.Instance.DebugOnlyGetState()!;
        var all=new[]{PileType.Hand,PileType.Draw,PileType.Discard,PileType.Exhaust,KarenCustomEnum.PromisePile}.SelectMany(t=>t.GetPile(p).Cards).Distinct().ToArray();
        await CardPileCmd.RemoveFromCombat(all);
        p.Creature.SetCurrentHpInternal(p.Creature.MaxHp);
        bool isVoid=PromisePileManager.IsVoidMode(p);
        var strike=ModelDb.AllCards.Single(c=>c.Id.Entry=="KAREN_STRIKE");
        var defend=ModelDb.AllCards.Single(c=>c.Id.Entry=="KAREN_DEFEND");
        var geer=ModelDb.AllCards.Single(c=>c.Id.Entry=="KAREN_GEER");
        for(int i=0;i<(isVoid && !empty ? 8 : 5);i++) await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard(isVoid && !empty ? strike:defend,p),PileType.Draw,p);
        if(!isVoid && !empty) for(int i=0;i<3;i++) await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard(strike,p),KarenCustomEnum.PromisePile,p);
        for(int i=0;i<casts;i++) await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard(geer,p),PileType.Hand,p);
    }
}
public sealed class GeerSpeedCmd : AbstractConsoleCmd
{
    public override string CmdName=>"geer_speed";
    public override string Args=>"watch|status|phase|switch AGE|cancel|deferred|seed CASTS [empty]";
    public override string Description=>"Isolated Geer timing fixtures and observation.";
    public override bool IsNetworked=>false;
    public override CmdResult Process(Player? p,string[] args)
    {
        if(p?.PlayerCombatState==null || args.Length==0)return new(false,Args);
        switch(args[0])
        {
            case "watch":GeerSpeedProbe.Watch(p);return new(true,"watching");
            case "status":return new(true,JsonSerializer.Serialize(GeerSpeedProbe.Status(p)));
            case "phase":GeerSpeedProbe.Phase();return new(true,"turn");
            case "switch" when args.Length==2&&float.TryParse(args[1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float age):GeerSpeedProbe.Switch(age);return new(true,"scheduled preference change");
            case "cancel":GeerSpeedProbe.Cancel();return new(true,"VFX-only interruption");
            case "deferred":KarenGeerVfxManager.PlayCast(p);SaveManager.Instance.PrefsSave.FastMode=FastModeType.Instant;return new(true,"queued before Instant preference");
            case "seed" when args.Length>=2&&int.TryParse(args[1],out int n):return new(GeerSpeedProbe.Seed(p,n,args.Length==3&&args[2]=="empty"),true,"seeding isolated cards");
        }
        return new(false,Args);
    }
}
