using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.ExtraReplaySystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

// Test-only fixture and observation. All effects resolve through native MCP plays.
public sealed class BoundaryProbeCommand : AbstractConsoleCmd
{
    public override string CmdName => "boundary";
    public override string Args => "seed Pile IDs|replay/shine Pile ID N|watch|status";
    public override string Description => "Controlled recursive VFX boundary tests";
    public override bool IsNetworked => false;
    private static NCombatRoom? _room;
    private static readonly HashSet<ulong> Seen = new();
    private static readonly List<object> Spawns = new();
    private static SceneTree? _tree;
    private static int _maxStages, _maxSpots, _duplicateFrames;
    private static PileType Pile(string name) => name == "Promise" ? KarenCustomEnum.PromisePile : Enum.Parse<PileType>(name);
    private static object Card(CardModel c) => new { id=c.Id.Entry, identity=RuntimeHelpers.GetHashCode(c), shine=c.GetShineValueRounded() };
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player?.PlayerCombatState == null || args.Length == 0) return new(false, Args);
        if(args[0]=="weakenEnemy" && args.Length==2){
            var enemy=player.Creature.CombatState!.Enemies.Single(c=>c.CombatId==uint.Parse(args[1]));
            enemy.SetCurrentHpInternal(1);return new(true,"One-HP fixture; lethal hit still uses an actual card");
        }
        if(args[0]=="energy" && args.Length==2)return new(PlayerCmd.SetEnergy(int.Parse(args[1]),player),true,"Set exact energy");
        if (args[0] == "seed" && args.Length == 3)
        {
            var cards=args[2].Split(',').Select(id=>player.Creature.CombatState!.CreateCard(ModelDb.AllCards.Single(c=>c.Id.Entry==id),player)).ToArray();
            return new(CardPileCmd.AddGeneratedCardsToCombat(cards,Pile(args[1]),player),true,"Seeded ordered actual cards");
        }
        if (args[0] is "replay" or "shine" && args.Length == 4)
        {
            var cards=Pile(args[1]).GetPile(player).Cards.Where(c=>c.Id.Entry==args[2]).ToArray();
            foreach(var card in cards)
                if(args[0]=="replay")card.SetExtraReplayCountForNextPlay(int.Parse(args[3]));
                else card.SetShineCurrent(int.Parse(args[3]));
            return new(true,$"Configured {cards.Length} cards");
        }
        if(args[0]=="watch")
        {
            if(_tree!=null&&GodotObject.IsInstanceValid(_tree))_tree.ProcessFrame-=Sample;
            _room=NCombatRoom.Instance;_tree=_room!.GetTree();Seen.Clear();Spawns.Clear();
            _maxStages=_maxSpots=_duplicateFrames=0;_tree.ProcessFrame+=Sample;
            return new(true,"Watching actual bridge nodes");
        }
        if(args[0]!="status")return new(false,Args);
        var room=NCombatRoom.Instance;
        var bridges=room?.BackCombatVfxContainer.GetChildren().OfType<NKarenBridgeVfx>().Where(n=>!n.IsQueuedForDeletion()).ToArray()??[];
        return new(true,JsonSerializer.Serialize(new{
            maxStages=_maxStages,maxSpots=_maxSpots,duplicateSpotlightFrames=_duplicateFrames,spawns=Spawns,
            activeBridges=bridges.Length,play=PileType.Play.GetPile(player).Cards.Select(Card),
            promise=KarenCustomEnum.PromisePile.GetPile(player).Cards.Select(Card),
            draw=PileType.Draw.GetPile(player).Cards.Select(Card),discard=PileType.Discard.GetPile(player).Cards.Select(Card),
            hand=PileType.Hand.GetPile(player).Cards.Select(Card),shine=ShinePileManager.GetShinePile(player).Cards.Select(Card),
            exhaust=PileType.Exhaust.GetPile(player).Cards.Select(Card),
            started=CombatManager.Instance.History.CardPlaysStarted.Select(e=>new{id=e.CardPlay.Card.Id.Entry,identity=RuntimeHelpers.GetHashCode(e.CardPlay.Card),auto=e.CardPlay.IsAutoPlay,target=e.CardPlay.Target?.CombatId}),
            finished=CombatManager.Instance.History.CardPlaysFinished.Select(e=>new{id=e.CardPlay.Card.Id.Entry,identity=RuntimeHelpers.GetHashCode(e.CardPlay.Card),auto=e.CardPlay.IsAutoPlay,target=e.CardPlay.Target?.CombatId})
        }));
    }
    private static void Sample()
    {
        if(_room==null||!GodotObject.IsInstanceValid(_room)||NCombatRoom.Instance!=_room)return;
        var nodes=_room.BackCombatVfxContainer.GetChildren().OfType<NKarenBridgeVfx>().Where(n=>!n.IsQueuedForDeletion()).ToArray();
        _maxStages=Math.Max(_maxStages,nodes.Count(n=>!n.IsSpotlight&&n.HasCards));
        _maxSpots=Math.Max(_maxSpots,nodes.Count(n=>n.IsSpotlight));
        var cardField=typeof(NKarenBridgeVfx).GetField("_card",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
        // Duplicated simultaneous spotlights for one model are suspicious in non-replay casts.
        if(nodes.Where(n=>n.IsSpotlight).GroupBy(n=>cardField.GetValue(n)).Any(g=>g.Count()>1))_duplicateFrames++;
        foreach(var node in nodes)if(Seen.Add(node.GetInstanceId()))
            Spawns.Add(new{spot=node.IsSpotlight,hasCards=node.HasCards,fromDraw=node.FromDraw,
                card=cardField.GetValue(node) is CardModel card?Card(card):null,ms=Time.GetTicksMsec()});
    }
}
