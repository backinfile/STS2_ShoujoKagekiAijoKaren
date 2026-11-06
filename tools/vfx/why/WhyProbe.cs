using System;
using System.Collections.Generic;
using System.Linq;
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
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.ancient;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

// Isolated verification only; all card plays and choices still use the native MCP actions.
[ModInitializer(nameof(Initialize))]
public static class WhyProbe
{
    internal sealed class Seen(NKarenWhyVfx node) {
        public readonly NKarenWhyVfx Node=node;
        public int Frames, SelectionFrames, UnexpectedVisibleFrames;
        public bool Freed, ScopeCorrect=true;
        public int SurfaceFrames, ShaderSamples, AdvancingSamples;
        public bool SurfaceCoversViewport=true, InputIgnored=true;
        public float LastShaderAge;
    }
    internal static readonly List<Seen> Effects=new();
    private static Node2D? _clock;
    private static readonly List<ColorRect> ClockCells=new();
    private static long _clockStarted;
    internal static void ShowClock(bool show) {
        if(GodotObject.IsInstanceValid(_clock)) _clock!.QueueFree();
        _clock=null;ClockCells.Clear();
        if(!show) return;
        _clock=new Node2D{Name="WhyRecordingAuditClock",Position=new Vector2(120,120),ZIndex=4095,ZAsRelative=false};
        _clock.AddChild(new ColorRect{Size=new Vector2(264,28),Color=Colors.Black,MouseFilter=Control.MouseFilterEnum.Ignore});
        for(int bit=0;bit<16;bit++) {
            var cell=new ColorRect{Position=new Vector2(4+16*bit,4),Size=new Vector2(12,20),Color=Colors.Black,MouseFilter=Control.MouseFilterEnum.Ignore};
            _clock.AddChild(cell);ClockCells.Add(cell);
        }
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_clock);
        _clockStarted=System.Diagnostics.Stopwatch.GetTimestamp();
    }
    public static void Initialize() => ((SceneTree)Engine.GetMainLoop()).ProcessFrame += Sample;
    private static void Sample() {
        if(GodotObject.IsInstanceValid(_clock)) {
            int ticks=(int)(System.Diagnostics.Stopwatch.GetElapsedTime(_clockStarted).TotalMilliseconds/10)&65535;
            int gray=ticks^(ticks>>1);
            for(int bit=0;bit<16;bit++) ClockCells[bit].Color=(gray&(1<<bit))==0?Colors.Black:Colors.White;
        }
        if(NCombatRoom.Instance is not {} room) return;
        foreach(var node in room.Ui.Hand.GetChildren().OfType<NKarenWhyVfx>()) {
            var seen=Effects.FirstOrDefault(e=>e.Node==node);
            if(seen==null) { seen=new Seen(node); Effects.Add(seen); var capture=seen; node.TreeExiting+=()=>capture.Freed=true; }
            seen.Frames++;
            if(node.IsSelectionActive && node.IsVisibleInTree() && node.Modulate.A>.1f) seen.SelectionFrames++;
            if(!node.IsSelectionActive && !node.IsClosing && node.IsVisibleInTree() && node.Modulate.A>.02f) seen.UnexpectedVisibleFrames++;
            seen.ScopeCorrect &= node.GetIndex()>room.Ui.Hand.GetNode<Control>("%SelectModeBackstop").GetIndex()
                && node.GetIndex()<room.Ui.Hand.CardHolderContainer.GetIndex();
            if(node.GetNodeOrNull<ColorRect>("EdgeLight") is {} light) {
                seen.SurfaceFrames++;
                seen.InputIgnored &= light.MouseFilter==Control.MouseFilterEnum.Ignore && light.FocusMode==Control.FocusModeEnum.None;
                // Resizing is applied on the following process pass, so inspect settled sizes.
                if(seen.Frames%6==0 && light.Material is ShaderMaterial material) {
                    seen.SurfaceCoversViewport &= light.Size.IsFinite() && light.Size.DistanceTo((Vector2)material.GetShaderParameter("screen_size"))<1;
                    float age=(float)material.GetShaderParameter("effect_age");
                    if(seen.ShaderSamples>0 && age>seen.LastShaderAge) seen.AdvancingSamples++;
                    seen.LastShaderAge=age;seen.ShaderSamples++;
                }
            }
        }
    }
    internal static object Status(Player p) => new {
        live=NCombatRoom.Instance?.Ui.Hand.GetChildren().OfType<NKarenWhyVfx>().Count()??0,
        layers=NCombatRoom.Instance is {} room ? room.BackCombatVfxContainer.GetChildren().Count(n=>n.Name.ToString().StartsWith("KarenWhy")) : 0,
        visible=NCombatRoom.Instance?.Ui.Hand.GetChildren().OfType<NKarenWhyVfx>().Any(n=>n.IsVisibleInTree()&&n.Modulate.A>.1f)??false,
        selecting=NCombatRoom.Instance?.Ui.Hand.IsInCardSelection??false,
        peeking=NCombatRoom.Instance?.Ui.Hand.PeekButton.IsPeeking??false,
        viewport=NCombatRoom.Instance?.GetViewportRect().Size.ToString(),
        texture=NCombatRoom.Instance?.GetViewport().GetTexture().GetSize().ToString(),
        clock=GodotObject.IsInstanceValid(_clock)?new{position=new[]{120,120},viewport=new[]{_clock!.GetViewportRect().Size.X,_clock.GetViewportRect().Size.Y},bits=16,cellStep=16,cellCenter=new[]{10,14},stepMs=10}:null,
        effects=Effects.Select(s=>new{s.Frames,s.SelectionFrames,s.UnexpectedVisibleFrames,s.ScopeCorrect,s.Freed,
            s.SurfaceFrames,s.ShaderSamples,s.AdvancingSamples,s.SurfaceCoversViewport,s.InputIgnored}),
        block=p.Creature.Block, hand=PileType.Hand.GetPile(p).Cards.Select(c=>c.Id.Entry),
        promise=KarenCustomEnum.PromisePile.GetPile(p).Cards.Select(c=>c.Id.Entry),
        draw=PileType.Draw.GetPile(p).Cards.Select(c=>c.Id.Entry)
    };
}
public sealed class WhyProbeCmd : AbstractConsoleCmd
{
    public override string CmdName=>"why_probe";
    public override string Args=>"status|reset|speed normal/fast/instant|seed COUNT base/upgrade/void";
    public override string Description=>"Prepare and observe Why card VFX in an isolated process.";
    public override bool IsNetworked=>false;
    public override CmdResult Process(Player? player,string[] args) {
        if(player?.PlayerCombatState==null||args.Length==0) return new(false,Args);
        if(args[0]=="status") return new(true,JsonSerializer.Serialize(WhyProbe.Status(player)));
        if(args[0]=="reset") { WhyProbe.Effects.Clear(); return new(true,"Cleared"); }
        if(args[0]=="clock") { WhyProbe.ShowClock(args[1]=="on"); return new(true,"Recording audit clock toggled"); }
        if(args[0]=="peek") { NCombatRoom.Instance!.Ui.Hand.PeekButton.SetPeeking(args[1]=="true"); return new(true,"Peek toggled"); }
        if(args[0]=="visual") {
            foreach(var node in NCombatRoom.Instance!.Ui.Hand.GetChildren().OfType<NKarenWhyVfx>())
                if(node.GetNodeOrNull<ColorRect>("EdgeLight") is {} light) light.Visible=args[1]=="on";
            return new(true,"Light visibility changed for visual comparison");
        }
        if(args[0]=="speed") { SaveManager.Instance.PrefsSave.FastMode=args[1] switch { "instant"=>FastModeType.Instant,"fast"=>FastModeType.Fast,_=>FastModeType.Normal }; return new(true,args[1]); }
        if(args[0]=="seed"&&args.Length==3) return new(Seed(player,int.Parse(args[1]),args[2]),true,"Fixture prepared");
        return new(false,Args);
    }
    private static async Task Seed(Player player,int count,string mode) {
        var combat=CombatManager.Instance.DebugOnlyGetState()!;
        await CardPileCmd.RemoveFromCombat(new[]{PileType.Hand,PileType.Draw,PileType.Discard,PileType.Exhaust,KarenCustomEnum.PromisePile}.SelectMany(p=>p.GetPile(player).Cards).Distinct().ToArray());
        if(mode=="void") await PromisePileManager.EnterMode(player,PromisePileMode.Void);
        var ids=new[]{"KAREN_STRIKE","KAREN_DEFEND","KAREN_SHINE_STRIKE","KAREN_FALL"};
        for(int i=0;i<count;i++) await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard(ModelDb.AllCards.Single(c=>c.Id.Entry==ids[i%ids.Length]),player),PileType.Hand,player);
        var why=combat.CreateCard<KarenWhy>(player);
        if(mode=="upgrade") { why.UpgradeInternal(); why.FinalizeUpgradeInternal(); }
        await CardPileCmd.AddGeneratedCardToCombat(why,PileType.Hand,player);
    }
}
