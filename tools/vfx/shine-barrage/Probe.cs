using System;
using System.Collections.Generic;
using System.IO;
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
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.ExtraReplaySystem;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using ShoujoKagekiAijoKaren.src.Models.Cards;

// Development-only fixtures and passive observation; excluded from the production package.
[ModInitializer(nameof(Initialize))]
public static class BarrageProbe
{
    private sealed class Seen(NKarenShineBarrageVfx node)
    {
        public readonly NKarenShineBarrageVfx Node = node;
        public readonly bool Impact = (bool)Field(node,"_impact")!;
        public readonly int Index = (int)Field(node,"_index")!;
        public readonly Node[] Children = node.GetChildren().ToArray();
        public bool Exited, Final;
        public int Frames, ParticleFrames;
        public float MaxAnchorError, MaxBeyondSilhouette;
    }
    private static readonly List<Seen> SeenNodes = new();
    private static bool Watching;
    private static string? Capture;
    private static bool Captured;
    private static KarenShineStrikeBarrage? Card;
    private static object? Field(object node, string name) => node.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(node);
    private static bool Alive(Node node) => GodotObject.IsInstanceValid(node)&&node.IsInsideTree()&&!node.IsQueuedForDeletion();
    public static void Initialize()
    {
        var tree=(SceneTree)Engine.GetMainLoop();tree.Root.Unfocusable=true;
        tree.ProcessFrame+=()=>Callable.From(Sample).CallDeferred();
        GD.Print("[Barrage Probe] Loaded");
    }
    private static void Sample()
    {
        if(!Watching||NCombatRoom.Instance is not {} room)return;
        foreach(var node in room.CombatVfxContainer.GetChildren().OfType<NKarenShineBarrageVfx>())
        {
            var seen=SeenNodes.FirstOrDefault(s=>s.Node==node);
            if(seen==null){seen=new(node);SeenNodes.Add(seen);var s=seen;node.TreeExiting+=()=>{s.Exited=true;s.Final=(bool)Field(node,"_final")!;};}
            seen.Frames++;
            if(node.GetChildren().OfType<CpuParticles2D>().Any(p=>p.Emitting))seen.ParticleFrames++;
            if(Field(node,"_victim") is NCreature view&&Alive(view))
            {
                var bounds=view.Visuals.GetNodeOrNull<Control>("%Bounds")??view.Hitbox;
                var expected=bounds.GetGlobalTransformWithCanvas()*(bounds.Size*.5f);
                seen.MaxAnchorError=Math.Max(seen.MaxAnchorError,expected.DistanceTo((Vector2)Field(node,"_target")!));
                if(seen.Impact&&(float)Field(node,"_age")!<.24f)
                {
                    var transform=bounds.GetGlobalTransformWithCanvas();
                    var direction=(((Vector2)Field(node,"_target")!)-((Vector2)Field(node,"_source")!)).Normalized();
                    var corners=new[]{Vector2.Zero,new Vector2(bounds.Size.X,0),bounds.Size,new Vector2(0,bounds.Size.Y)};
                    float farSide=corners.Max(p=>(transform*p).Dot(direction));
                    seen.MaxBeyondSilhouette=Math.Max(seen.MaxBeyondSilhouette,((Vector2)Field(node,"_tip")!).Dot(direction)-farSide);
                }
            }
            if(Capture!=null&&!Captured&&seen.Impact&&(float)Field(node,"_age")!>.08f)
            {Captured=true;node.GetViewport().GetTexture().GetImage().SavePng(Capture);}
        }
    }
    internal static void Watch(string? capture){SeenNodes.Clear();Watching=true;Capture=capture;Captured=false;}
    internal static string Status(Player player)=>JsonSerializer.Serialize(new{
        live=SeenNodes.Count(s=>Alive(s.Node)),childLive=SeenNodes.Sum(s=>s.Children.Count(Alive)),shine=Card?.GetShineValue(),pile=Card?.Pile?.Type.ToString(),
        shineStock=ShinePileManager.GetShinePile(player).Cards.Count(c=>c is KarenShineStrikeBarrage),effects=SeenNodes.Select(s=>new{impact=s.Impact,index=s.Index,ownedChildren=s.Children.Length,frames=s.Frames,exited=s.Exited,final=s.Final,maxAnchorError=s.MaxAnchorError,particleFrames=s.ParticleFrames,maxBeyondSilhouette=s.MaxBeyondSilhouette})});
    internal static async Task Seed(Player player,string mode)
    {
        await CardPileCmd.RemoveFromCombat(PileType.Hand.GetPile(player).Cards.ToArray());
        var card=CombatManager.Instance.DebugOnlyGetState()!.CreateCard<KarenShineStrikeBarrage>(player);
        if(mode=="upgrade"){card.UpgradeInternal();card.FinalizeUpgradeInternal();}
        if(mode=="last")card.SetShineCurrent(1);
        if(mode=="replay")card.SetExtraReplayCountForNextPlay(2);
        await CardPileCmd.AddGeneratedCardToCombat(card,PileType.Hand,player);Card=card;
        if(mode=="stage")await PowerCmd.Apply<KarenStarlight02Power>(new ThrowingPlayerChoiceContext(),player.Creature,1,player.Creature,null);
    }
}
public sealed class BarrageProbeCmd:AbstractConsoleCmd
{
    public override string CmdName=>"barrage_probe";
    public override string Args=>"seed normal/upgrade/last/replay/stage|watch [capture.png]|status|speed normal/fast/instant|pose|lethal|clearfx";
    public override string Description=>"Isolated Barrage VFX verification";
    public override bool IsNetworked=>false;
    public override CmdResult Process(Player? player,string[] args)
    {
        if(player==null||args.Length==0)return new(false,Args);
        switch(args[0])
        {
            case "status":return new(true,BarrageProbe.Status(player));
            case "watch":BarrageProbe.Watch(args.Length>1?args[1]:null);return new(true,"Watching");
            case "seed":return new(BarrageProbe.Seed(player,args[1]),true,"Seeded");
            case "speed":SaveManager.Instance.PrefsSave.FastMode=args[1] switch{"fast"=>FastModeType.Fast,"instant"=>FastModeType.Instant,_=>FastModeType.Normal};return new(true,args[1]);
            case "lethal":foreach(var enemy in player.Creature.CombatState!.Enemies)enemy.SetCurrentHpInternal(4);return new(true,"hp=4");
            case "pose":
                var room=NCombatRoom.Instance!;var owner=room.GetCreatureNode(player.Creature)!;
                owner.ScaleTo(.65f,0);owner.Body.Scale*=new Vector2(-1,1);
                var target=room.CreatureNodes.First(v=>v.Entity.IsMonster);target.Position+=new Vector2(-170,45);
                target.Visuals.Rotation=.14f;target.Visuals.Scale*=new Vector2(-.8f,.8f);return new(true,"Transformed");
            case "clearfx":foreach(var node in NCombatRoom.Instance!.CombatVfxContainer.GetChildren().OfType<NKarenShineBarrageVfx>())node.QueueFree();return new(true,"Presentation removed");
            default:return new(false,Args);
        }
    }
}
