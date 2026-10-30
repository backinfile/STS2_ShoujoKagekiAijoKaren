using System;
using ShoujoKagekiAijoKaren;
using ShoujoKagekiAijoKaren.src.Models.Cards;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using System.Reflection;
using ShoujoKagekiAijoKaren.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.ExtraReplaySystem;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

[ModInitializer(nameof(Initialize))]
public static class HeavyCardsProbe
{
    internal static readonly HashSet<string> Types = ["NKarenContinueVfx","NKarenStarVfx","NKarenBridgeVfx","NKarenGeerVfx","NKarenDropFuelVfx","NKarenStageCueVfx","NKarenUnforgottenShineVfx","NKarenNononVfx","NKarenShineBarrageVfx","NKarenBackToBackVfx","NKarenBaseballVfx","NKarenJealousyVfx","NKarenStageLightVfx","NKarenStageLightImpactVfx","NKarenStageAttackVfx"];
    internal static readonly Dictionary<string,int> Spawned=new(),Exited=new();
    internal static CardModel? Card;
    internal static int Draws,MaxScars,MaxParticles,MaxEffects,MaxManualParticles;
    internal static bool Watching, ReuseStable;
    internal static int PoseFrames;
    internal static float MaxAttackOffset;
    internal static int FatalCueFrames,ReturnCueFrames,TotalContacts;
    internal static float MaxProfileGrowth,MaxGroupSpan,MaxTargetWidth;
    internal static int TotalGroupWaves;
    internal static readonly List<object> GroupDirections=new();
    internal static readonly HashSet<(ulong,int)> SeenDirections=new();
    internal static readonly Dictionary<ulong,int> GroupCounts=new();
    internal static readonly Dictionary<ulong,int> ContactCounts=new();
    private static readonly FieldInfo? StageHits=typeof(NKarenStageAttackVfx).GetField("_hits",BindingFlags.Instance|BindingFlags.NonPublic);
    private static readonly PropertyInfo? ProfileGrowth=typeof(NKarenStageAttackVfx).GetProperty("Growth",BindingFlags.Instance|BindingFlags.NonPublic);
    private static readonly PropertyInfo? PoseProperty=typeof(SNCreatureVisuals).GetProperty("IsAttackPoseActive");
    private static readonly FieldInfo? PoseOffset=typeof(SNCreatureVisuals).GetField("_attackCanvasOffset",BindingFlags.Instance|BindingFlags.NonPublic);
    private static readonly FieldInfo? NononParticles=typeof(NKarenNononVfx).GetField("_particles",BindingFlags.Instance|BindingFlags.NonPublic);
    internal static IEnumerable<Node> Walk(Node node)
    { yield return node;foreach(Node child in node.GetChildren())foreach(var n in Walk(child))yield return n; }
    public static void Initialize()
    {
        var tree=(SceneTree)Engine.GetMainLoop();
        tree.NodeAdded+=node=> {
            string type=node.GetType().Name;
            if(!Watching || !Types.Contains(type))return;
            Spawned[type]=Spawned.GetValueOrDefault(type)+1;
            if(node is CanvasItem item)item.Draw+=()=>Draws++;
            node.TreeExiting+=()=>Exited[type]=Exited.GetValueOrDefault(type)+1;
        };
        tree.ProcessFrame+=()=>Sample();
        GD.Print("[Heavy Cards Probe] loaded");
    }
    internal static void Sample()
    {
        if(!Watching || NCombatRoom.Instance is not {} room)return;
        if (Card?.Owner?.Creature is {} creature && room.GetCreatureNode(creature)?.Visuals is SNCreatureVisuals actor)
        {
            if (PoseProperty?.GetValue(actor) is true) PoseFrames++;
            if (PoseOffset?.GetValue(actor) is Vector2 offset) MaxAttackOffset=Math.Max(MaxAttackOffset,offset.Length());
        }
        ChainMetrics.Sample();
        var nodes=Walk(room).ToArray();
        MaxScars=Math.Max(MaxScars,nodes.OfType<CanvasGroup>().Where(n=>n.Name=="RetainedCuts").Sum(n=>n.GetChildCount()));
        MaxParticles=Math.Max(MaxParticles,nodes.OfType<CpuParticles2D>().Where(n=>Types.Contains(n.GetParent().GetType().Name)).Sum(n=>n.Amount));
        MaxManualParticles=Math.Max(MaxManualParticles,nodes.OfType<NKarenNononVfx>().Sum(n=>(NononParticles?.GetValue(n) as System.Collections.ICollection)?.Count ?? 0));
        foreach(var effect in nodes.Where(n=>n.GetType().Name=="NKarenStageAttackVfx"))
        {
            var type=effect.GetType();
            int contacts=StageHits?.GetValue(effect) is int value ? value : 0;
            ulong identity=effect.GetInstanceId();
            int waves=type.GetField("_groupWaveCount",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(effect) is int waveCount ? waveCount : 0;
            TotalGroupWaves+=Math.Max(0,waves-GroupCounts.GetValueOrDefault(identity));GroupCounts[identity]=waves;
            if(type.GetField("_groupSpan",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(effect) is float span)MaxGroupSpan=Math.Max(MaxGroupSpan,span);
            if(type.GetField("_maxTargetWidth",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(effect) is float width)MaxTargetWidth=Math.Max(MaxTargetWidth,width);
            if(type.GetField("_groupWaves",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(effect) is Array paths)
                foreach(var path in paths)
                {
                    var t=path!.GetType();
                    if(t.GetField("Active")?.GetValue(path) is not true)continue;
                    int round=(int)t.GetField("Round")!.GetValue(path)!;
                    if(!SeenDirections.Add((identity,round)))continue;
                    var from=(Vector2)t.GetField("From")!.GetValue(path)!;
                    var to=(Vector2)t.GetField("To")!.GetValue(path)!;
                    float side=(float)type.GetField("_side",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(effect)!;
                    GroupDirections.Add(new { round, side, dx=to.X-from.X, dy=to.Y-from.Y });
                }
            TotalContacts+=Math.Max(0,contacts-ContactCounts.GetValueOrDefault(identity));ContactCounts[identity]=contacts;
            if(ProfileGrowth?.GetValue(effect) is float growth)MaxProfileGrowth=Math.Max(MaxProfileGrowth,growth);
            if(type.GetField("_fatalAt",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(effect) is float fatal && fatal>=0)FatalCueFrames++;
            if(type.GetField("_returnAt",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(effect) is float returned && returned>=0)ReturnCueFrames++;
        }
        MaxEffects=Math.Max(MaxEffects,nodes.Count(n=>Types.Contains(n.GetType().Name)));
    }
}
public sealed class HeavyCardsCmd:AbstractConsoleCmd
{
    public override string CmdName=>"heavy_probe";
    public override string Args=>"seed ID [upgrade|stress|stock] | quality Full|Low|Off | speed normal|fast|instant | status | lethal | focus | unfocus";
    public override string Description=>"Heavy card real-play fixture and passive visual observation";
    public override bool IsNetworked=>false;
    public override CmdResult Process(Player? player,string[] args)
    {
        if(player==null || args.Length==0)return new(false,Args);
        var room=NCombatRoom.Instance;
        switch(args[0])
        {
            case "quality":KarenModConfig.VfxLevel=Enum.Parse<KarenVfxLevel>(args[1]);return new(true,args[1]);
            case "speed":SaveManager.Instance.PrefsSave.FastMode=args[1] switch {"fast"=>FastModeType.Fast,"instant"=>FastModeType.Instant,_=>FastModeType.Normal};return new(true,args[1]);
            case "seed":return new(Seed(player,args[1],args.ElementAtOrDefault(2)),true,"Real card ready");
            case "budget":
                var targets=room!.CreatureNodes.Take(4).ToArray();
                for(int pass=0;pass<2;pass++){
                    var first=HeavyCardsProbe.Walk(room).OfType<Sprite2D>().Where(n=>n.GetParent().Name.ToString() is "RetainedCuts" or "FreshHits").Select(n=>n.GetInstanceId()).ToHashSet();
                    for(int i=0;i<64;i++){
                        var effect=NKarenUnforgottenShineVfx.Create(player.Creature,targets[i%targets.Length].Entity);
                        if(effect!=null)room.CombatVfxContainer.AddChild(effect);
                    }
                    var last=HeavyCardsProbe.Walk(room).OfType<Sprite2D>().Where(n=>n.GetParent().Name.ToString() is "RetainedCuts" or "FreshHits").Select(n=>n.GetInstanceId()).ToHashSet();
                    if(pass==1)HeavyCardsProbe.ReuseStable=first.SetEquals(last);
                }
                return new(true,"Presentation-only budget fixture; 128 factory calls across four real creature views");
            case "mirror":
                room!.GetCreatureNode(player.Creature)!.Body.Scale *= new Vector2(-1,1);
                return new(true,"Mirrored actor");
            case "clearfx":
                foreach(var node in HeavyCardsProbe.Walk(room!).Where(n=>n.GetType().Name is "NKarenStageAttackVfx" or "NKarenStarVfx").ToArray())node.QueueFree();
                return new(true,"Removed only attack presentation");
            case "left":
            {
                var ownerView=room!.GetCreatureNode(player.Creature)!;
                var enemies=room.CreatureNodes.Where(n=>n.Entity.IsMonster).ToArray();
                float axis=ownerView.GlobalPosition.X+enemies.Max(n=>n.GlobalPosition.X);
                ownerView.GlobalPosition=new Vector2(axis-ownerView.GlobalPosition.X,ownerView.GlobalPosition.Y);
                foreach(var enemy in enemies)enemy.GlobalPosition=new Vector2(axis-enemy.GlobalPosition.X,enemy.GlobalPosition.Y);
                var leftSprite=ownerView.Visuals.GetNode<Sprite2D>("%Visuals");
                leftSprite.Scale=new Vector2(-Math.Abs(leftSprite.Scale.X),leftSprite.Scale.Y);
                return new(true,"Target left of attacker");
            }
            case "size":
                room!.CreatureNodes.First(n=>n.Entity.IsMonster).Visuals.Scale*=float.Parse(args[1],System.Globalization.CultureInfo.InvariantCulture);
                return new(true,"Scaled target visuals");
            case "lethal":room!.CreatureNodes.First(n=>n.Entity.IsMonster).Entity.SetCurrentHpInternal(1);return new(true,"HP1");
            case "focus":((KarenCarryingGuilt)HeavyCardsProbe.Card!).OnCreatureHover(room!.CreatureNodes.First(n=>n.Entity.IsMonster));return new(true,"Focused");
            case "unfocus":((KarenCarryingGuilt)HeavyCardsProbe.Card!).OnCreatureUnhover(room!.CreatureNodes.First(n=>n.Entity.IsMonster));return new(true,"Unfocused");
            case "status":
                HeavyCardsProbe.Sample();
                var sprite=room?.GetCreatureNode(player.Creature)?.Visuals.GetNodeOrNull<Sprite2D>("%Visuals");
                var nodes=room==null?Array.Empty<Node>():HeavyCardsProbe.Walk(room).ToArray();
                return new(true,JsonSerializer.Serialize(new {chain=ChainMetrics.Status(player),bodyZ=sprite?.ZIndex,bodyRelative=sprite?.ZAsRelative,spawned=HeavyCardsProbe.Spawned,exited=HeavyCardsProbe.Exited,draws=HeavyCardsProbe.Draws,
                    reuseStable=HeavyCardsProbe.ReuseStable,poseFrames=HeavyCardsProbe.PoseFrames,maxAttackOffset=HeavyCardsProbe.MaxAttackOffset,poseActive=room?.GetCreatureNode(player.Creature)?.Visuals is SNCreatureVisuals actor && typeof(SNCreatureVisuals).GetProperty("IsAttackPoseActive")?.GetValue(actor) is true,maxScars=HeavyCardsProbe.MaxScars,maxParticles=HeavyCardsProbe.MaxParticles,maxEffects=HeavyCardsProbe.MaxEffects,maxManualParticles=HeavyCardsProbe.MaxManualParticles,fatalCueFrames=HeavyCardsProbe.FatalCueFrames,returnCueFrames=HeavyCardsProbe.ReturnCueFrames,totalContacts=HeavyCardsProbe.TotalContacts,maxProfileGrowth=HeavyCardsProbe.MaxProfileGrowth,totalGroupWaves=HeavyCardsProbe.TotalGroupWaves,groupDirections=HeavyCardsProbe.GroupDirections,maxGroupSpan=HeavyCardsProbe.MaxGroupSpan,maxTargetWidth=HeavyCardsProbe.MaxTargetWidth,
                    effects=nodes.Where(n=>HeavyCardsProbe.Types.Contains(n.GetType().Name)).Select(n=>new{type=n.GetType().Name,id=n.GetInstanceId(),children=n.GetChildCount()}),
                    scars=nodes.OfType<CanvasGroup>().Where(n=>n.Name=="RetainedCuts").Sum(n=>n.GetChildCount()),
                    hand=PileType.Hand.GetPile(player).Cards.Count,promise=PromisePileManager.GetCount(player),shine=HeavyCardsProbe.Card?.GetShineValue(),
                    powers=player.Creature.Powers.Select(p=>new{id=p.Id.Entry,amount=p.Amount})}));
            default:return new(false,Args);
        }
    }
    private static async Task Seed(Player player,string id,string? mode)
    {
        HeavyCardsProbe.Watching=false;
        var all=new[]{PileType.Hand,PileType.Draw,PileType.Discard,PileType.Exhaust,PileType.Play,KarenCustomEnum.PromisePile}
            .SelectMany(t=>t.GetPile(player).Cards).Distinct().ToArray();
        foreach(var old in all)await CardPileCmd.RemoveFromCombat(old);
        var state=CombatManager.Instance.DebugOnlyGetState()!;
        var card=state.CreateCard(ModelDb.AllCards.Single(c=>c.Id.Entry==id),player);
        if(mode=="upgrade"){card.UpgradeInternal();card.FinalizeUpgradeInternal();}
        if(mode=="growth")
        {
            if(id=="KAREN_CONTINUE02")for(int i=0;i<12;i++){card.UpgradeInternal();card.FinalizeUpgradeInternal();}
            else card.DynamicVars.Damage.UpgradeValueBy(120-card.DynamicVars.Damage.BaseValue);
        }
        if(mode=="last")card.SetShineCurrent(1);
        if(mode=="replay")card.SetExtraReplayCountForNextPlay(2);
        if(id=="KAREN_BACK_TO_BACK_CARD"){
            int count=mode=="stock"?30:8;
            for(int i=0;i<count;i++)await CardPileCmd.AddGeneratedCardToCombat(state.CreateCard(ModelDb.AllCards.Single(c=>c.Id.Entry=="KAREN_STRIKE"),player),KarenCustomEnum.PromisePile,player);
        }
        await ChainMetrics.Setup(player,card,mode);
        await CardPileCmd.AddGeneratedCardToCombat(card,PileType.Hand,player);
        HeavyCardsProbe.TotalGroupWaves=0;HeavyCardsProbe.MaxGroupSpan=HeavyCardsProbe.MaxTargetWidth=0;HeavyCardsProbe.GroupDirections.Clear();HeavyCardsProbe.SeenDirections.Clear();HeavyCardsProbe.GroupCounts.Clear();HeavyCardsProbe.TotalContacts=0;HeavyCardsProbe.MaxProfileGrowth=0;HeavyCardsProbe.ContactCounts.Clear();HeavyCardsProbe.FatalCueFrames=HeavyCardsProbe.ReturnCueFrames=0;HeavyCardsProbe.MaxManualParticles=0;HeavyCardsProbe.ReuseStable=false;HeavyCardsProbe.PoseFrames=0;HeavyCardsProbe.MaxAttackOffset=0;HeavyCardsProbe.Card=card;HeavyCardsProbe.Spawned.Clear();HeavyCardsProbe.Exited.Clear();
        HeavyCardsProbe.Draws=HeavyCardsProbe.MaxScars=HeavyCardsProbe.MaxParticles=HeavyCardsProbe.MaxEffects=0;HeavyCardsProbe.Watching=true;
    }
}
