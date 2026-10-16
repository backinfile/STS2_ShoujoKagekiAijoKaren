using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
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
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.basic;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

[ModInitializer(nameof(Initialize))]
public static class ExchangeFateProbe
{
    private sealed class Seen(NKarenExchangeFateVfx node)
    {
        public readonly NKarenExchangeFateVfx Node = node;
        public int Frames, Sent, Received;
        public bool Confirmed;
    }
    private static readonly List<Seen> SeenNodes = new();
    private static readonly List<CardModel> Outgoing = new(), Incoming = new();
    private static CardModel? _source;
    private static bool _ready;
    public static void Initialize()
    {
        ((SceneTree)Engine.GetMainLoop()).ProcessFrame += () =>
        {
            if (!_ready || NCombatRoom.Instance is not { } room) return;
            foreach (var node in room.Ui.GetChildren().OfType<NKarenExchangeFateVfx>())
                if (SeenNodes.All(s => s.Node != node)) SeenNodes.Add(new(node));
            foreach (var s in SeenNodes)
                if (GodotObject.IsInstanceValid(s.Node) && s.Node.IsInsideTree())
                { s.Frames++; s.Sent = s.Node.SentCount; s.Received = s.Node.ReceivedCount; s.Confirmed |= s.Node.Confirmed; }
        };
        GD.Print("[Exchange Fate Probe] Loaded");
    }
    public static async Task Setup(Player p, int outgoing, int incoming, string mode)
    {
        _ready = false; SeenNodes.Clear(); Outgoing.Clear(); Incoming.Clear();
        var all = new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust, PileType.Play, KarenCustomEnum.PromisePile }
            .SelectMany(t => t.GetPile(p).Cards).Distinct().ToArray();
        foreach (var c in all) await CardPileCmd.RemoveFromCombat(c);
        if (mode == "void") await PromisePileManager.EnterMode(p, PromisePileMode.Void);
        var combat = p.Creature.CombatState!;
        for (int i = 0; i < outgoing; i++)
        {
            var c = combat.CreateCard<KarenStrike>(p); Outgoing.Add(c);
            await CardPileCmd.Add(c, PileType.Hand);
        }
        for (int i = 0; i < incoming; i++)
        {
            CardModel c = mode == "repeat" && i == 0 ? combat.CreateCard<KarenExchangeFate>(p) : combat.CreateCard<KarenDefend>(p);
            Incoming.Add(c);
            if (mode == "void") await CardPileCmd.Add(c, PileType.Draw);
            else await PromisePileCmd.Add(p, c);
        }
        _source = combat.CreateCard<KarenExchangeFate>(p);
        if (mode == "repeat") _source.UpgradeInternal();
        await CardPileCmd.Add(_source, PileType.Hand);
        _ready = true;
    }
    public static object Status(Player p) => new
    {
        ready = _ready,
        sourcePile = _source?.Pile?.Type.ToString(),
        sent = Outgoing.Select(c => c.Pile?.Type.ToString()).ToArray(),
        received = Incoming.Select(c => c.Pile?.Type.ToString()).ToArray(),
        handCount = PileType.Hand.GetPile(p).Cards.Count,
        playCount = PileType.Play.GetPile(p).Cards.Count,
        live = NCombatRoom.Instance?.Ui.GetChildren().OfType<NKarenExchangeFateVfx>().Count() ?? 0,
        progress = NCombatRoom.Instance?.Ui.GetChildren().OfType<NKarenExchangeFateVfx>().Select(n => n.ExchangeProgress).ToArray(),
        cues = SeenNodes.Select(s => new { frames = s.Frames, sent = s.Sent, received = s.Received, confirmed = s.Confirmed }).ToArray()
    };
}
public sealed class ExchangeFateProbeCmd : AbstractConsoleCmd
{
    public override string CmdName => "fate_probe";
    public override string Args => "setup OUT IN normal/void/repeat | status | speed normal/fast/instant | cancel";
    public override string Description => "Isolated ExchangeFate identity and presentation observer.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || args.Length == 0) return new(false, Args);
        switch (args[0])
        {
            case "setup" when args.Length == 4: return new(ExchangeFateProbe.Setup(player, int.Parse(args[1]), int.Parse(args[2]), args[3]), true, "fixture");
            case "status": return new(true, JsonSerializer.Serialize(ExchangeFateProbe.Status(player)));
            case "speed" when args.Length == 2:
                SaveManager.Instance.PrefsSave.FastMode = args[1] switch { "fast" => FastModeType.Fast, "instant" => FastModeType.Instant, _ => FastModeType.Normal };
                return new(true, "speed");
            case "cancel":
                foreach (var n in NCombatRoom.Instance!.Ui.GetChildren().OfType<NKarenExchangeFateVfx>()) n.QueueFree();
                return new(true, "presentation removed");
        }
        return new(false, Args);
    }
}
