using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using ShoujoKagekiAijoKaren.src.Models.Cards;

// Test-only fixture and frame observer; never packaged with the character mod.
[ModInitializer(nameof(Initialize))]
public static class MemoryProbe
{
    internal sealed class Seen(NKarenUnforgottenShineVfx node)
    {
        public readonly NKarenUnforgottenShineVfx Node=node;
        public readonly Node Back=(Node)Field(node,"_back")!;
        public readonly CpuParticles2D[] Particles=node.GetChildren().OfType<CpuParticles2D>().ToArray();
        public string Kind=Field(node,"_kind")!.ToString()!;
        public int Frames;
        public bool Exited,AssetsReady;
        public int ParticleEmissionFrames;
        public float FinalAge,HitAt=-1,MaxAnchorError,Unit;
    }
    internal static readonly List<Seen> Effects=new();
    internal static bool Watching;
    internal static string? CaptureDir;
    private static readonly HashSet<string> Captured=new();
    internal static void ResetCaptures()=>Captured.Clear();
    internal static KarenShineTogether? Card;
    private static object? Field(object node,string name)=>node.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(node);
    public static void Initialize()=>((SceneTree)Engine.GetMainLoop()).ProcessFrame+=()=>Callable.From(Sample).CallDeferred();
    private static bool Alive(Node node)=>GodotObject.IsInstanceValid(node)&&node.IsInsideTree()&&!node.IsQueuedForDeletion();
    private static void Sample()
    {
        if(!Watching||NCombatRoom.Instance is not {} room)return;
        foreach(var node in room.CombatVfxContainer.GetChildren().OfType<NKarenUnforgottenShineVfx>())
        {
            var seen=Effects.FirstOrDefault(s=>s.Node==node);
            if(seen==null){seen=new Seen(node);Effects.Add(seen);var captured=seen;node.TreeExiting+=()=>{captured.Exited=true;captured.FinalAge=(float)Field(node,"_age")!;};}
            seen.Frames++;seen.HitAt=(float)Field(node,"_hitAt")!;seen.Unit=(float)Field(node,"_unit")!;
            seen.AssetsReady |= node.AssetsReady;
            if(seen.Particles.Any(p=>Alive(p)&&p.Emitting))seen.ParticleEmissionFrames++;
            float age=(float)Field(node,"_age")!;
            string? stage=seen.Kind switch{
                "Attack" when age>=.14f&&age<.19f=>"01-focus",
                "Attack" when age>=.53f&&age<.64f=>"02-cut",
                "Impact" when age>=.12f&&age<.20f=>"03-shine",
                "Attack" when seen.HitAt>=0&&age-seen.HitAt>=1.0f&&age-seen.HitAt<1.2f=>"04-remain",
                _=>null};
            if(CaptureDir!=null&&stage!=null&&Captured.Add(stage))
            {
                node.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(CaptureDir,stage+".png"));
                File.WriteAllText(Path.Combine(CaptureDir,stage+".json"),JsonSerializer.Serialize(new{stage,age,seen.Kind,
                    particleEmitters=seen.Particles.Length,emitting=seen.Particles.Count(p=>Alive(p)&&p.Emitting)}));
            }
            if(Field(node,"_victim") is not NCreature target||!Alive(target))continue;
            var bounds=target.Visuals.GetNodeOrNull<Control>("%Bounds")??target.Hitbox;
            var expected=bounds.GetGlobalTransformWithCanvas()*(bounds.Size*.5f);
            var actual=(Vector2)Field(node,"_target")!;
            seen.MaxAnchorError=Math.Max(seen.MaxAnchorError,actual.DistanceTo(expected));
        }
    }
    internal static object Status(Player player)=>new{
        live=Effects.Count(e=>Alive(e.Node)),backLive=Effects.Count(e=>Alive(e.Back)),
        particleLive=Effects.Sum(e=>e.Particles.Count(Alive)),
        shine=Card?.GetShineValue(),pile=Card?.Pile?.Type.ToString(),
        rewardPower=player.Creature.GetPowerAmount<KarenShineCardRewardPower>(),
        extraRewards=player.RunState.CurrentRoom is CombatRoom room&&room.ExtraRewards.TryGetValue(player,out var rewards)?rewards.Count:0,
        shinePile=ShinePileManager.GetShinePile(player).Cards.Count,
        effects=Effects.Select(e=>new{e.Kind,e.Frames,e.Exited,e.FinalAge,e.HitAt,e.MaxAnchorError,e.Unit,e.AssetsReady,
            particleEmitters=e.Particles.Length,e.ParticleEmissionFrames})};
}
public sealed class MemoryProbeCmd:AbstractConsoleCmd
{
    public override string CmdName=>"memory_probe";
    public override string Args=>"seed ordinary/last/upgrade/replay|watch|status|speed normal/fast/instant|pose|lethal";
    public override string Description=>"Observe real Unforgettable Shine plays in isolated games.";
    public override bool IsNetworked=>false;
    public override CmdResult Process(Player? player,string[] args)
    {
        if(player==null||args.Length==0)return new(false,Args);
        switch(args[0])
        {
            case "status":return new(true,JsonSerializer.Serialize(MemoryProbe.Status(player)));
            case "watch":MemoryProbe.Effects.Clear();MemoryProbe.Watching=true;MemoryProbe.CaptureDir=args.Length>1?args[1]:null;
                if(MemoryProbe.CaptureDir!=null)Directory.CreateDirectory(MemoryProbe.CaptureDir);
                MemoryProbe.ResetCaptures();return new(true,"Watching");
            case "seed":return new(Seed(player,args[1]),true,"Prepared");
            case "speed":SaveManager.Instance.PrefsSave.FastMode=args[1] switch{"fast"=>FastModeType.Fast,"instant"=>FastModeType.Instant,_=>FastModeType.Normal};return new(true,args[1]);
            case "lethal":NCombatRoom.Instance!.CreatureNodes.First(v=>v.Entity.IsMonster).Entity.SetCurrentHpInternal(12);return new(true,"hp=12");
            case "pose":
                var owner=NCombatRoom.Instance!.GetCreatureNode(player.Creature)!;
                owner.ScaleTo(.65f,0);owner.Body.Scale*=new Vector2(-1,1);
                var target=NCombatRoom.Instance.CreatureNodes.First(v=>v.Entity.IsMonster);
                target.Position+=new Vector2(-170,45);target.Visuals.Rotation=.14f;return new(true,"Transformed");
            default:return new(false,Args);
        }
    }
    private static async Task Seed(Player player,string mode)
    {
        await CardPileCmd.RemoveFromCombat(PileType.Hand.GetPile(player).Cards.ToArray());
        var state=CombatManager.Instance.DebugOnlyGetState()!;
        var card=state.CreateCard<KarenShineTogether>(player);
        if(mode=="upgrade"){card.UpgradeInternal();card.FinalizeUpgradeInternal();}
        if(mode!="ordinary")card.SetShineCurrent(mode=="replay"?2:1);
        await CardPileCmd.AddGeneratedCardToCombat(card,PileType.Hand,player);
        MemoryProbe.Card=card;
        if(mode=="replay")await PowerCmd.Apply<KarenStarlight02Power>(new ThrowingPlayerChoiceContext(),player.Creature,1,player.Creature,null);
    }
}
