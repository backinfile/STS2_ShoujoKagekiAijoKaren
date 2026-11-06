using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;
using MegaCrit.Sts2.Core.Nodes;
using ShoujoKagekiAijoKaren;
using ShoujoKagekiAijoKaren.src.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.basic;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;

// Only the isolated fixture DLL contains this command and the observer below.
public sealed class PileEmptyCmd : AbstractConsoleCmd
{
    public override string CmdName => "pile_empty";
    public override string Args => "setup mode count hand inactive; op action [count]; snapshot";
    public override string Description => "Observe native empty-pile transitions and Stage Is Waiting gains.";
    public override bool IsNetworked => false;
    internal static Player? Player;
    internal static int Hits;
    internal static string State="idle", Mode="normal", Reentry="";
    private static CardModel? _last;
    private static PileType _lastPile;
    private static Label? _hud;
    private static readonly PlayerChoiceContext Context=new BlockingPlayerChoiceContext();
    private static CardPile Active(Player p)=>PromisePileManager.IsVoidMode(p) ? p.PlayerCombatState!.DrawPile : PromisePileManager.GetPromisePile(p);
    public override CmdResult Process(Player? player,string[] args)
    {
        if(args.Length==1 && args[0]=="snapshot")return new(true,JsonSerializer.Serialize(Snapshot()));
        if(player==null || args.Length<2)return new(false,"A combat player and operation are required.");
        return new(Run(player,args),true,"running");
    }
    internal static object Snapshot()
    {
        var p=Player;var combat=p?.PlayerCombatState;
        return new { mode=Mode,status=State,hits=Hits,energy=combat?.Energy,active=combat==null?0:Active(p!).Cards.Count,
            promise=combat==null?0:PromisePileManager.GetPromisePile(p!).Cards.Count,draw=combat?.DrawPile.Cards.Count,
            hand=combat?.Hand.Cards.Count,discard=combat?.DiscardPile.Cards.Count,exhaust=combat?.ExhaustPile.Cards.Count,
            ending=CombatManager.Instance.IsOverOrEnding,
            handCards=combat?.Hand.Cards.Select(c=>new { id=c.Id.Entry,upgraded=c.IsUpgraded,exhaust=c.Keywords.Contains(CardKeyword.Exhaust) }).ToArray() };
    }
    private static async Task Run(Player p,string[] args)
    {
        try
        {
            if(args[0]=="setup") { await Setup(p,args[1],int.Parse(args[2]),int.Parse(args[3]),int.Parse(args[4]));return; }
            State="running";
            var op=args[1];int n=args.Length>2?int.Parse(args[2]):999;
            var pile=Active(p);var cards=pile.Cards.Take(n).ToArray();
            if(cards.Length>0){_last=cards[^1];_lastPile=pile.Type;}
            switch(op)
            {
                case "reorder":await CardPileCmd.Add(cards,pile,CardPilePosition.Top);break;
                case "stack-power":await PowerCmd.Apply<KarenStageIsWaitingPower>(Context,p.Creature,2,p.Creature,null);break;
                case "update":await PromisePileManager.UpdatePowerAsync(p);break;
                case "draw-win":await PromisePileCmd.Draw(Context,p);await CombatManager.Instance.CheckWinCondition();break;
                case "draw":await PromisePileCmd.Draw(Context,p,n==999?1:n);break;
                case "native-draw":for(int i=0;i<(n==999?1:n);i++)await CardPileCmd.Draw(Context,p);break;
                case "transfer":await CardPileCmd.Add(cards,PileType.Hand);break;
                case "discard-hand":await CardCmd.Discard(Context,p.PlayerCombatState!.Hand.Cards.ToArray());break;
                case "discard":await PromisePileCmd.DiscardAll(Context,p);break;
                case "discard-one":await PromisePileCmd.DiscardCards(Context,p,cards.ToList());break;
                case "exhaust":foreach(var card in cards)await CardCmd.Exhaust(Context,card);break;
                case "remove":await CardPileCmd.RemoveFromCombat(cards);break;
                case "to-draw":await PromisePileCmd.MoveAllToDrawPile(p);break;
                case "select":await PromisePileCmd.SelectedToHand(Context,p,n==999?cards.Length:n);break;
                case "select-play":await PromisePileCmd.SelectAndAutoPlayFromPromisePile(Context,p);break;
                case "autoplay":
                    await (Task)typeof(PromisePileCmd).GetMethod("AutoPlayFromPromisePile",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[Context,p,n==999?cards.Length:n])!;break;
                case "switch":await PromisePileCmd.SwitchHand(Context,p);break;
                case "refill":for(int i=0;i<(n==999?1:n);i++)await PromisePileCmd.Add(p,p.Creature.CombatState!.CreateCard<KarenDefend>(p));break;
                case "refill-native":await CardPileCmd.Add(p.Creature.CombatState!.CreateCard<KarenDefend>(p),Active(p));break;
                case "refill-direct":
                    Active(p).AddInternal(p.Creature.CombatState!.CreateCard<KarenDefend>(p));Active(p).InvokeContentsChanged();await PromisePileManager.UpdatePowerAsync(p);break;
                case "refill-token":await PromisePileCmd.AddToken<KarenDefend>(p,p.Creature.CombatState!,n==999?1:n);break;
                case "refill-discard":await PromisePileCmd.AddFromDiscard(Context,p,n==999?1:n);break;
                case "refill-hand":await PromisePileCmd.AddFromHand(Context,p,n==999?1:n,ModelDb.Card<KarenDefend>());break;
                case "inactive-clear":
                    var inactive=PromisePileManager.IsVoidMode(p)?PromisePileManager.GetPromisePile(p):p.PlayerCombatState!.DrawPile;
                    await CardPileCmd.RemoveFromCombat(inactive.Cards.ToArray());break;
                case "repeat-hook":await RepeatHook(p);await RepeatHook(p);break;
                case "reenter-same":Reentry="same";break;
                case "reenter-refill":Reentry="refill";break;
                case "play-hand":
                    var defend=p.Creature.CombatState!.CreateCard<KarenDefend>(p);await CardPileCmd.Add(defend,PileType.Hand);await CardCmd.AutoPlay(Context,defend,null);break;
                case "cast-void":
                    var empty=p.Creature.CombatState!.CreateCard<KarenVoid>(p);await CardPileCmd.Add(empty,PileType.Hand);await CardCmd.AutoPlay(Context,empty,null);break;
                case "cast-infinite":
                    var infinite=p.Creature.CombatState!.CreateCard<KarenStageReproduce>(p);await CardPileCmd.Add(infinite,PileType.Hand);await CardCmd.AutoPlay(Context,infinite,null);break;
                case "empty-noops":
                    await PromisePileCmd.Draw(Context,p);await PromisePileCmd.DiscardAll(Context,p);await PromisePileCmd.MoveAllToDrawPile(p);await CardPileCmd.Add(Array.Empty<CardModel>(),PileType.Hand);break;
                case "win":
                    foreach(var enemy in p.Creature.CombatState!.HittableEnemies.ToArray())await CreatureCmd.Kill(enemy);
                    await CombatManager.Instance.CheckWinCondition();break;
                default:throw new ArgumentException("Unknown empty-pile operation: "+op);
            }
            State="finished";
        }
        catch(Exception e){State="error:"+e.GetType().Name+":"+e.Message;MainFile.Logger.Error("[PileEmpty] "+e);}
    }
    private static async Task Setup(Player p,string mode,int count,int hand,int inactive)
    {
        Player=p;State="setup";Reentry="";_last=null;Mode=mode;
        foreach(var power in p.Creature.Powers.ToArray())await PowerCmd.Remove(power);
        await CardPileCmd.RemoveFromCombat(p.PlayerCombatState!.AllCards.ToArray());
        await PromisePileManager.UpdatePowerAsync(p);
        if(mode.Contains("void"))await PromisePileManager.EnterMode(p,PromisePileMode.Void);
        for(int i=0;i<count;i++)
        {
            CardModel card=mode.Contains("lethal")?p.Creature.CombatState!.CreateCard<KarenCry>(p):p.Creature.CombatState!.CreateCard<KarenDefend>(p);
            await PromisePileCmd.Add(p,card);
        }
        for(int i=0;i<hand;i++)await CardPileCmd.Add(p.Creature.CombatState!.CreateCard<KarenDefend>(p),PileType.Hand);
        var other=mode.Contains("void")?PromisePileManager.GetPromisePile(p):p.PlayerCombatState!.DrawPile;
        for(int i=0;i<inactive;i++){other.AddInternal(p.Creature.CombatState!.CreateCard<KarenDefend>(p));}other.InvokeContentsChanged();
        if(mode.Contains("infinite"))await PromisePileManager.EnterMode(p,PromisePileMode.InfiniteReinforcement);
        if(mode.Contains("burn"))await PromisePileManager.EnterMode(p,PromisePileMode.Burn);
        if(mode.Contains("past"))await PromisePileManager.EnterPastAndFutureMode(p,2);
        await PowerCmd.Apply<KarenStageIsWaitingPower>(Context,p.Creature,1,p.Creature,null);
        p.PlayerCombatState!.Energy=3;Hits=0;State="ready";
        foreach(var enemy in p.Creature.CombatState!.HittableEnemies){enemy.SetMaxHpInternal(9999);enemy.SetCurrentHpInternal(9999);}
        if(mode.Contains("lethal"))
        {
            await PowerCmd.Apply<JuggernautPower>(Context,p.Creature,10,p.Creature,null);
            foreach(var enemy in p.Creature.CombatState!.HittableEnemies)enemy.SetCurrentHpInternal(1);
        }
        if(!GodotObject.IsInstanceValid(_hud))
        {
            _hud=new Label { Name="EmptyPileAudit",Position=new Vector2(24,24),ZIndex=4090,MouseFilter=Control.MouseFilterEnum.Ignore };
            _hud.AddThemeFontSizeOverride("font_size",20);_hud.AddThemeConstantOverride("outline_size",8);_hud.AddThemeColorOverride("font_outline_color",Colors.Black);
            NGame.Instance.AddChild(_hud);((SceneTree)Engine.GetMainLoop()).ProcessFrame+=()=>{if(GodotObject.IsInstanceValid(_hud))_hud.Text=$"{Mode} | {State}\nEmpty callbacks: {Hits} | Energy: {Player?.PlayerCombatState?.Energy}";};
        }
    }
    private static Task RepeatHook(Player p)=>_last==null?Task.CompletedTask:Hook.AfterCardChangedPiles(p.RunState,p.Creature.CombatState,_last,_lastPile,null);
    internal static async Task ObserveAsync(Task original,KarenStageIsWaitingPower power)
    {
        await original;
        if(power.Owner.Player!=Player || Reentry.Length==0)return;
        var mode=Reentry;Reentry="";
        if(mode=="same")await RepeatHook(Player!);
        if(mode=="refill")
        {
            await PromisePileCmd.Add(Player!,Player!.Creature.CombatState!.CreateCard<KarenDefend>(Player));
            await PromisePileCmd.Draw(Context,Player);
        }
    }
}
[HarmonyPatch(typeof(KarenStageIsWaitingPower),nameof(KarenStageIsWaitingPower.OnPromisePileEmpty))]
internal static class EmptyPowerObserver
{
    private static void Prefix(KarenStageIsWaitingPower __instance)
    {
        if(__instance.Owner.Player==PileEmptyCmd.Player && ++PileEmptyCmd.Hits>8)throw new InvalidOperationException("Empty callback recursion exceeded fixture safety limit");
    }
    private static void Postfix(KarenStageIsWaitingPower __instance,ref Task __result)=>__result=PileEmptyCmd.ObserveAsync(__result,__instance);
}