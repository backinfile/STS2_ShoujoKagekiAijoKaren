using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using BaseLib.Config;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using ShoujoKagekiAijoKaren.src.Core.Commands;
using ShoujoKagekiAijoKaren;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using ShoujoKagekiAijoKaren.src.Core;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;

[ModInitializer(nameof(Initialize))]
public static class QualityProbe
{
    internal static readonly Dictionary<string, int> Draws = new();
    internal static double DrawMs;
    internal static int RibbonDraws;
    internal static readonly Dictionary<string, long> DrawBytes = new();
    internal static readonly Dictionary<ulong, int> PlaceFrames = new();
    internal static readonly Dictionary<string, int> NativeEvents = new();
    public static void Initialize()
    {
        new Harmony("karen.quality.fixture").PatchAll();
        GD.Print("[Karen Quality Probe] loaded");
    }
    [HarmonyPatch]
    private static class NativeMeasure
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(VfxCmd), "PlayVfx");
            foreach (string name in new[] { "NCardTrailVfx", "NExhaustVfx", "NCardExhaustVfx" })
            {
                var type = typeof(VfxCmd).Assembly.GetTypes().FirstOrDefault(t => t.Name == name);
                if (type != null) yield return AccessTools.Method(type, "Create");
            }
            yield return AccessTools.Method(typeof(MegaCrit.Sts2.Core.Nodes.Vfx.Utilities.NParticlesContainer), "Restart");
        }
        private static void Postfix(MethodBase __originalMethod, object? __instance, object[] __args)
        {
            string type = __originalMethod.DeclaringType!.Name;
            if (type == "NParticlesContainer" && (__instance is not Node node || !node.Name.ToString().StartsWith("VanillaEnergy"))) return;
            string key = type + ":" + (__args.OfType<string>().FirstOrDefault() ?? __originalMethod.Name);
            NativeEvents[key] = NativeEvents.GetValueOrDefault(key) + 1;
        }
    }
    [HarmonyPatch]
    private static class RibbonMeasure
    {
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(KarenVfxLevel).Assembly.GetType("ShoujoKagekiAijoKaren.src.Core.Vfx.KarenLightRibbon"), "Draw");
        private static void Postfix(float alpha) { if (alpha > 0) RibbonDraws++; }
    }
    [HarmonyPatch(typeof(NKarenWaitingStageVfx), "Place")]
    private static class PlaceMeasure
    {
        private static void Prefix()
        {
            ulong frame = (ulong)Engine.GetFramesDrawn();
            PlaceFrames[frame] = PlaceFrames.GetValueOrDefault(frame) + 1;
        }
    }
    [HarmonyPatch]
    private static class DrawMeasure
    {
        private static IEnumerable<MethodBase> TargetMethods() => typeof(KarenModConfig).Assembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Vfx") == true || t.Namespace?.Contains("ShineSystem.Nodes") == true)
            .Select(t => t.GetMethod("_Draw", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .OfType<MethodBase>();
        private static void Prefix(out (long Ticks, long Bytes) __state) => __state = (Stopwatch.GetTimestamp(), GC.GetAllocatedBytesForCurrentThread());
        private static void Postfix(MethodBase __originalMethod, (long Ticks, long Bytes) __state)
        {
            string key = __originalMethod.DeclaringType!.Name;
            long bytes = GC.GetAllocatedBytesForCurrentThread() - __state.Bytes;
            Draws[key] = Draws.GetValueOrDefault(key) + 1;
            DrawBytes[key] = DrawBytes.GetValueOrDefault(key) + bytes;
            DrawMs += Stopwatch.GetElapsedTime(__state.Ticks).TotalMilliseconds;
        }
    }
}

// Isolated recording fixture; never included in the shipped Mod.
public sealed class QualityCmd : AbstractConsoleCmd
{
    public override string CmdName => "quality_probe";
    public override string Args => "set Full|Low|Off | status | reset | check";
    public override string Description => "Effect quality verification and recording";
    public override bool IsNetworked => false;
    private static IEnumerable<Node> Walk(Node node)
    {
        yield return node;
        foreach (var child in node.GetChildren()) foreach (var item in Walk(child)) yield return item;
    }
    public override CmdResult Process(Player? player, string[] args)
    {
        try
        {
            if (args.FirstOrDefault() == "set")
            {
                KarenModConfig.VfxLevel = Enum.Parse<KarenVfxLevel>(args[1]);
                var config = ModConfigRegistry.Get<KarenModConfig>()!;
                config.Save();
                var expected = KarenModConfig.VfxLevel;
                config.Load();
                if (KarenModConfig.VfxLevel != expected) throw new Exception("Setting persistence failed");
            }
            if (args.FirstOrDefault() == "reset") { QualityProbe.Draws.Clear(); QualityProbe.DrawMs = 0; QualityProbe.NativeEvents.Clear(); QualityProbe.DrawBytes.Clear(); QualityProbe.PlaceFrames.Clear(); QualityProbe.RibbonDraws = 0; }
            if (args.FirstOrDefault() == "check") return new(true, Check());
            if (args.FirstOrDefault() == "prepare")
            {
                if (player?.Creature.CombatState is not { } combat) return new(false, "Combat required");
                foreach (var card in PileType.Hand.GetPile(player).Cards.Where(c => c.Id.Entry == "KAREN_SHINE_STRIKE"))
                    card.SetShineCurrent(1);
                var model = ModelDb.AllCards.Single(c => c.Id.Entry == "KAREN_DEFEND");
                var cards = Enumerable.Range(0, 3).Select(_ => combat.CreateCard(model, player)).ToArray();
                return new(CardPileCmd.AddGeneratedCardsToCombat(cards, KarenCustomEnum.PromisePile, player), true, "Prepared real tower cards");
            }
            if (args.FirstOrDefault() == "clear")
            {
                if (player == null) return new(false, "Player required");
                var cards = Enum.Parse<PileType>(args[1]).GetPile(player).Cards.ToArray();
                return new(CardPileCmd.RemoveFromCombat(cards), true, "Cleared test pile");
            }
            if (args.FirstOrDefault() == "ribbon")
            {
                if (player == null) return new(false, "Player required");
                NKarenBenefitVfx.Play(player, NKarenBenefitVfx.BenefitKind.Dream);
                return new(true, "Dream ribbon preview");
            }
            if (args.FirstOrDefault() == "relay")
            {
                if (player == null) return new(false, "Player required");
                if (args.ElementAtOrDefault(1) == "finale") NKarenFightRelayVfx.RewardRegistered(player, 3);
                else NKarenFightRelayVfx.Transferred(player, player, 3);
                return new(true, "Visual relay preview; no network or rule changes");
            }
            if (args.FirstOrDefault() == "recall")
            {
                if (player == null) return new(false, "Player required");
                return new(PromisePileCmd.Draw(new ThrowingPlayerChoiceContext(), player), true, "Recall real promise card");
            }
            if (args.FirstOrDefault() == "video")
            {
                if (player == null) return new(false, "Player required");
                bool native = KarenModConfig.VfxLevel == KarenVfxLevel.Off;
                bool played = NKarenLastWordVideoVfx.Play(player);
                if (native && player.Creature.CombatState is { } combat)
                    VfxCmd.PlayOnSide(MegaCrit.Sts2.Core.Combat.CombatSide.Enemy, VfxCmd.slashPath, combat);
                var root = ((SceneTree)Engine.GetMainLoop()).Root;
                int players = Walk(root).OfType<VideoStreamPlayer>().Count(p => p.IsPlaying());
                return new(true, JsonSerializer.Serialize(new { played, players, nativePreview = native, waitSeconds = native ? 0 : NKarenLastWordVideoVfx.VideoSequenceSeconds }));
            }
            var nodes = Walk(((SceneTree)Engine.GetMainLoop()).Root).ToArray();
            var nonon = nodes.OfType<NKarenNononVfx>().FirstOrDefault();
            int scatter = nonon == null ? 0 : ((ICollection)AccessTools.Field(typeof(NKarenNononVfx), "_particles").GetValue(nonon)!).Count;
            return new(true, JsonSerializer.Serialize(new {
                level = KarenModConfig.VfxLevel.ToString(),
                visibleEffects = nodes.Count(n => n.GetType().Assembly == typeof(KarenModConfig).Assembly
                    && (n.GetType().Name.StartsWith("NKaren") || n.GetType().Name.StartsWith("NDisableRelic"))
                    && n is CanvasItem visual && visual.IsVisibleInTree()),
                activeParticleEmitters = nodes.OfType<GpuParticles2D>().Count(p => p.IsVisibleInTree() && p.Emitting),
                ribbonDraws = QualityProbe.RibbonDraws,
                nativeEvents = QualityProbe.NativeEvents,
                nativeEnergyVisible = nodes.Count(n => n is CanvasItem c && c.IsVisibleInTree() && n.Name.ToString().StartsWith("VanillaEnergy")),
                trailPath = player?.Character.TrailPath,
                draws = QualityProbe.Draws, drawBytes = QualityProbe.DrawBytes,
                maxPlacesPerFrame = QualityProbe.PlaceFrames.Count == 0 ? 0 : QualityProbe.PlaceFrames.Values.Max(),
                placeCalls = QualityProbe.PlaceFrames.Values.Sum(), renderFramesWithPlace = QualityProbe.PlaceFrames.Count,
                waiting = nodes.OfType<NKarenWaitingStageVfx>().Select(n => new {
                    n.PowerLayers, heads = n.PatrolHeads.Count(), n.EnergyPointCanvas,
                    uiVisible = n.UiVisible, ringVisible = n.RingVisual.IsVisibleInTree()
                }).ToArray(),
                cpuDrawMs = QualityProbe.DrawMs,
                nononParticles = scatter,
                ambientVisible = nodes.Count(n => n.GetType().Name == "NKarenShineAmbientVfx" && n is CanvasItem c && c.IsVisibleInTree()),
                warningVisible = nodes.Count(n => n.GetType().Name == "NKarenShineWarningVfx" && n is CanvasItem c && c.IsVisibleInTree()),
                gpuAmounts = nodes.OfType<GpuParticles2D>().Where(p => p.Name.ToString().Contains("Star")).Select(p => p.Amount).ToArray(),
                effects = nodes.Where(n => n.GetType().Assembly == typeof(KarenModConfig).Assembly && n.GetType().Name.EndsWith("Vfx")).GroupBy(n => n.GetType().Name).ToDictionary(g => g.Key, g => g.Count())
            }));
        }
        catch (Exception error) { return new(false, error.ToString()); }
    }
    private static string Check()
    {
        var original = KarenModConfig.VfxLevel;
        var assembly = typeof(KarenModConfig).Assembly;
        var policy = assembly.GetType("ShoujoKagekiAijoKaren.src.Core.Vfx.KarenVfxQuality")!;
        var batchType = assembly.GetType("ShoujoKagekiAijoKaren.src.Core.Vfx.KarenVfxCanvasBatch")!;
        var results = new List<object>();
        try
        {
            foreach (var level in Enum.GetValues<KarenVfxLevel>())
            {
                KarenModConfig.VfxLevel = level;
                int Count(int n) => (int)AccessTools.Method(policy, "ParticleCount").Invoke(null, [n])!;
                var batch = Activator.CreateInstance(batchType, [4096])!;
                AccessTools.Method(batchType, "Circle").Invoke(batch, [Vector2.Zero, 50f, Colors.White]);
                int triangles = (int)AccessTools.Field(batchType, "_indexCount").GetValue(batch)! / 3;
                int stride = level == KarenVfxLevel.Full ? 1 : 4;
                if (Count(40) != 40 / stride || Count(0) != 0 || Count(1) != 1 || triangles != 64 / stride)
                    throw new Exception("Budget failed: " + level);
                string label = new LocString("settings_ui", "SHOUJOKAGEKIAIJOKAREN-VFX_LEVEL." + level).GetRawText();
                if (string.IsNullOrWhiteSpace(label) || label.Contains("VFX_LEVEL")) throw new Exception("Missing localized dropdown label");
                results.Add(new { level = level.ToString(), particles = Count(40), triangles, label });
            }
            KarenModConfig.VfxLevel = (KarenVfxLevel)999;
            if (KarenModConfig.VfxLevel != KarenVfxLevel.Full) throw new Exception("Invalid level fallback failed");
        }
        finally { KarenModConfig.VfxLevel = original; }
        return JsonSerializer.Serialize(new { passed = true, levels = results });
    }
}
