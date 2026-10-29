using System;
using System.Collections.Generic;
using System.Linq;
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
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using ShoujoKagekiAijoKaren;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.basic;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.token;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.Rewards;
using ShoujoKagekiAijoKaren.src.Models.Cards;

[ModInitializer(nameof(Initialize))]
public static class BoundaryReviewEntry
{
    public static void Initialize() => new Harmony("KarenBoundaryReview").PatchAll();
}

public sealed class BoundaryReviewCmd : AbstractConsoleCmd
{
    public override string CmdName => "review_boundary";
    public override string Args => "setup empty|void|tower|batch|infinite|swap|reward; draw; refill; run; offer; normal; snapshot";
    public override string Description => "Isolated review regression fixtures, including a bounded baseline loop guard.";
    public override bool IsNetworked => false;
    internal static Player? Player;
    internal static string Case = "idle", Status = "idle";
    internal static int FailedMoves;
    private static int _choicesBefore, _deckBefore;
    private static Label? _hud;
    private static readonly PlayerChoiceContext Context = new BlockingPlayerChoiceContext();

    public override CmdResult Process(Player? player, string[] args)
    {
        if (args.Length == 1 && args[0] == "snapshot") return new(true, JsonSerializer.Serialize(Snapshot()));
        if (player == null || args.Length == 0) return new(false, "A combat player and operation are required.");
        return new(Run(player,args),true,"running");
    }
    internal static object Snapshot()
    {
        var p=Player;
        var choices=p?.RunState.CurrentMapPointHistoryEntry?.GetEntry(p.NetId).CardChoices;
        return new { @case=Case,status=Status,failedMoves=FailedMoves,energy=p?.PlayerCombatState?.Energy,
            hand=p?.PlayerCombatState?.Hand.Cards.Count,
            promise=p?.PlayerCombatState == null ? 0 : PromisePileManager.GetCount(p),
            ending=CombatManager.Instance.IsOverOrEnding,
            choicesDelta=(choices?.Count ?? 0)-_choicesBefore,
            deckDelta=(p?.Deck.Cards.Count ?? 0)-_deckBefore,
            choices=choices?.Skip(_choicesBefore).Select(c=>new { card=c.Card.Id.Entry,picked=c.wasPicked }).ToArray() };
    }
    private static void EnsureHud()
    {
        if (GodotObject.IsInstanceValid(_hud)) return;
        _hud=new Label { Name="ReviewBoundaryAudit",Position=new Vector2(24,24),ZIndex=4090,MouseFilter=Control.MouseFilterEnum.Ignore };
        _hud.AddThemeFontSizeOverride("font_size",20);
        _hud.AddThemeColorOverride("font_outline_color",Colors.Black);
        _hud.AddThemeConstantOverride("outline_size",8);
        NGame.Instance.AddChild(_hud);
        ((SceneTree)Engine.GetMainLoop()).ProcessFrame += UpdateHud;
    }
    private static void UpdateHud()
    {
        if (!GodotObject.IsInstanceValid(_hud) || Player == null) return;
        var choices=Player.RunState.CurrentMapPointHistoryEntry?.GetEntry(Player.NetId).CardChoices.Count ?? 0;
        _hud!.Text=$"{Case} | {Status}\nEnergy: {Player.PlayerCombatState?.Energy}  Failed moves: {FailedMoves}\nChoice records: {choices-_choicesBefore}  Deck gained: {Player.Deck.Cards.Count-_deckBefore}";
    }
    private static async Task Run(Player p,string[] args)
    {
        try
        {
            if (args[0]=="setup") { await Setup(p,args[1]); return; }
            Status="running";
            switch(args[0])
            {
                case "draw": await PromisePileCmd.Draw(Context,p); break;
                case "refill": await AddPromise<KarenDefend>(p); break;
                case "offer":
                    var rewards=new List<Reward>();
                    for(int i=0;i<3;i++) rewards.Add(new KarenShineCardReward(p.RunState.CreateCard(ModelDb.Card<KarenPotato>(),p),p));
                    Status="selecting";
                    await RewardsCmd.OfferCustom(p,rewards);
                    break;
                case "normal":
                    Status="selecting";
                    var cards=Enumerable.Range(0,3).Select(_=>p.RunState.CreateCard(ModelDb.Card<KarenPotato>(),p)).ToArray();
                    await RewardsCmd.OfferCustom(p,[new CardReward(cards,(CardCreationSource)0,p,null!)]);
                    break;
                case "run":
                    if(Case=="tower")
                    {
                        var tower=p.PlayerCombatState!.Hand.Cards.OfType<KarenTowerOfPromise>().Single();
                        await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(),tower,null);
                        Status="finished";
                        await CombatManager.Instance.CheckWinCondition();
                        return;
                    }
                    if(Case=="batch") await CardPileCmd.Add(PromisePileManager.GetPromisePile(p).Cards.ToArray(),PileType.Hand);
                    else if(Case=="infinite") await PromisePileCmd.Draw(Context,p,3);
                    else if(Case=="swap") await PromisePileCmd.SwitchHand(Context,p);
                    else throw new InvalidOperationException("Unknown runnable fixture");
                    break;
                default: throw new ArgumentException("Unknown fixture operation");
            }
            Status="finished";
        }
        catch(BaselineSafetyStop) { Status="baseline-stopped-after-8-failures"; }
        catch(Exception e) { Status="error:"+e.GetType().Name+":"+e.Message; MainFile.Logger.Error("[BoundaryReview] "+e); }
    }
    private static async Task AddPromise<T>(Player p) where T:CardModel =>
        await PromisePileCmd.Add(p,p.Creature.CombatState!.CreateCard<T>(p));
    private static async Task Setup(Player p,string name)
    {
        Case="setup"; Player=p; Status="setup"; FailedMoves=0;
        foreach(var power in p.Creature.Powers.ToArray()) await PowerCmd.Remove(power);
        await CardPileCmd.RemoveFromCombat(p.PlayerCombatState!.AllCards.ToArray());
        var combat=p.Creature.CombatState!;
        foreach(var enemy in combat.HittableEnemies) { enemy.SetMaxHpInternal(9999); enemy.SetCurrentHpInternal(9999); }
        if(name=="void") await PromisePileManager.EnterMode(p,PromisePileMode.Void);
        if(name is "empty" or "void" or "batch" or "swap")
        {
            int count=name is "batch" or "swap" ? 3 : 1;
            for(int i=0;i<count;i++) await AddPromise<KarenDefend>(p);
            await PowerCmd.Apply<KarenStageIsWaitingPower>(Context,p.Creature,1,p.Creature,null);
            if(name=="swap") for(int i=0;i<2;i++) await CardPileCmd.Add(combat.CreateCard<KarenStrike>(p),PileType.Hand);
        }
        if(name=="infinite")
        {
            await PromisePileManager.EnterMode(p,PromisePileMode.InfiniteReinforcement);
            await PowerCmd.Apply<KarenStageIsWaitingPower>(Context,p.Creature,1,p.Creature,null);
        }
        if(name=="tower")
        {
            await AddPromise<KarenCry>(p);
            await AddPromise<KarenDefend>(p); await AddPromise<KarenDefend>(p);
            await PowerCmd.Apply<JuggernautPower>(Context,p.Creature,10,p.Creature,null);
            foreach(var enemy in combat.HittableEnemies.Skip(1).ToArray()) await CreatureCmd.Kill(enemy);
            combat.HittableEnemies.Single().SetCurrentHpInternal(1);
            await CardPileCmd.Add(combat.CreateCard<KarenTowerOfPromise>(p),PileType.Hand);
        }
        p.PlayerCombatState!.Energy=3;
        _choicesBefore=p.RunState.CurrentMapPointHistoryEntry!.GetEntry(p.NetId).CardChoices.Count;
        _deckBefore=p.Deck.Cards.Count;
        Case=name;Status="ready";EnsureHud();UpdateHud();
    }
}
internal sealed class BaselineSafetyStop : Exception { }

