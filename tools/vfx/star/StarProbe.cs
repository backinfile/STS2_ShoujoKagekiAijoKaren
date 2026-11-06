using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Models.Cards;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.basic;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.shine;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

[ModInitializer(nameof(Initialize))]
public static class StarProbe
{
    private sealed class Seen(NKarenStarVfx node) {
        public readonly NKarenStarVfx Node = node;
        public int Planned, Played, Frames;
        public bool Reward, BackCorrect;
    }
    private static readonly List<Seen> SeenNodes = new();
    private static readonly List<int> Plays = new();
    private static CardModel? _shine;
    private static bool _watch;
    public static void Initialize() {
        ((SceneTree)Engine.GetMainLoop()).ProcessFrame += Sample;
        ((SceneTree)Engine.GetMainLoop()).ProcessFrame += () => RenderingServer.ForceDraw(false);
        GD.Print("[Star Probe] Loaded");
    }
    private static void Sample() {
        if (!_watch || NCombatRoom.Instance is not {} room) return;
        foreach (var entry in CombatManager.Instance.History.CardPlaysStarted.Where(e => e.CardPlay.Card == _shine))
            if (!Plays.Contains(entry.CardPlay.PlayIndex)) Plays.Add(entry.CardPlay.PlayIndex);
        foreach (var n in room.CombatVfxContainer.GetChildren().OfType<NKarenStarVfx>())
            if (SeenNodes.All(s => s.Node != n)) SeenNodes.Add(new(n));
        foreach (var s in SeenNodes) {
            if (!GodotObject.IsInstanceValid(s.Node) || !s.Node.IsInsideTree()) continue;
            s.Planned = s.Node.PlannedCount; s.Played = s.Node.PlayedCount; s.Reward |= s.Node.RewardConfirmed;
            s.BackCorrect |= s.Node.BackgroundLight.GetParent() == room.BackCombatVfxContainer; s.Frames++;
        }
    }
    public static async Task Setup(Player p, int shine, string pile, bool multiple) {
        _watch = false; SeenNodes.Clear(); Plays.Clear();
        var all = new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust, PileType.Play, KarenCustomEnum.PromisePile }
            .SelectMany(t => t.GetPile(p).Cards).Distinct().ToArray();
        foreach (var c in all) await CardPileCmd.RemoveFromCombat(c);
        _shine = p.Creature.CombatState!.CreateCard<KarenShineStrike>(p);
        _shine.SetShineMax(Math.Max(9, shine));
        _shine.SetShineCurrent(shine);
        if (pile == "promise") await PromisePileCmd.Add(p, _shine);
        else await CardPileCmd.Add(_shine, pile switch { "draw" => PileType.Draw, "discard" => PileType.Discard, _ => PileType.Hand });
        if (multiple) await CardPileCmd.Add(p.Creature.CombatState.CreateCard<KarenShineStrike>(p), PileType.Draw);
        await CardPileCmd.Add(p.Creature.CombatState.CreateCard<KarenStar>(p), PileType.Hand);
        await CardPileCmd.Add(p.Creature.CombatState.CreateCard<KarenDefend>(p), PileType.Hand);
        _watch = true;
    }
    public static object Status(Player p) => new {
        plays = Plays.ToArray(), sourcePile = _shine?.Pile?.Type.ToString(),
        shineCount = ShinePileManager.GetShinePile(p).Cards.Count,
        sourceDepleted = _shine != null && _shine.Pile == null && ShinePileManager.GetShinePile(p).Cards.Any(c => c.Id == _shine.Id),
        extraRewards = p.RunState.CurrentRoom is CombatRoom c && c.ExtraRewards.TryGetValue(p, out var r) ? r.Count : 0,
        hp = p.Creature.CombatState?.Enemies.Select(e => e.CurrentHp).ToArray(),
        playPileCount = PileType.Play.GetPile(p).Cards.Count,
        visibleHandNodes = NCombatRoom.Instance?.Ui.Hand.ActiveHolders.Count(h => h.CardNode != null && h.CardNode.IsVisibleInTree()) ?? 0,
        live = NCombatRoom.Instance?.CombatVfxContainer.GetChildren().OfType<NKarenStarVfx>().Count() ?? 0,
        backLive = NCombatRoom.Instance?.BackCombatVfxContainer.GetChildren().Count(n => n.Name.ToString() == "KarenStarBackLight") ?? 0,
        cues = SeenNodes.Select(s => new { planned = s.Planned, played = s.Played, reward = s.Reward, frames = s.Frames, backCorrect = s.BackCorrect })
    };
}
public sealed class StarProbeCommand : AbstractConsoleCmd
{
    public override string CmdName => "star_probe";
    public override string Args => "setup N hand/draw/discard/promise [multiple]|status|speed normal/fast/instant|cancel|lethal";
    public override string Description => "Temporary isolated KarenStar fixture and event observer.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args) {
        if (player == null || args.Length == 0) return new(false, Args);
        switch (args[0]) {
            case "setup" when args.Length >= 3: return new(StarProbe.Setup(player, int.Parse(args[1]), args[2], args.Length > 3), true, "fixture");
            case "status": return new(true, JsonSerializer.Serialize(StarProbe.Status(player)));
            case "act2": return new(PowerCmd.Apply<KarenStarlight02Power>(new ThrowingPlayerChoiceContext(), player.Creature, 2, player.Creature, null), true, "Act2 fixture");
            case "speed" when args.Length == 2: SaveManager.Instance.PrefsSave.FastMode = args[1] switch { "fast" => FastModeType.Fast, "instant" => FastModeType.Instant, _ => FastModeType.Normal }; return new(true, "speed");
            case "cancel": foreach (var n in NCombatRoom.Instance!.CombatVfxContainer.GetChildren().OfType<NKarenStarVfx>()) n.QueueFree(); return new(true, "cancelled presentation only");
            case "lethal": foreach (var e in player.Creature.CombatState!.Enemies) e.SetCurrentHpInternal(1); return new(true, "lethal fixture");
        }
        return new(false, Args);
    }
}
