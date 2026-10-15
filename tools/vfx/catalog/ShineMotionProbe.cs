using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem.Nodes;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

// Observes real visual motion only. This assembly is never included in the Mod package.
[ModInitializer(nameof(Initialize))]
public static class ShineMotionProbe
{
    internal sealed class Deplete(NKarenShineDepleteVfx node)
    {
        public readonly NKarenShineDepleteVfx Node = node;
        public readonly float Duration = (float)Field(node, "_duration")!, Gather = (float)Field(node, "_gatherDuration")!;
        public readonly bool Fuel = (bool)Field(node, "_fuel")!;
        public float LandingError = float.MaxValue, MinCardScale = 1;
        public bool Released, FaceReappeared;
        public readonly List<object> Samples = new();
    }
    internal static readonly List<Deplete> Depletes = new();
    internal static bool Watching;
    internal static int DetachedSparkFrames, MaxSparks, FloorFrames;
    internal static float MaxRigidDifference, MaxFloorY, MaxFloorOffset, ViewportHeight;
    static readonly Dictionary<object, (Vector2 Position, Transform2D Pose)> Previous = new();
    internal static object? Field(object o,string field) => o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Public)!.GetValue(o);
    public static void Initialize() => ((SceneTree)Engine.GetMainLoop()).ProcessFrame += () => Callable.From(Sample).CallDeferred();
    static void Sample()
    {
        if (!Watching || NCombatRoom.Instance is not {} room || NRun.Instance is not {} run) return;
        foreach (var node in run.GlobalUi.GetChildren().OfType<NKarenShineDepleteVfx>()) {
            var seen = Depletes.FirstOrDefault(s => s.Node == node);
            if (seen == null) { seen = new(node); Depletes.Add(seen); }
            float age = (float)Field(node,"_age")!;
            var head = (Vector2)Field(node,"_headPosition")!;
            var target = (Control)Field(node,"_target")!;
            var card = (NCard)Field(node,"_card")!;
            bool released = (bool)Field(node,"_released")!;
            seen.Released |= released;
            if (released) seen.LandingError = Math.Min(seen.LandingError,head.DistanceTo(target.GetGlobalTransformWithCanvas()*(target.Size*.5f)));
            if (!released && GodotObject.IsInstanceValid(card)) {
                if (seen.Fuel && card.Visible) seen.FaceReappeared = true;
                var source = (Transform2D)Field(node,"_source")!;
                seen.MinCardScale = Math.Min(seen.MinCardScale,card.GetGlobalTransformWithCanvas().Scale.Length()/source.Scale.Length());
            }
            if (age <= seen.Gather+seen.Duration+.02f)
                seen.Samples.Add(new { age, x=head.X, y=head.Y, released });
        }
        var alive = new HashSet<object>();
        foreach (var warning in ShineFuelProbe.Descend(run).OfType<NKarenShineWarningVfx>().Where(w=>w.Visible && w.IsVisibleInTree())) {
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
    internal static void Reset() { Watching=true;Depletes.Clear();Previous.Clear();DetachedSparkFrames=MaxSparks=FloorFrames=0;MaxRigidDifference=MaxFloorY=MaxFloorOffset=ViewportHeight=0; }
    internal static object Status()=>new {
        detachedSparkFrames=DetachedSparkFrames,maxRigidDifference=MaxRigidDifference,maxSparks=MaxSparks,
        floorFrames=FloorFrames,maxFloorY=MaxFloorY,maxFloorOffset=MaxFloorOffset,viewportHeight=ViewportHeight,
        liveDepletes=NRun.Instance?.GlobalUi.GetChildren().OfType<NKarenShineDepleteVfx>().Count()??0,
        depletes=Depletes.Select(s=>new {duration=s.Duration,gather=s.Gather,fuel=s.Fuel,landingError=s.LandingError,minCardScale=s.MinCardScale,released=s.Released,faceReappeared=s.FaceReappeared,samples=s.Samples})
    };
}
public sealed class ShineMotionProbeCommand:AbstractConsoleCmd
{
    public override string CmdName=>"shine_motion";
    public override string Args=>"watch|status";
    public override string Description=>"Observe independent embers, flight timing and scene-ground endpoints.";
    public override bool IsNetworked=>false;
    public override CmdResult Process(Player? player,string[] args) {
        if(args.Length!=1)return new(false,Args);
        if(args[0]=="watch"){ShineMotionProbe.Reset();return new(true,"watch");}
        return new(true,JsonSerializer.Serialize(ShineMotionProbe.Status()));
    }
}
