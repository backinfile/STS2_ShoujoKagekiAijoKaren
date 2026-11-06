using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem.Nodes;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

// Observes real visual motion only. This assembly is never included in the Mod package.
[ModInitializer(nameof(Initialize))]
public static class ShineMotionProbe
{
    internal sealed class Deplete(NKarenShineDepleteVfx node)
    {
        public readonly NKarenShineDepleteVfx Node = node;
        public readonly float Duration = (float)(Field(node, "_duration") ?? 0f), Gather = (float)Field(node, "_gatherDuration")!;
        public readonly bool HasFlight = Field(node, "_headPosition") is Vector2;
        public readonly bool Fuel = (bool)Field(node, "_fuel")!;
        public float? LandingError;
        public float MinCardScale = 1, GatherDisplacement;
        public float? CompletionAge;
        public bool Released, FaceReappeared, Arrived;
        public float ArrivalDelay = float.MaxValue;
        public readonly List<object> Samples = new();
    }
    internal static readonly List<Deplete> Depletes = new();
    internal sealed class Arrival(NKarenShinePileArrivalVfx node)
    {
        public readonly NKarenShinePileArrivalVfx Node = node;
        public int Count;
        public float MaxScale = 1, MinScale = 1;
        public float MinRotation, MaxRotation;
        public readonly float Duration = (float)Field(node,"_duration")!;
    }
    internal static readonly List<Arrival> Arrivals = new();
    private static readonly List<object> CountChanges = new();
    private static int _initialCount, _lastCount, _earlyCountFrames, _heldCountFrames;
    private static ulong _countStart;
    internal static NTopBarShinePileButton? Button => NRun.Instance?.GlobalUi.TopBar.GetNodeOrNull<NTopBarShinePileButton>("RightAlignedStuff/ShinePile");

