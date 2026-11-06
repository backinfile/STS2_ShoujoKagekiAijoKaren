using System;
using System.Collections;
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
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.neutral;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

// Test-only fixture and observer. Not included in the distributed Mod.
[ModInitializer(nameof(Initialize))]
public static class WakeUpProbe
{
    internal sealed class Seen(NKarenWakeUpCardVfx node)
    {
        public readonly NKarenWakeUpCardVfx Node = node;
        public readonly CardModel Model = (CardModel)node.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(f => f.FieldType == typeof(CardModel)).GetValue(node)!;
        public int Frames, MaxTrail, MaxSparks;
        public float Distance;
        public bool Started, Landed, Freed, WrongCard, FrameBound;
        public bool FiniteMesh = true;
        public Vector2? Previous;
    }
    internal static bool Watching;
    internal static readonly List<Seen> Effects = new();
    internal sealed class SeenCurtain(NKarenWakeUpSelectionVfx node)
    {
        public readonly NKarenWakeUpSelectionVfx Node = node;
        public int Frames, PeakSelected, Targets;
        public bool Shown, BelowCards, InputIgnored, Gathered, Freed;
    }
    internal static readonly List<SeenCurtain> Curtains = new();
    static object? Field(object o, string name) => o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(o);
    public static void Initialize() => ((SceneTree)Engine.GetMainLoop()).ProcessFrame += () => Callable.From(Sample).CallDeferred();
    static void Sample()
    {
        if (!Watching || NCombatRoom.Instance is not { } room) return;
        foreach (var node in room.Ui.GetChildren().OfType<NKarenWakeUpSelectionVfx>())
        {
            var seen = Curtains.FirstOrDefault(s => s.Node == node);
            if (seen == null) { seen = new SeenCurtain(node); Curtains.Add(seen); var captured = seen; node.TreeExiting += () => captured.Freed = true; }
            seen.Frames++;
            seen.Shown |= node.Showing;
            seen.PeakSelected = Math.Max(seen.PeakSelected, node.SelectedCount);
            seen.Gathered |= (float)Field(node, "_closingAt")! >= 0;
            seen.Targets = Math.Max(seen.Targets, ((ICollection)Field(node, "_targets")!).Count);
            if (Field(node, "_field") is ColorRect field && GodotObject.IsInstanceValid(field))
            {
                seen.BelowCards |= field.GetParent() is NSimpleCardSelectScreen && field.GetIndex() == 0;
                seen.InputIgnored |= field.MouseFilter == Control.MouseFilterEnum.Ignore;
            }
        }
        foreach (var node in room.Ui.GetChildren().OfType<NKarenWakeUpCardVfx>())
        {
            var seen = Effects.FirstOrDefault(s => s.Node == node);
            if (seen == null)
            {
                seen = new Seen(node); Effects.Add(seen);
                var tracked = seen;
                node.TreeExiting += () => tracked.Freed = true;
            }
            seen.Frames++;
            var card = Field(node, "_card") as NCard;
            if (card == null || !GodotObject.IsInstanceValid(card)) continue;
            seen.Started = true;
            seen.WrongCard |= card.Model != seen.Model || seen.Model.Pile?.Type != PileType.Hand;
            seen.Landed |= (float)Field(node, "_landedAt")! >= 0;
            seen.MaxTrail = Math.Max(seen.MaxTrail, ((ICollection)Field(node, "_trail")!).Count);
            seen.MaxSparks = Math.Max(seen.MaxSparks, ((ICollection)Field(node, "_sparks")!).Count);
            seen.FrameBound |= Field(node, "_frame") is TextureRect { Texture: not null } frame && frame.Size.X > 0 && frame.Size.Y > 0;
            if (Field(node, "_silkMesh") is ArrayMesh mesh && mesh.GetSurfaceCount() > 0)
                foreach (var vertex in mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array())
                    seen.FiniteMesh &= vertex.IsFinite();
            var point = card.Body.GetGlobalTransformWithCanvas() * (card.Body.Size * .5f);
            if (seen.Previous is { } previous) seen.Distance += previous.DistanceTo(point);
            seen.Previous = point;
        }
    }
    internal static object Status() => new {
        live = NCombatRoom.Instance?.Ui.GetChildren().OfType<NKarenWakeUpCardVfx>().Count() ?? 0,
        liveCurtains = NCombatRoom.Instance?.Ui.GetChildren().OfType<NKarenWakeUpSelectionVfx>().Count() ?? 0,
        showingCurtains = NCombatRoom.Instance?.Ui.GetChildren().OfType<NKarenWakeUpSelectionVfx>().Count(s => s.Showing) ?? 0,
        selected = NCombatRoom.Instance?.Ui.GetChildren().OfType<NKarenWakeUpSelectionVfx>().Sum(s => s.SelectedCount) ?? 0,
        curtains = Curtains.Select(s => new { s.Frames, s.PeakSelected, s.Targets, s.Shown, s.BelowCards, s.InputIgnored, s.Gathered, s.Freed }),
        effects = Effects.Select(s => new { card = s.Model.Id.Entry, s.Frames, s.Started, s.Landed, s.Freed,
            s.WrongCard, s.MaxTrail, s.MaxSparks, s.FrameBound, s.FiniteMesh, s.Distance })
    };
}

