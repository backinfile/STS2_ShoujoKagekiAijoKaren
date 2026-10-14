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
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.basic;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem.Nodes;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

[ModInitializer(nameof(Initialize))]
public static class StageProbe
{
    internal sealed class Seen(NKarenStageCueVfx node)
    {
        public readonly NKarenStageCueVfx Node=node;
        public string Kind=node.Kind.ToString();
        public bool Confirmed,BackCorrect;
        public int Cards,Energy,OwnedFrames,DepletionHeldFrames,SettledHeldFrames,EndingHeldFrames;
        public float Age;
        public readonly HashSet<int> PlayIndices=new();
    }
    internal static readonly List<Seen> Casts=new();
    internal static bool Watching;
    public static void Initialize(){((SceneTree)Engine.GetMainLoop()).ProcessFrame+=Sample;GD.Print("[Stage Probe] Loaded; observes native casts and whole-card settlement.");}
    static object? Field(object o,string name)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(o);
    static void Sample()
    {
        if(!Watching||NCombatRoom.Instance is not {} room)return;
        foreach(var node in room.CombatVfxContainer.GetChildren().OfType<NKarenStageCueVfx>())if(Casts.All(c=>c.Node!=node))Casts.Add(new(node));
        foreach(var seen in Casts){
            var node=seen.Node;if(!GodotObject.IsInstanceValid(node))continue;
            seen.Confirmed|=node.Confirmed;seen.Cards=Math.Max(seen.Cards,node.DeliveredCount);seen.Energy=Math.Max(seen.Energy,node.EnergyGain);
            seen.BackCorrect|=node.BackgroundLight.GetParent()==room.BackCombatVfxContainer;
            seen.Age=Math.Max(seen.Age,(float)Field(node,"_age")!);
            if(node.PlaybackOwned){seen.OwnedFrames++;var owner=(Player)Field(node,"_owner")!;
                if(owner.PlayerCombatState!=null)foreach(var card in PileType.Play.GetPile(owner).Cards)seen.PlayIndices.Add(card.CurrentPlayIndex);}
            if(node.Holding&&!node.PlaybackOwned&&seen.OwnedFrames>0)seen.SettledHeldFrames++;
            if(node.Holding&&(CombatManager.Instance.IsEnding||!CombatManager.Instance.IsInProgress))seen.EndingHeldFrames++;
            if(Field(node,"_source") is CardModel source&&NKarenShineDepleteVfx.IsAnimating(source)&&node.Holding)seen.DepletionHeldFrames++;
        }
    }
    internal static object Status(Player p)=>new{
        live=Casts.Count(c=>GodotObject.IsInstanceValid(c.Node)&&c.Node.IsInsideTree()),
        allLive=NCombatRoom.Instance?.CombatVfxContainer.GetChildren().OfType<NKarenStageCueVfx>().Count()??0,
        backLive=NCombatRoom.Instance?.BackCombatVfxContainer.GetChildren().Count(n=>n.Name.ToString().StartsWith("KarenStage"))??0,
        hudLive=NCombatRoom.Instance?.Ui.GetChildren().Count(n=>n.Name.ToString().StartsWith("KarenStage"))??0,
        ticket=p.Creature.GetPowerAmount<KarenTicketPower>(),act1=p.Creature.GetPowerAmount<KarenStarlightPower>(),act2=p.Creature.GetPowerAmount<KarenStarlight02Power>(),
        energy=p.PlayerCombatState?.Energy??0,promise=PromisePileManager.GetPromisePile(p).Cards.Count,shine=ShinePileManager.GetShinePile(p).Cards.Count,
        casts=Casts.Select(c=>new{kind=c.Kind,confirmed=c.Confirmed,cards=c.Cards,energy=c.Energy,age=c.Age,backCorrect=c.BackCorrect,
            ownedFrames=c.OwnedFrames,depletionHeldFrames=c.DepletionHeldFrames,settledHeldFrames=c.SettledHeldFrames,endingHeldFrames=c.EndingHeldFrames,playIndices=c.PlayIndices.Order().ToArray()})
    };
    internal static async Task Promise(Player p,int count){PromisePileManager.ClearPromisePileInternal(p);for(int i=0;i<count;i++)await PromisePileCmd.Add(p,p.Creature.CombatState!.CreateCard<KarenStrike>(p));}
}
public sealed class StageProbeCommand:AbstractConsoleCmd
{
    public override string CmdName=>"stage_probe";
    public override string Args=>"watch|status|promise N|speed normal/fast/instant|upgrade CARD|cancel";
    public override string Description=>"Temporary isolated stage VFX fixtures and observation.";
    public override bool IsNetworked=>false;
    public override CmdResult Process(Player? player,string[] args)
    {
        if(player==null||args.Length==0)return new(false,Args);
        switch(args[0]){
            case "watch":StageProbe.Casts.Clear();StageProbe.Watching=true;return new(true,"watch");
            case "status":return new(true,JsonSerializer.Serialize(StageProbe.Status(player)));
            case "promise" when args.Length==2&&int.TryParse(args[1],out int n):return new(StageProbe.Promise(player,n),true,"Promise fixture");
            case "weaken":foreach(var enemy in player.Creature.CombatState!.Enemies)enemy.SetCurrentHpInternal(1);return new(true,"Enemy HP fixture");
            case "speed" when args.Length==2:SaveManager.Instance.PrefsSave.FastMode=args[1] switch{"fast"=>FastModeType.Fast,"instant"=>FastModeType.Instant,_=>FastModeType.Normal};return new(true,"speed");
            case "upgrade" when args.Length==2:var card=player.PlayerCombatState.Hand.Cards.First(c=>c.Id.Entry==args[1]);CardCmd.Upgrade(card);return new(true,"upgraded");
            case "cancel":foreach(var node in NCombatRoom.Instance!.CombatVfxContainer.GetChildren().OfType<NKarenStageCueVfx>())node.QueueFree();return new(true,"cancelled presentation");
        }
        return new(false,Args);
    }
}
