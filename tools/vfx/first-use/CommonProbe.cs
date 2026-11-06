using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.basic;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem.Nodes;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

[ModInitializer(nameof(Initialize))]
public static class CommonProbe
{
    public static string Result = "idle";
    private static bool HoldShine, Measuring;
    private static int PreparedContours;
    private static CardModel? Card;
    private static readonly Dictionary<string,List<double>> Cpu = new();

    public static void Initialize()
    {
        string? effect = System.Environment.GetEnvironmentVariable("KAREN_VFX_PROBE_EFFECT");
        if (effect is not ("shine" or "promise")) return;
        HoldShine = effect == "shine";
        var h = new Harmony("karen.common-first-use.probe");
        h.Patch(AccessTools.Method(typeof(ShineVfx),"Refresh"),prefix:new HarmonyMethod(typeof(CommonProbe),nameof(AllowShine)));
        var border=typeof(ShineVfx).Assembly.GetType("ShoujoKagekiAijoKaren.src.Core.ShineSystem.ShineCardBorder")!;
        foreach(var method in new[]{AccessTools.Method(border,"ReadContour"),
            AccessTools.Method(typeof(NKarenShineAmbientVfx),"_Ready"),
            AccessTools.Method(typeof(NKarenPromiseCardEntryVfx),"_Ready"),
            AccessTools.Method(typeof(KarenPromiseVfxStarManager),"GuideIntoOrbit")})
            h.Patch(method,prefix:new HarmonyMethod(typeof(CommonProbe),nameof(Before)),postfix:new HarmonyMethod(typeof(CommonProbe),nameof(After)));
    }
    private static bool AllowShine() => !HoldShine;
    private static void Before(out long __state) => __state=Stopwatch.GetTimestamp();
    private static void After(System.Reflection.MethodBase __originalMethod,long __state)
    {
        if(!Measuring){if(__originalMethod.Name=="ReadContour")PreparedContours++;return;}
        string key=__originalMethod.DeclaringType!.Name+"."+__originalMethod.Name;
        if(!Cpu.TryGetValue(key,out var values))Cpu[key]=values=new();
        values.Add((Stopwatch.GetTimestamp()-__state)*1000d/Stopwatch.Frequency);
    }
    private static async Task Wait(double seconds) => await NCombatRoom.Instance!.ToSignal(
        NCombatRoom.Instance.GetTree().CreateTimer(seconds),SceneTreeTimer.SignalName.Timeout);
    public static async Task Run(Player player,string operation,string effect)
    {
        try
        {
            Result="running";
            if(operation=="prepare")
            {
                if(effect=="shine")
                {
                    Card=player.Creature.CombatState!.CreateCard<KarenShineStrike>(player);
                    await CardPileCmd.AddGeneratedCardsToCombat(new[]{Card},PileType.Hand,player);
                }
                else
                {
                    if(!player.Creature.HasPower<KarenPromisePilePower>())
                        await PowerCmd.Apply<KarenPromisePilePower>(new ThrowingPlayerChoiceContext(),player.Creature,1,player.Creature,null);
                    Card=PileType.Hand.GetPile(player).Cards.First(c=>!c.IsShineCard());
                }
                await Wait(2);
                object? frameInfo=null;
                if(effect=="shine")
                {
                    var view=NCard.FindOnTable(Card)!;var texture=view.GetNode<TextureRect>("%Frame").Texture;
                    var atlas=texture as AtlasTexture;
                    frameInfo=new{path=texture.ResourcePath,size=new[]{texture.GetWidth(),texture.GetHeight()},atlas=atlas?.Atlas.ResourcePath,
                        atlasSize=atlas==null?null:new[]{atlas.Atlas.GetWidth(),atlas.Atlas.GetHeight()},region=atlas?.Region.ToString()};
                    var border=typeof(ShineVfx).Assembly.GetType("ShoujoKagekiAijoKaren.src.Core.ShineSystem.ShineCardBorder")!;
                    string mode=System.Environment.GetEnvironmentVariable("KAREN_VFX_PROBE_MODE")??"cold";
                    if(mode=="preload")
                    {
                        var points=(Vector2[])AccessTools.Method(border,"ReadContour").Invoke(null,new object[]{texture})!;
                        ((System.Collections.IDictionary)AccessTools.Field(border,"Contours").GetValue(null)!)[texture]=points;
                    }
                    if(mode=="draw")
                    {
                        HoldShine=false;view.UpdateVisuals(PileType.Hand,CardPreviewMode.Normal);await Wait(1.2);
                        HoldShine=true;AccessTools.Method(typeof(ShineVfx),"Clear").Invoke(null,new object[]{view});await Wait(.3);
                    }
                }
                Result=JsonSerializer.Serialize(new{status="prepared",card=Card.Id.Entry,frame=frameInfo});return;
            }
            if(operation=="dump")
            {
                var border=typeof(ShineVfx).Assembly.GetType("ShoujoKagekiAijoKaren.src.Core.ShineSystem.ShineCardBorder")!;
                var frames=new List<object>();
                foreach(string kind in new[]{"attack","skill","power","quest","ancient"})
                {
                    string path="res://images/atlases/ui_atlas.sprites/card/card_frame_"+kind+"_s.tres";
                    if(!ResourceLoader.Exists(path))continue;
                    var texture=ResourceLoader.Load<Texture2D>(path);
                    var points=(Vector2[])AccessTools.Method(border,"ReadContour").Invoke(null,new object[]{texture})!;
                    var cache=(System.Collections.IDictionary)AccessTools.Field(border,"Contours").GetValue(null)!;
                    bool cached=cache.Contains(texture);
                    bool cacheMatches=!cached||((Vector2[])cache[texture]!).SequenceEqual(points);
                    if(!cacheMatches)throw new Exception("Prepared contour differs from actual image: "+path);
                    frames.Add(new{path,cached,cacheMatches,points=points.Select(v=>new[]{v.X,v.Y}).ToArray()});
                }
                Result=JsonSerializer.Serialize(new{status="contours",frames});return;
            }
            if(operation=="reset")
            {
                if(effect=="shine")
                {
                    HoldShine=true;
                    AccessTools.Method(typeof(ShineVfx),"Clear").Invoke(null,new object[]{NCard.FindOnTable(Card!)!});
                }
                else await PromisePileManager.DrawFromPromisePileAsync(new ThrowingPlayerChoiceContext(),player);
                await Wait(.6);Result=JsonSerializer.Serialize(new{status="reset"});return;
            }
            var tree=NCombatRoom.Instance!.GetTree();
            var samples=new List<double>();Cpu.Clear();
            long last=Stopwatch.GetTimestamp();
            void Tick(){long now=Stopwatch.GetTimestamp();samples.Add((now-last)*1000d/Stopwatch.Frequency);last=now;}
            tree.ProcessFrame+=Tick;long rendered=Engine.GetFramesDrawn();
            long allocated=GC.GetTotalAllocatedBytes();long started=Stopwatch.GetTimestamp();Measuring=true;
            Task? moving=null;
            if(effect=="shine")
            {
                HoldShine=false;
                NCard.FindOnTable(Card!)!.UpdateVisuals(PileType.Hand,CardPreviewMode.Normal);
            }
            else moving=PromisePileCmd.Add(player,Card!);
            double syncMs=(Stopwatch.GetTimestamp()-started)*1000d/Stopwatch.Frequency;
            if(moving!=null)await moving;
            await Wait(1.2);
            Measuring=false;tree.ProcessFrame-=Tick;
            int outlinePoints=0;
            if(effect=="shine")
            {
                var ambient=NCard.FindOnTable(Card!)!.OverlayContainer.GetChildren().OfType<NKarenShineAmbientVfx>().Single();
                var border=AccessTools.Field(typeof(NKarenShineAmbientVfx),"_border").GetValue(ambient);
                outlinePoints=((Vector2[])AccessTools.Property(border!.GetType(),"Outline").GetValue(border)!).Length;
                if(!ambient.IsVisibleInTree()||!ambient.Visible||outlinePoints<4)throw new Exception("Visible Shine border missing");
            }
            Result=JsonSerializer.Serialize(new{status="done",effect,syncMs,allocated=GC.GetTotalAllocatedBytes()-allocated,
                drawn=Engine.GetFramesDrawn()-rendered,frames=samples.Count,maxMs=samples.Max(),medianMs=samples.Order().ElementAt(samples.Count/2),
                over33=samples.Count(x=>x>33.3),outlinePoints,preparedContours=PreparedContours,cpu=Cpu,samples});
        }
        catch(Exception error){Measuring=false;Result=JsonSerializer.Serialize(new{status="error",error=error.ToString()});}
    }
}
public sealed class CommonFirstUseCmd : AbstractConsoleCmd
{
    public override string CmdName=>"karen_common_first_use";
    public override string Args=>"prepare|run|reset shine|promise";
    public override string Description=>"Target real Shine and Promise paths";
    public override bool IsNetworked=>false;
    public override CmdResult Process(Player? p,string[] args)=>p!=null&&args.Length==2
        ?new(CommonProbe.Run(p,args[0],args[1]),true,"running"):new(false,"Arguments required");
}
public sealed class CommonFirstUseStatusCmd : AbstractConsoleCmd
{
    public override string CmdName=>"karen_common_first_use_status";public override string Args=>"";
    public override string Description=>"Common VFX measurement result";public override bool IsNetworked=>false;
    public override CmdResult Process(Player? p,string[] args)=>new(true,CommonProbe.Result);
}
