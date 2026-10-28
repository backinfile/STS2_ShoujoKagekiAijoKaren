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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Rooms;
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
    public static void Initialize()
    {
        new Harmony("karen.quality.fixture").PatchAll();
        GD.Print("[Karen Quality Probe] loaded");
    }
    [HarmonyPatch]
    private static class DrawMeasure
    {
        private static IEnumerable<MethodBase> TargetMethods() => typeof(KarenModConfig).Assembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Vfx") == true || t.Namespace?.Contains("ShineSystem.Nodes") == true)
            .Select(t => t.GetMethod("_Draw", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .OfType<MethodBase>();
        private static void Prefix(out long __state) => __state = Stopwatch.GetTimestamp();
        private static void Postfix(MethodBase __originalMethod, long __state)
        {
            string key = __originalMethod.DeclaringType!.Name;
            Draws[key] = Draws.GetValueOrDefault(key) + 1;
            DrawMs += Stopwatch.GetElapsedTime(__state).TotalMilliseconds;
        }
    }
}

// Isolated recording fixture; never included in the shipped Mod.
public sealed class QualityCmd : AbstractConsoleCmd
{
    public override string CmdName => "quality_probe";
    public override string Args => "set Full|Balanced|Low | status | reset | check";
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
            if (args.FirstOrDefault() == "reset") { QualityProbe.Draws.Clear(); QualityProbe.DrawMs = 0; }
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
            if (args.FirstOrDefault() == "video")
            {
                if (player == null) return new(false, "Player required");
                bool played = NKarenLastWordVideoVfx.Play(player);
                var root = ((SceneTree)Engine.GetMainLoop()).Root;
                int players = Walk(root).OfType<VideoStreamPlayer>().Count(p => p.IsPlaying());
                return new(true, JsonSerializer.Serialize(new { played, players, waitSeconds = NKarenLastWordVideoVfx.VideoSequenceSeconds }));
            }
            var nodes = Walk(((SceneTree)Engine.GetMainLoop()).Root).ToArray();
            var nonon = nodes.OfType<NKarenNononVfx>().FirstOrDefault();
            int scatter = nonon == null ? 0 : ((ICollection)AccessTools.Field(typeof(NKarenNononVfx), "_particles").GetValue(nonon)!).Count;
            return new(true, JsonSerializer.Serialize(new {
                level = KarenModConfig.VfxLevel.ToString(), draws = QualityProbe.Draws, cpuDrawMs = QualityProbe.DrawMs,
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
                int stride = level == KarenVfxLevel.Full ? 1 : level == KarenVfxLevel.Balanced ? 2 : 4;
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
