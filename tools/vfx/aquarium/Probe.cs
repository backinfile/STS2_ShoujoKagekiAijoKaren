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
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

// Isolated fixture and passive observer. No direct invocation of Aquarium's turn hook or VFX.
[ModInitializer(nameof(Initialize))]
public static class AquariumProbe
{
    private sealed class Seen(NKarenAquariumVfx node)
    {
        public readonly NKarenAquariumVfx Node = node;
        public readonly Node2D Back = node.BackLayer, Hand = node.HandLayer;
        public bool Exited, Ignored = true;
        public int Frames, SelectionFrames, ParticleFrames, Casts, Turns, Drawn, Moved;
    }
    private static readonly List<Seen> SeenNodes = new();
    private static bool Watching;
    private static int PeakLive;
    private static bool Alive(Node n) => GodotObject.IsInstanceValid(n) && n.IsInsideTree() && !n.IsQueuedForDeletion();
    public static void Initialize()
    {
        var tree = (SceneTree)Engine.GetMainLoop(); tree.Root.Unfocusable = true;
        tree.ProcessFrame += () => Callable.From(Sample).CallDeferred();
        GD.Print("[Aquarium Probe] Loaded");
    }
    private static void Sample()
    {
        if (!Watching || NCombatRoom.Instance is not {} room) return;
        PeakLive = Math.Max(PeakLive, room.CombatVfxContainer.GetChildren().OfType<NKarenAquariumVfx>().Count(Alive));
        foreach (var node in room.CombatVfxContainer.GetChildren().OfType<NKarenAquariumVfx>())
        {
            var seen = SeenNodes.FirstOrDefault(s => s.Node == node);
            if (seen == null)
            {
                seen = new(node); SeenNodes.Add(seen); var s = seen;
                node.TreeExiting += () => { Store(s); s.Exited = true; };
            }
            seen.Frames++; if (node.Selecting) seen.SelectionFrames++;
            if (node.BackLayer.GetChildren().OfType<CpuParticles2D>().Any(p => p.Emitting)) seen.ParticleFrames++;
            foreach (var layer in new[] {node.BackLayer, node.HandLayer})
                seen.Ignored &= layer.GetChildren().OfType<Control>().All(c => c.MouseFilter == Control.MouseFilterEnum.Ignore);
            Store(seen);
        }
    }
    private static void Store(Seen seen)
    {
        seen.Casts = seen.Node.CastStarts; seen.Turns = seen.Node.TurnStarts; seen.Drawn = seen.Node.ActualDrawn; seen.Moved = seen.Node.ActualMoved;
    }
    internal static void Watch() { SeenNodes.Clear(); PeakLive = 0; Watching = true; }
    internal static object Status(Player player) => new {
        roomId = NCombatRoom.Instance?.GetInstanceId() ?? 0,
        live = SeenNodes.Count(s => Alive(s.Node)), layers = SeenNodes.Sum(s => (Alive(s.Back) ? 1 : 0) + (Alive(s.Hand) ? 1 : 0)),
        peakLive = PeakLive,
        showing = SeenNodes.Count(s => Alive(s.Node) && s.Node.Showing),
        emitting = SeenNodes.Where(s => Alive(s.Node)).Sum(s => new[] {s.Back,s.Hand}.Sum(l => l.GetChildren().OfType<CpuParticles2D>().Count(p => Alive(p) && p.Emitting))),
        looping = SeenNodes.Where(s => Alive(s.Node)).Sum(s => new[] {s.Back,s.Hand}.Sum(l => l.GetChildren().OfType<CpuParticles2D>().Count(p => Alive(p) && !p.OneShot))),
        selecting = SeenNodes.Count(s => Alive(s.Node) && s.Node.Selecting),
        hand = PileType.Hand.GetPile(player).Cards.Count, draw = PileType.Draw.GetPile(player).Cards.Count,
        promise = KarenCustomEnum.PromisePile.GetPile(player).Cards.Count, amount = player.Creature.GetPower<KarenAquariumPower>()?.Amount ?? 0,
        effects = SeenNodes.Select(s => new {s.Frames,s.SelectionFrames,s.ParticleFrames,s.Casts,s.Turns,s.Drawn,s.Moved,s.Exited,s.Ignored})
    };
    internal static async Task Seed(Player player, string mode)
    {
        var combat = CombatManager.Instance.DebugOnlyGetState()!;
        var cards = new[] {PileType.Hand,PileType.Draw,PileType.Discard,PileType.Exhaust,KarenCustomEnum.PromisePile}.SelectMany(p => p.GetPile(player).Cards).Distinct().ToArray();
        await CardPileCmd.RemoveFromCombat(cards);
        if (mode == "empty" && player.Relics.FirstOrDefault(r => r.Id.Entry == "KAREN_HAIRPIN_RELIC") is {} hairpin) await RelicCmd.Remove(hairpin);
        if (mode == "void") await PromisePileManager.EnterMode(player, PromisePileMode.Void);
        var template = ModelDb.AllCards.Single(c => c.Id.Entry == "KAREN_DEFEND");
        for (int i = 0; i < (mode == "empty" ? 0 : mode == "single" ? 1 : 18); i++)
            await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard(template,player),PileType.Draw,player);
        if (mode == "full") for (int i = 0; i < 9; i++)
        {
            var retained = combat.CreateCard(template,player); retained.AddKeyword(CardKeyword.Retain);
            await CardPileCmd.AddGeneratedCardToCombat(retained,PileType.Hand,player);
        }
        for (int i = 0; i < (mode == "stack" ? 2 : 1); i++)
        {
            var card = combat.CreateCard<KarenAquariumCard>(player);
            if (mode == "upgrade") {card.UpgradeInternal();card.FinalizeUpgradeInternal();}
            await CardPileCmd.AddGeneratedCardToCombat(card,PileType.Hand,player);
        }
        if (mode == "combo")
        {
            await PromisePileManager.EnterMode(player,PromisePileMode.Burn);
            await PromisePileManager.EnterMode(player,PromisePileMode.PastAndFuture);
            await PowerCmd.Apply<KarenStageIsWaitingPower>(new ThrowingPlayerChoiceContext(),player.Creature,1,player.Creature,null);
        }
    }
}
public sealed class AquariumProbeCmd : AbstractConsoleCmd
{
    public override string CmdName => "aquarium_probe";
    public override string Args => "seed normal/upgrade/stack/full/empty/single/void/combo|watch|status|speed normal/fast/instant|remove|clearfx|pose";
    public override string Description => "Isolated Aquarium VFX verification";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player,string[] args)
    {
        if (player == null || args.Length == 0) return new(false,Args);
        switch(args[0])
        {
            case "seed":return new(AquariumProbe.Seed(player,args[1]),true,"Seeded");
            case "watch":AquariumProbe.Watch();return new(true,"Watching");
            case "status":return new(true,JsonSerializer.Serialize(AquariumProbe.Status(player)));
            case "speed":SaveManager.Instance.PrefsSave.FastMode=args[1] switch {"fast"=>FastModeType.Fast,"instant"=>FastModeType.Instant,_=>FastModeType.Normal};return new(true,args[1]);
            case "remove":return new(PowerCmd.Remove<KarenAquariumPower>(player.Creature),true,"Removed");
            case "clearfx":foreach(var node in NCombatRoom.Instance!.CombatVfxContainer.GetChildren().OfType<NKarenAquariumVfx>())node.QueueFree();return new(true,"Cleared");
            case "pose":var view=NCombatRoom.Instance!.GetCreatureNode(player.Creature)!;view.ScaleTo(.65f,0);view.Visuals.Scale*=new Vector2(-1,1);view.Position+=new Vector2(120,35);return new(true,"Transformed");
            default:return new(false,Args);
        }
    }
}