public sealed class WakeUpProbeCmd : AbstractConsoleCmd
{
    public override string CmdName => "wake_probe";
    public override string Args => "watch|status|speed normal/instant|seed draw/discard/promise/void/infinite COUNT base/upgrade/full";
    public override string Description => "Observe native Wake Up plays in isolated tests.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player?.PlayerCombatState == null || args.Length == 0) return new(false, Args);
        if (args[0] == "watch") { WakeUpProbe.Effects.Clear(); WakeUpProbe.Curtains.Clear(); WakeUpProbe.Watching = true; return new(true, "Watching"); }
        if (args[0] == "status") return new(true, JsonSerializer.Serialize(WakeUpProbe.Status()));
        if (args[0] == "peek" && NOverlayStack.Instance?.Peek() is NSimpleCardSelectScreen screen)
        { screen.GetNode<NPeekButton>("%PeekButton").SetPeeking(args[1] == "on"); return new(true, args[1]); }
        if (args[0] == "abort_vfx")
        { foreach (var node in NCombatRoom.Instance!.Ui.GetChildren().OfType<NKarenWakeUpSelectionVfx>()) node.End(); return new(true, "Scope ended"); }
        if (args[0] == "speed") { SaveManager.Instance.PrefsSave.FastMode = args[1] == "instant" ? FastModeType.Instant : FastModeType.Normal; return new(true, args[1]); }
        if (args[0] == "seed" && args.Length == 4) return new(Seed(player, args[1], int.Parse(args[2]), args[3]), true, "Prepared fixture");
        return new(false, Args);
    }
    private static async Task Seed(Player player, string source, int count, string mode)
    {
        var combat = CombatManager.Instance.DebugOnlyGetState()!;
        var cards = new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust, KarenCustomEnum.PromisePile }
            .SelectMany(p => p.GetPile(player).Cards).Distinct().ToArray();
        await CardPileCmd.RemoveFromCombat(cards);
        if (source == "void") await PromisePileManager.EnterMode(player, PromisePileMode.Void);
        if (source == "infinite") await PromisePileManager.EnterMode(player, PromisePileMode.InfiniteReinforcement);
        var sourcePile = source switch { "discard" => PileType.Discard, "promise" or "infinite" => KarenCustomEnum.PromisePile, _ => PileType.Draw };
        var ids = new[] { "KAREN_STRIKE", "KAREN_DEFEND", "KAREN_FALL" };
        for (int i = 0; i < count; i++)
        {
            var template = ModelDb.AllCards.Single(c => c.Id.Entry == ids[i % ids.Length]);
            await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard(template, player), sourcePile, player);
        }
        var old = ModelDb.AllCards.Single(c => c.Id.Entry == "KAREN_DEFEND");
        for (int i = 0; i < (mode == "full" ? 9 : 3); i++)
            await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard(old, player), PileType.Hand, player);
        var wake = combat.CreateCard<KarenWakeUp>(player);
        if (mode == "upgrade") { wake.UpgradeInternal(); wake.FinalizeUpgradeInternal(); }
        await CardPileCmd.AddGeneratedCardToCombat(wake, PileType.Hand, player);
    }
}