    private static (int Model, int Display, int Target, int Pending, bool Animating, float Scale) ReadCount()
    {
        var button = Button!;
        var tween = Field(button,"_bumpTween") as Tween;
        return ((int)Field(button,"_modelCount")!,int.Parse(button.GetNode<Label>("ShinePileCardCount").Text),
            (int)Field(button,"_countTarget")!,((IEnumerable)Field(button,"_inFlight")!).Cast<object>().Count(),
            tween?.IsRunning()==true,button.GetNode<Label>("ShinePileCardCount").Scale.X);
    }
    internal static object CountStatus()
    {
        var c = ReadCount();
        return new {model=c.Model,displayed=c.Display,target=c.Target,pending=c.Pending,animating=c.Animating,scale=c.Scale,
            earlyFrames=_earlyCountFrames,heldFrames=_heldCountFrames,changes=CountChanges};
    }
    internal static bool Watching;
    internal static int DetachedSparkFrames, MaxSparks, FloorFrames;
    internal static float MaxRigidDifference, MaxFloorY, MaxFloorOffset, ViewportHeight;
    static readonly Dictionary<object, (Vector2 Position, Transform2D Pose)> Previous = new();
    internal static object? Field(object o,string field) => o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Public)?.GetValue(o);
    public static void Initialize() => ((SceneTree)Engine.GetMainLoop()).ProcessFrame += () => Callable.From(Sample).CallDeferred();
    static void Sample()
    {
        if (!Watching || NCombatRoom.Instance is not {} room || NRun.Instance is not {} run) return;
        foreach (var node in run.GlobalUi.GetChildren().OfType<NKarenShineDepleteVfx>()) {
            var seen = Depletes.FirstOrDefault(s => s.Node == node);
            if (seen == null) {
                seen = new(node); Depletes.Add(seen);
                var observed = seen;
                node.TreeExiting += () => {
                    observed.CompletionAge = (float)Field(node,"_age")!;
                    observed.Released = (bool)Field(node,"_released")!;
                };
            }
            float age = (float)Field(node,"_age")!;
            var head = Field(node,"_headPosition") is Vector2 flightHead ? flightHead : ((Transform2D)Field(node,"_source")!).Origin;
            var target = (Control)Field(node,"_target")!;
            var card = (NCard)Field(node,"_card")!;
            bool released = (bool)Field(node,"_released")!;
            if ((bool)Field(node,"_arrived")!) {
                seen.Arrived = true;
                seen.ArrivalDelay = Math.Min(seen.ArrivalDelay,age-seen.Gather-seen.Duration);
            }
            seen.Released |= released;
            if (released && seen.HasFlight) seen.LandingError = Math.Min(seen.LandingError ?? float.MaxValue,head.DistanceTo(target.GetGlobalTransformWithCanvas()*(target.Size*.5f)));
            if (!released && GodotObject.IsInstanceValid(card)) {
                if (seen.Fuel && card.Visible) seen.FaceReappeared = true;
                var source = (Transform2D)Field(node,"_source")!;
                seen.MinCardScale = Math.Min(seen.MinCardScale,card.GetGlobalTransformWithCanvas().Scale.Length()/source.Scale.Length());
                if(age <= seen.Gather)
                    seen.GatherDisplacement = Math.Max(seen.GatherDisplacement,card.GetGlobalTransformWithCanvas().Origin.DistanceTo(source.Origin));
            }
            if (age <= seen.Gather+seen.Duration+.02f)
                seen.Samples.Add(new { age, x=head.X, y=head.Y, released });
        }
        var button = run.GlobalUi.TopBar.GetNodeOrNull<NTopBarShinePileButton>("RightAlignedStuff/ShinePile");
        if (button != null)
            foreach (var node in button.GetChildren().OfType<NKarenShinePileArrivalVfx>()) {
                var seen = Arrivals.FirstOrDefault(s=>s.Node==node);
                if (seen == null) { seen = new(node); Arrivals.Add(seen); }
                seen.Count = Math.Max(seen.Count,(int)Field(node,"_arrivals")!);
                var visual = button.GetNode<Control>("Control");
                var scale = visual.Scale/(Vector2)Field(node,"_baseScale")!;
                seen.MaxScale = Math.Max(seen.MaxScale,Math.Max(scale.X,scale.Y));
                seen.MinScale = Math.Min(seen.MinScale,Math.Min(scale.X,scale.Y));
                float rotation = visual.Rotation-(float)Field(node,"_baseRotation")!;
                seen.MinRotation = Math.Min(seen.MinRotation,rotation);
                seen.MaxRotation = Math.Max(seen.MaxRotation,rotation);
            }
        if (button != null)
        {
            var c = ReadCount();
            bool flying = Depletes.Any(d=>GodotObject.IsInstanceValid(d.Node) && !d.Arrived && !(bool)Field(d.Node,"_released")!);
            // A gather is freed on completion, so its final frame may be absent.
            // Compare the actual displayed count with unresolved native tickets.
            if (flying && c.Display > c.Model-c.Pending) _earlyCountFrames++;
            if (c.Pending>0 && c.Model>c.Display) _heldCountFrames++;
            if (c.Display != _lastCount)
            {
                CountChanges.Add(new {time=(Time.GetTicksMsec()-_countStart)/1000.0,count=c.Display,model=c.Model,pending=c.Pending,arrivals=Depletes.Count(d=>d.Arrived)});
                _lastCount = c.Display;
            }
        }
        var alive = new HashSet<object>();
        foreach (var warning in ShineFuelProbe.Descend(run).OfType<Node2D>()
            .Where(w=>w is NKarenShineWarningVfx or NKarenShineAmbientVfx).Where(w=>w.Visible && w.IsVisibleInTree())) {
            var sparks = ((IEnumerable)Field(warning,"_sparks")!).Cast<object>().ToArray();
            MaxSparks=Math.Max(MaxSparks,sparks.Length);
            var pose=warning.GetGlobalTransformWithCanvas();
            foreach(var spark in sparks) {
                alive.Add(spark);var point=(Vector2)Field(spark,"Position")!;
                if(Previous.TryGetValue(spark,out var old) && pose.Origin.DistanceTo(old.Pose.Origin)>3) {
                    float difference=point.DistanceTo(pose*(old.Pose.AffineInverse()*old.Position));
                    if(difference>.5f)DetachedSparkFrames++;
                    MaxRigidDifference=Math.Max(MaxRigidDifference,difference);
                }
                Previous[spark]=(point,pose);
            }
        }
        foreach(var key in Previous.Keys.Where(k=>!alive.Contains(k)).ToArray())Previous.Remove(key);
        foreach(var cue in room.CombatVfxContainer.GetChildren().OfType<NKarenStageCueVfx>().Where(c=>c.Kind==NKarenStageCueVfx.CueKind.Act2Cast)) {
            var method=typeof(NKarenStageCueVfx).GetMethod("CastGroundPoint",BindingFlags.NonPublic|BindingFlags.Instance)!;
            var foot=(Vector2)Field(cue,"_foot")!;ViewportHeight=cue.GetViewport().GetVisibleRect().Size.Y;
            foreach(bool front in new[]{false,true})for(int i=0;i<(front?5:7);i++){
                var point=(Vector2)method.Invoke(cue,new object[]{front?135f+i*252:44f+i*198,front})!;
                MaxFloorY=Math.Max(MaxFloorY,point.Y);MaxFloorOffset=Math.Max(MaxFloorOffset,Math.Abs(point.Y-foot.Y));
            }
            FloorFrames++;
        }
    }
    internal static void Reset() {
        Watching=true;Depletes.Clear();Arrivals.Clear();Previous.Clear();DetachedSparkFrames=MaxSparks=FloorFrames=0;MaxRigidDifference=MaxFloorY=MaxFloorOffset=ViewportHeight=0;
        CountChanges.Clear();_initialCount=ReadCount().Display;_lastCount=int.MinValue;_earlyCountFrames=_heldCountFrames=0;_countStart=Time.GetTicksMsec();
    }
    internal static object Status()=>new {
        detachedSparkFrames=DetachedSparkFrames,maxRigidDifference=MaxRigidDifference,maxSparks=MaxSparks,
        floorFrames=FloorFrames,maxFloorY=MaxFloorY,maxFloorOffset=MaxFloorOffset,viewportHeight=ViewportHeight,
        liveDepletes=NRun.Instance?.GlobalUi.GetChildren().OfType<NKarenShineDepleteVfx>().Count()??0,
        liveArrivals = NRun.Instance?.GlobalUi.TopBar.GetNodeOrNull("RightAlignedStuff/ShinePile")?.GetChildren().OfType<NKarenShinePileArrivalVfx>().Count()??0,
        pileVisualRestoreError = NRun.Instance?.GlobalUi.TopBar.GetNodeOrNull<Control>("RightAlignedStuff/ShinePile/Control") is {} visual
            ? (visual.Scale-Vector2.One).Length()+visual.PivotOffset.Length()+Math.Abs(visual.Rotation) : -1,
        pileIconScale = NRun.Instance?.GlobalUi.TopBar.GetNodeOrNull<Control>("RightAlignedStuff/ShinePile/Control/Icon") is {} icon
            ? new[]{icon.Scale.X,icon.Scale.Y} : new float[0],
        pileIconRotation = NRun.Instance?.GlobalUi.TopBar.GetNodeOrNull<Control>("RightAlignedStuff/ShinePile/Control/Icon")?.Rotation ?? -1,
        counter=CountStatus(),
        arrivals = Arrivals.Select(s=>new {count=s.Count,duration=s.Duration,maxScale=s.MaxScale,minScale=s.MinScale,minRotation=s.MinRotation,maxRotation=s.MaxRotation}),
        depletes=Depletes.Select(s=>new {duration=s.Duration,gather=s.Gather,fuel=s.Fuel,hasFlight=s.HasFlight,landingError=s.LandingError,minCardScale=s.MinCardScale,gatherDisplacement=s.GatherDisplacement,completionAge=s.CompletionAge,released=s.Released,faceReappeared=s.FaceReappeared,arrived=s.HasFlight ? (bool?)s.Arrived : null,arrivalDelay=s.HasFlight ? (float?)s.ArrivalDelay : null,samples=s.Samples})
    };
}
public sealed class ShineMotionProbeCommand:AbstractConsoleCmd
{
    private static NKarenShineDepleteVfx? _countTicket;
    public override string CmdName=>"shine_motion";
    public override string Args=>"watch|status|arrival|focus|unfocus|count_add N|count_clear|count_init|count_hold|count_cancel";
    public override string Description=>"Observe independent embers, fixed gather timing and scene-ground endpoints.";
    public override bool IsNetworked=>false;
    public override CmdResult Process(Player? player,string[] args) {
        if(args.Length==2 && args[0]=="count_add" && player!=null) {
            var model=ModelDb.AllCards.Single(c=>c.Id.Entry=="KAREN_SHINE_STRIKE");
            for(int i=0;i<int.Parse(args[1]);i++)
                ShinePileManager.AddToShinePileInternal(player,CombatManager.Instance.DebugOnlyGetState()!.CreateCard(model,player));
            return new(true,"Added count fixture cards without flight");
        }
        if(args.Length!=1)return new(false,Args);
        if(args[0]=="count_clear" && player!=null) {
            var pile=ShinePileManager.GetShinePile(player);
            foreach(var card in pile.Cards.ToArray()) pile.RemoveInternal(card);
            return new(true,"Cleared count fixture pile");
        }
        if(args[0]=="count_init" && player!=null) { ShineMotionProbe.Button!.Initialize(player);return new(true,"Rebound count snapshot"); }
        if(args[0]=="count_hold") {
            if(_countTicket!=null)return new(false,"Count ticket already held");
            _countTicket=new NKarenShineDepleteVfx();ShineMotionProbe.Button!.BeginDepletion(_countTicket);return new(true,"Held visual count ticket");
        }
        if(args[0]=="count_cancel") {
            if(_countTicket==null)return new(false,"No count ticket");
            ShineMotionProbe.Button!.FinishDepletion(_countTicket,false);_countTicket.Free();_countTicket=null;return new(true,"Cancelled visual count ticket");
        }
        if(args[0]=="watch"){ShineMotionProbe.Reset();return new(true,"watch");}
        if(args[0] is "arrival" or "focus" or "unfocus") {
            var button = NRun.Instance!.GlobalUi.TopBar.GetNode<NTopBarShinePileButton>("RightAlignedStuff/ShinePile");
            if(args[0]=="arrival") button.PlayDepletionArrival();
            else button.EmitSignal(args[0]=="focus" ? Control.SignalName.MouseEntered : Control.SignalName.MouseExited);
            return new(true,"visual control");
        }
        return new(true,JsonSerializer.Serialize(ShineMotionProbe.Status()));
    }
}
