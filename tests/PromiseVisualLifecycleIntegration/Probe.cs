using System.Reflection;
using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

[MegaCrit.Sts2.Core.Modding.ModInitializer(nameof(Initialize))]
public static class PromiseLifecycleFixture
{
 public static void Initialize()
 {
  var tree=(SceneTree)Engine.GetMainLoop();
  RenderingServer.ViewportSetUpdateMode(tree.Root.GetViewportRid(),RenderingServer.ViewportUpdateMode.Always);
  tree.ProcessFrame+=()=>RenderingServer.ForceDraw(false);
 }
}
public sealed class PromiseLifecycleCmd : AbstractConsoleCmd
{
 public override string CmdName=>"promise_lifecycle";
 public override string Args=>"check|report";
 public override string Description=>"Directed OBS-054 visual lifecycle regression.";
 public override bool IsNetworked=>false;
 static string Report="{}";
 public override CmdResult Process(Player? player,string[] args)
 {
  if(args.FirstOrDefault()=="report")return new(true,Report);
  if(player==null)return new(false,"Run required");
  return new(Check(player),true,"running");
 }
 static async Task Check(Player player)
 {
  Report="{\"status\":\"running\"}";
  var rows=new List<object>();var temporary=new List<GodotObject>();
  var creature=NCombatRoom.Instance!.GetCreatureNode(player.Creature)!;
  var original=creature.Visuals;
  var assembly=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetType("ShoujoKagekiAijoKaren.Core.SNCreatureVisuals")!=null);
  var type=assembly.GetType("ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx.NKarenPromiseStarNode")!;
  var star=(Node2D)Activator.CreateInstance(type)!;temporary.Add(star);
  var visualProperty=typeof(NCreature).GetProperty("Visuals")!;
  var placement=AccessTools.Method(type,"UpdatePlacement");
  var tick=AccessTools.Method(type,"_Process");
  void Run(string name,Action check){try{check();rows.Add(new{name,passed=true});}catch(Exception e){rows.Add(new{name,passed=false,error=(e is TargetInvocationException ? e.InnerException! : e).ToString()});}}
  void SetVisual(NCreatureVisuals? visual)=>visualProperty.SetValue(creature,visual);
  try
  {
   creature.AddChild(star);star.SetProcess(false);
   AccessTools.Field(type,"_creature").SetValue(star,creature);
   AccessTools.Field(type,"_alwaysVisible").SetValue(star,true);
   placement.Invoke(star,null);var before=star.Transform;
   var freed=(NCreatureVisuals)Activator.CreateInstance(assembly.GetType("ShoujoKagekiAijoKaren.Core.SNCreatureVisuals")!)!;
   freed.Free();
   Run("disposed child visuals are safe across repeated ticks",()=>{
    SetVisual(freed);
    for(int i=0;i<3;i++){placement.Invoke(star,null);tick.Invoke(star,[1d/60]);}
    if(star.Transform!=before)throw new Exception("Last placement lost during visual replacement");
   });
   Run("missing child visuals are safe",()=>{SetVisual(null);placement.Invoke(star,null);});
   var queued=new NCreatureVisuals();temporary.Add(queued);queued.QueueFree();
   Run("queued child visuals are safe",()=>{SetVisual(queued);placement.Invoke(star,null);});
   Run("replacement visuals resume placement",()=>{
    SetVisual(original);star.Transform=Transform2D.Identity;placement.Invoke(star,null);
    if(!star.GlobalPosition.IsEqualApprox(creature.VfxSpawnPosition))throw new Exception("Replacement origin not followed");
    var basis=original.GlobalTransform;
    if(!Mathf.IsEqualApprox(star.Transform.X.Length(),basis.X.Length()))throw new Exception("Replacement scale not followed");
   });
   Run("live child scale changes remain aligned",()=>{
    var scale=original.Scale;
    try{original.Scale=scale*.8f;placement.Invoke(star,null);
     if(!Mathf.IsEqualApprox(star.Transform.X.Length(),original.GlobalTransform.X.Length()))throw new Exception("Live scale not followed");}
    finally{original.Scale=scale;placement.Invoke(star,null);}
   });
   var parent=new NCreature();temporary.Add(parent);typeof(NCreature).GetProperty("Entity")!.SetValue(parent,player.Creature);visualProperty.SetValue(parent,original);parent.QueueFree();
   Run("queued parent stops and frees star",()=>{
    AccessTools.Field(type,"_creature").SetValue(star,parent);tick.Invoke(star,[1d/60]);
    if(!star.IsQueuedForDeletion())throw new Exception("Star outlives queued parent");
   });
  }
  catch(Exception e){rows.Add(new{name="fixture",passed=false,error=e.ToString()});}
  finally
  {
   SetVisual(original);
   foreach(var node in temporary.OfType<Node>())if(GodotObject.IsInstanceValid(node)&&!node.IsQueuedForDeletion())node.QueueFree();
  }
  var tree=(SceneTree)Engine.GetMainLoop();await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
  Report=JsonSerializer.Serialize(new{status="done",temporaryNodesFreed=temporary.All(n=>!GodotObject.IsInstanceValid(n)),rows});
  File.WriteAllText(Path.Combine(OS.GetUserDataDir(),"promise-lifecycle-result.json"),Report);
 }
}
