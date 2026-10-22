using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

// Test plugin only. Exercises real visual factories; no damage/benefit/card-play claims.
public sealed class AllVfxCmd : AbstractConsoleCmd
{
    public override string CmdName => "karen_all_vfx_first_use";
    public override string Args => "status|run";
    public override string Description => "All resource families and repeated visual playback";
    public override bool IsNetworked => false;
    private static string Result = "idle";
    public override CmdResult Process(Player? player,string[] args)
    {
        if(args.FirstOrDefault()=="status")return new(true,Result);
        if(player==null)return new(false,"Player required");
        Result="running";return new(Run(player),true,"running");
    }
    private static async Task Run(Player p)
    {
        try
        {
            var room=NCombatRoom.Instance!;var tree=room.GetTree();
            var asm=typeof(NKarenBridgeVfx).Assembly;
            var warm=asm.GetType("ShoujoKagekiAijoKaren.src.Core.Vfx.KarenVfxWarmup")!;
            var catalog=asm.GetType("ShoujoKagekiAijoKaren.src.Core.Vfx.KarenVfxCatalog")!;
            object? Value(string name)=>AccessTools.Property(warm,name).GetValue(null);
            if((string)Value("Status")! != "ready" || (int)Value("DrawnBatches")!<=0 || (int)Value("DrawCalls")!<=0)throw new Exception("Render warmup failed");
            var resources=(IDictionary)AccessTools.Field(warm,"Resources").GetValue(null)!;
            void Valid(){foreach(Resource resource in resources.Values)if(!GodotObject.IsInstanceValid(resource))throw new Exception("Disposed prewarmed resource: "+resource.GetType().FullName);}
            Valid();
            var paths=(string[])AccessTools.Field(catalog,"AssetPaths").GetValue(null)!;
            // Reproduce native cache eviction against only our own catalog in this isolated game.
            PreloadManager.Cache.UnloadAssets(paths);
            await room.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            Valid();
            var misses=(HashSet<string>)AccessTools.Field(typeof(AssetCache),"_missedCacheAssets").GetValue(PreloadManager.Cache)!;
            misses.Add(paths.First(path=>path.EndsWith(".tres")));
            PreloadManager.Cache.UnloadMissedCacheAssets();
            await room.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            Valid();
            long started=Stopwatch.GetTimestamp();
            await (Task)AccessTools.Method(warm,"EnsureReady").Invoke(null,null)!;
            double repeatedPreparationMs=Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            if(repeatedPreparationMs>10)throw new Exception("Warmup repeated unexpectedly");
            if(tree.Root.GetChildren().Any(n=>n.Name=="KarenVfxWarmup"))throw new Exception("Warmup viewport leaked");
            var target=p.Creature.CombatState!.GetOpponentsOf(p.Creature).First();
            var card=PileType.Hand.GetPile(p).Cards.First();
            var cases=new List<(string Name,Func<Node?> Create)> {
                ("nonon",()=>NKarenNononVfx.Create(p)),
                ("barrage-start",()=>NKarenShineBarrageVfx.Create(p.Creature,target)),
                ("barrage-hit",()=>NKarenShineBarrageVfx.Hit(p.Creature,target,0)),
                ("baseball",()=>NKarenBaseballVfx.Create(p.Creature,target)),
                ("baseball-impact",()=>NKarenBaseballVfx.CreateImpact(target,null)),
                ("jealousy",()=>NKarenJealousyVfx.CreateImpact(p.Creature,target)),
                ("memory",()=>NKarenUnforgottenShineVfx.Create(p.Creature,target)),
                ("back-to-back",()=>NKarenBackToBackVfx.CreateApproach(p.Creature,target,3)),
                ("back-to-back-impact",()=>NKarenBackToBackVfx.CreateImpact(p.Creature,target,3,null)),
                ("stage-light",()=>NKarenStageLightVfx.Create(target)),
                ("stage-light-impact",()=>NKarenStageLightImpactVfx.Create(target)),
                ("arrogant",()=>NKarenArrogantVfx.Play(p))
            };
            foreach(var kind in Enum.GetValues<NKarenBenefitVfx.BenefitKind>())cases.Add(("benefit-"+kind,()=>NKarenBenefitVfx.Play(p,kind,card)));
            foreach(var kind in Enum.GetValues<NKarenStageCueVfx.CueKind>())cases.Add(("stage-"+kind,()=>NKarenStageCueVfx.Play(p,kind,card)));
            var output=new List<object>();
            foreach(var test in cases)
            {
                var passes=new List<object>();
                for(int pass=0;pass<3;pass++)
                {
                    await room.ToSignal(tree.CreateTimer(.2),SceneTreeTimer.SignalName.Timeout);
                    var samples=new List<double>();long last=Stopwatch.GetTimestamp();
                    void Tick(){long now=Stopwatch.GetTimestamp();samples.Add(Stopwatch.GetElapsedTime(last,now).TotalMilliseconds);last=now;}
                    tree.ProcessFrame+=Tick;long frames=Engine.GetFramesDrawn();long begin=Stopwatch.GetTimestamp();
                    Node? node=test.Create();
                    if(node==null)throw new Exception("Factory returned null: "+test.Name);
                    if(node.GetParent()==null)room.CombatVfxContainer.AddChild(node);
                    double createMs=Stopwatch.GetElapsedTime(begin).TotalMilliseconds;
                    await room.ToSignal(tree.CreateTimer(1.4),SceneTreeTimer.SignalName.Timeout);
                    tree.ProcessFrame-=Tick;
                    if(Engine.GetFramesDrawn()-frames<20)throw new Exception("No rendering");
                    passes.Add(new{pass,createMs,maxMs=samples.Max(),over33=samples.Count(x=>x>33.3),samples});
                    if(GodotObject.IsInstanceValid(node)&&!node.IsQueuedForDeletion())node.QueueFree();
                    await room.ToSignal(tree.CreateTimer(.3),SceneTreeTimer.SignalName.Timeout);
                }
                output.Add(new{effect=test.Name,passes});
            }
            Valid();
            Result=JsonSerializer.Serialize(new{status="done",warmupStatus=Value("Status"),resources=Value("ResourceCount"),batches=Value("DrawnBatches"),drawCalls=Value("DrawCalls"),prepareMs=Value("ElapsedMs"),repeatedPreparationMs,catalogEffects=((string[])AccessTools.Field(catalog,"EffectTypes").GetValue(null)!).Length,assetPaths=paths.Length,samples=output});
        }
        catch(Exception e){Result=JsonSerializer.Serialize(new{status="error",error=e.ToString()});}
        finally
        {
            string? output=System.Environment.GetEnvironmentVariable("KAREN_ALL_VFX_RESULT");
            if(!string.IsNullOrEmpty(output)){ File.WriteAllText(output+".tmp",Result); File.Move(output+".tmp",output,true); }
        }
    }
}
