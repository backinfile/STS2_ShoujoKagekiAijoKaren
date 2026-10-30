using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

internal static class ChainMetrics
{
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static int HistoryStart,MaxPulses,MaxGpu;
    static readonly Dictionary<ulong,object> Stars=new();
    static CardModel? Source;
    static CardModel New(Player p,string id)=>p.Creature.CombatState!.CreateCard(ModelDb.AllCards.Single(c=>c.Id.Entry==id),p);
    static async Task Add(Player p,string id,PileType pile,int count)
    {for(int i=0;i<count;i++)await CardPileCmd.AddGeneratedCardToCombat(New(p,id),pile,p);}
    internal static async Task Setup(Player p,CardModel card,string? mode)
    {
        MaxPulses=MaxGpu=0;Stars.Clear();Source=null;
        // Depleted cards persist across debug fights; reset only this isolated fixture's inventory.
        var depletedPile=ShinePileManager.GetShinePile(p);
        foreach(var old in depletedPile.Cards.ToArray())depletedPile.RemoveInternal(old);
        ShinePileManager.UpdateShineCardDisposedCount(p);
        string id=card.Id.Entry;
        if(id=="KAREN_REWRITE")
            for(int i=0;i<(mode=="stress"?4:3);i++)
            {
                var depleted=New(p,id);
                await CardPileCmd.AddGeneratedCardToCombat(depleted,PileType.Discard,p);
                await ShinePileManager.MoveToShinePile(depleted,new ThrowingPlayerChoiceContext());
            }
        if(id is "KAREN_BRIDGE" or "KAREN_GEER")
            await Add(p,"KAREN_STRIKE",KarenCustomEnum.PromisePile,id=="KAREN_BRIDGE"?3:2);
        if(id=="KAREN_STAR")
        {
            Source=New(p,"KAREN_SHINE_STRIKE");int shine=mode=="stress"?9:5;
            Source.SetShineMax(shine);Source.SetShineCurrent(shine);
            await CardPileCmd.AddGeneratedCardToCombat(Source,KarenCustomEnum.PromisePile,p);
        }
        if(id=="KAREN_STARLIGHT02_CARD")
        {
            Source=New(p,"KAREN_SHINE_STRIKE");Source.SetShineCurrent(3);
            await CardPileCmd.AddGeneratedCardToCombat(Source,PileType.Hand,p);
        }
        if(id=="KAREN_DROP_FUEL" || id=="KAREN_GEER")await Add(p,"KAREN_DEFEND",PileType.Draw,mode=="replay"?8:5);
        HistoryStart=CombatManager.Instance.History.CardPlaysStarted.Count();
    }
    internal static void Sample()
    {
        if(NCombatRoom.Instance is not {} room)return;
        var nodes=HeavyCardsProbe.Walk(room).ToArray();
        MaxGpu=Math.Max(MaxGpu,nodes.OfType<GpuParticles2D>().Where(n=>n.IsInsideTree() && (n.Emitting || n.GetParent()?.Name.ToString()=="EncoreParticleLayers")).Sum(n=>n.Amount));
        foreach(var n in nodes.OfType<NKarenStarVfx>())
        {
            int pulses=(n.GetType().GetField("_pulses",Flags)?.GetValue(n) as System.Collections.ICollection)?.Count??0;
            MaxPulses=Math.Max(MaxPulses,pulses);
            Stars[n.GetInstanceId()]=new{planned=n.PlannedCount,played=n.PlayedCount,reward=n.RewardConfirmed,backCorrect=n.BackgroundLight.GetParent()==room.BackCombatVfxContainer};
        }
    }
    internal static object Status(Player p)=>new{
        plays=CombatManager.Instance.History.CardPlaysStarted.Skip(HistoryStart).Select(e=>new{id=e.CardPlay.Card.Id.Entry,index=e.CardPlay.PlayIndex,count=e.CardPlay.PlayCount}).ToArray(),
        maxPulses=MaxPulses,maxGpu=MaxGpu,stars=Stars.Values,
        shinePile=ShinePileManager.GetShinePile(p).Cards.Select(c=>c.Id.Entry).ToArray(),
        sourceDepleted=Source!=null && Source.Pile==null && ShinePileManager.GetShinePile(p).Cards.Any(c=>c.Id==Source.Id),
        extraRewards=p.RunState.CurrentRoom is CombatRoom c && c.ExtraRewards.TryGetValue(p,out var rewards)?rewards.Count:0,
        starBackLive=NCombatRoom.Instance?.BackCombatVfxContainer.GetChildren().Count(n=>n.Name.ToString()=="KarenStarBackLight")??0,
        playPile=PileType.Play.GetPile(p).Cards.Count,draw=PileType.Draw.GetPile(p).Cards.Count,
        handIds=PileType.Hand.GetPile(p).Cards.Select(c=>c.Id.Entry).ToArray()
    };
}
