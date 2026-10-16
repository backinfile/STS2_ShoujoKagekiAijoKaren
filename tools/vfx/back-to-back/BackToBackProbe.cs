using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.ExtraReplaySystem;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.basic;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.promisePile;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

// Isolated recording fixture and passive observer. Never shipped in the Karen package.
[ModInitializer(nameof(Initialize))]
public static class BackToBackProbe
{
    private sealed class Seen(NKarenBackToBackVfx node)
    {
        public readonly NKarenBackToBackVfx Node = node;
        public readonly bool Impact = (bool)Field(node, "_impact")!;
        public readonly int Stock = (int)Field(node, "_stock")!;
        public readonly float Strength = (float)Field(node, "_strength")!;
        public int Frames;
        public float FinalAge, MaxAnchorError;
        public bool Exited;
    }
    private static readonly List<Seen> Casts = new();
    private static bool Watching;
    private static object? Field(object o, string name) => o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(o);
    public static void Initialize()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.Unfocusable = true;
        tree.ProcessFrame += () => Callable.From(Sample).CallDeferred();
        GD.Print("[BackToBack Probe] Loaded; observes native attacks.");
    }
    private static void Sample()
    {
        if (!Watching || NCombatRoom.Instance is not { } room) return;
        foreach (var node in room.CombatVfxContainer.GetChildren().OfType<NKarenBackToBackVfx>())
        {
            var seen = Casts.FirstOrDefault(s => s.Node == node);
            if (seen == null)
            {
                seen = new(node); Casts.Add(seen);
                var track = seen;
                node.TreeExiting += () => { track.Exited = true; track.FinalAge = (float)Field(node, "_age")!; };
            }
            seen.Frames++;
            if (!seen.Impact)
            {
                var target = (MegaCrit.Sts2.Core.Entities.Creatures.Creature)Field(node, "_target")!;
                var view = room.GetCreatureNode(target);
                if (view != null)
                    seen.MaxAnchorError = Math.Max(seen.MaxAnchorError,
                        ((Vector2)Field(node, "_end")!).DistanceTo(node.ToLocal(view.VfxSpawnPosition)));
            }
        }
    }
    internal static void Watch() { Casts.Clear(); Watching = true; }
    internal static string Status(Player player) => JsonSerializer.Serialize(new {
        live = NCombatRoom.Instance?.CombatVfxContainer.GetChildren().OfType<NKarenBackToBackVfx>().Count() ?? 0,
        promise = player.PlayerCombatState == null ? 0 : PromisePileManager.GetCount(player),
        casts = Casts.Select(s => new { impact = s.Impact, stock = s.Stock, strength = s.Strength, frames = s.Frames,
            exited = s.Exited, finalAge = s.FinalAge, maxAnchorError = s.MaxAnchorError })
    });
    internal static async Task Seed(Player player, int count)
    {
        PromisePileManager.ClearPromisePileInternal(player);
        for (int i = 0; i < count; i++)
            await PromisePileCmd.Add(player, player.Creature.CombatState!.CreateCard<KarenStrike>(player));
    }
}

public sealed class BackToBackProbeCommand : AbstractConsoleCmd
{
    public override string CmdName => "b2b_probe";
    public override string Args => "watch|status|seed N|speed normal/fast/instant|upgrade|replay|lethal|pose|clearfx";
    public override string Description => "Isolated Back to Back native-play fixtures and observation.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || args.Length == 0) return new(false, Args);
        switch (args[0])
        {
            case "watch": BackToBackProbe.Watch(); return new(true, "watch");
            case "status": return new(true, BackToBackProbe.Status(player));
            case "seed" when args.Length == 2 && int.TryParse(args[1], out int count) && count >= 0 && count <= 30:
                return new(BackToBackProbe.Seed(player, count), true, "seed");
            case "speed" when args.Length == 2:
                SaveManager.Instance.PrefsSave.FastMode = args[1] switch {
                    "fast" => FastModeType.Fast, "instant" => FastModeType.Instant, _ => FastModeType.Normal };
                return new(true, "speed");
            case "upgrade":
                CardCmd.Upgrade(player.PlayerCombatState!.Hand.Cards.OfType<KarenBackToBackCard>().Single());
                return new(true, "upgrade");
            case "replay":
                player.PlayerCombatState!.Hand.Cards.OfType<KarenBackToBackCard>().Single().SetExtraReplayCountForNextPlay(2);
                return new(true, "two extra native replays");
            case "lethal":
                foreach (var enemy in player.Creature.CombatState!.Enemies) enemy.SetCurrentHpInternal(1);
                return new(true, "lethal");
            case "pose":
                var room = NCombatRoom.Instance!;
                var owner = room.GetCreatureNode(player.Creature)!;
                owner.ScaleTo(.65f, 0); owner.Body.Scale *= new Vector2(-1, 1);
                var target = room.CreatureNodes.First(v => v.Entity.IsMonster);
                target.Position += new Vector2(-170, 45); target.Visuals.Rotation = .14f;
                return new(true, "scaled/flipped owner; translated/rotated target");
            case "clearfx":
                foreach (var node in NCombatRoom.Instance!.CombatVfxContainer.GetChildren().OfType<NKarenBackToBackVfx>()) node.QueueFree();
                return new(true, "removed presentation only");
            default: return new(false, Args);
        }
    }
}
