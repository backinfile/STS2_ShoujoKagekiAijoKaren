using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
public sealed class FirstUseCmd : AbstractConsoleCmd {
 public override string CmdName=>"karen_first_use";public override string Args=>"";public override string Description=>"Measure first vs repeated real VFX";public override bool IsNetworked=>false;
 internal static string Result="idle";
 public override CmdResult Process(Player? p,string[] args){if(p==null)return new(false,"Player required");Result="running";return new(Run(p,args.Length==0?"cold":args[0]),true,"running");}
 static readonly List<Resource> WarmResources=new();
 static double ReadyMs;
 static void ReadyPrefix(out long __state){__state=Stopwatch.GetTimestamp();}
 static void ReadyPostfix(long __state){ReadyMs=(Stopwatch.GetTimestamp()-__state)*1000d/Stopwatch.Frequency;}
 static async Task Run(Player player,string mode){
  try {var room=NCombatRoom.Instance!;var tree=room.GetTree();var output=new List<object>();
   new Harmony("karen.first-use.probe").Patch(AccessTools.Method(typeof(NKarenBridgeVfx),"_Ready"),prefix:new HarmonyMethod(typeof(FirstUseCmd),nameof(ReadyPrefix)),postfix:new HarmonyMethod(typeof(FirstUseCmd),nameof(ReadyPostfix)));
   if(mode=="preload"){
    foreach(var path in new[]{"res://materials/vfx/bridge_atmosphere.gdshader","res://materials/vfx/bridge_reveal.gdshader","res://materials/vfx/bridge_light.gdshader","res://images/vfx/bridge/stage-v12.png"})if(ResourceLoader.Exists(path))WarmResources.Add(ResourceLoader.Load(path));
    ShoujoKagekiAijoKaren.src.Core.Audio.KarenAudioManager.GetDuration("karen_bridge_slide.wav");
    ShoujoKagekiAijoKaren.src.Core.Audio.KarenAudioManager.GetDuration("karen_bridge_lock.wav");
   }
   if(mode=="draw"){
    var warm=NKarenBridgeVfx.Play(player,true);await room.ToSignal(tree.CreateTimer(1.5),SceneTreeTimer.SignalName.Timeout);warm.Dispose();
    await room.ToSignal(tree.CreateTimer(.7),SceneTreeTimer.SignalName.Timeout);
   }
   foreach(string effect in new[]{"bridge"}){
    for(int pass=0;pass<4;pass++){
     await room.ToSignal(tree.CreateTimer(.5),SceneTreeTimer.SignalName.Timeout);
     var samples=new List<double>();long last=Stopwatch.GetTimestamp();
     void Tick(){long now=Stopwatch.GetTimestamp();samples.Add((now-last)*1000d/Stopwatch.Frequency);last=now;}
     ReadyMs=0;tree.ProcessFrame+=Tick;long rendered=Engine.GetFramesDrawn();long alloc=GC.GetTotalAllocatedBytes();long started=Stopwatch.GetTimestamp();
     IDisposable? playback=null;
     Node? node=effect=="bridge"?null:effect=="nonon"?NKarenNononVfx.Create(player):NKarenShineBarrageVfx.Hit(player.Creature,player.Creature.CombatState!.GetOpponentsOf(player.Creature).First(),0);
     if(effect=="bridge")playback=NKarenBridgeVfx.Play(player,true);else {if(node==null)throw new Exception("No real VFX created");room.CombatVfxContainer.AddChild(node);}
     double buildMs=(Stopwatch.GetTimestamp()-started)*1000d/Stopwatch.Frequency;long allocated=GC.GetTotalAllocatedBytes()-alloc;
     await room.ToSignal(tree.CreateTimer(1.5),SceneTreeTimer.SignalName.Timeout);
     tree.ProcessFrame-=Tick;long drawn=Engine.GetFramesDrawn()-rendered;
     playback?.Dispose();if(node!=null&&GodotObject.IsInstanceValid(node)&&!node.IsQueuedForDeletion())node.QueueFree();
     output.Add(new{effect,pass,buildMs,readyMs=ReadyMs,allocated,drawn,frames=samples.Count,maxMs=samples.Max(),over33=samples.Count(x=>x>33.3),samples});
    }
   }
   Result=JsonSerializer.Serialize(new{status="done",mode,rendering=RenderingServer.GetCurrentRenderingMethod(),samples=output});
  }catch(Exception e){Result=JsonSerializer.Serialize(new{status="error",error=e.ToString()});}
 }
}
public sealed class FirstUseStatusCmd : AbstractConsoleCmd {public override string CmdName=>"karen_first_use_status";public override string Args=>"";public override string Description=>"First use measurement result";public override bool IsNetworked=>false;public override CmdResult Process(Player? p,string[] args)=>new(true,FirstUseCmd.Result);}