// The unmodified baseline would otherwise loop forever on the Godot thread.
// This test-only cutout acts only after eight real failed hand transfers.
[HarmonyPatch(typeof(CardPileCmd),nameof(CardPileCmd.Add))]
#if STS2_BETA
[HarmonyPatch([typeof(IEnumerable<CardModel>),typeof(CardPile),typeof(CardPilePosition),typeof(AbstractModel),typeof(bool),typeof(bool)])]
#else
[HarmonyPatch([typeof(IEnumerable<CardModel>),typeof(CardPile),typeof(CardPilePosition),typeof(AbstractModel),typeof(bool)])]
#endif
internal static class BaselineLoopGuard
{
    private static void Postfix(CardPile newPile,ref Task<IReadOnlyList<CardPileAddResult>> __result)
    {
        if(BoundaryReviewCmd.Case!="tower" || BoundaryReviewCmd.Status!="running" || newPile.Type!=PileType.Hand) return;
        __result=Observe(__result);
    }
    private static async Task<IReadOnlyList<CardPileAddResult>> Observe(Task<IReadOnlyList<CardPileAddResult>> original)
    {
        var results=await original;
        foreach(var result in results) if(!result.success && ++BoundaryReviewCmd.FailedMoves>=8) throw new BaselineSafetyStop();
        return results;
    }
}
