using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using MegaCrit.Sts2.Core.Commands;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

// Disposable isolated-test plugin. Actual cards are played through MCP; this only observes
// presentation and prepares upgrade/speed/scale fixtures. It is never shipped in Karen.
[ModInitializer(nameof(Initialize))]
public static class BananaProbe
{
    internal sealed class Cast
    {
        public NKarenBananaVfx Effect = null!;
        public Node2D Steam = null!;
        public Node2D Hud = null!;
        public string Kind = "";
        public int Before, Maximum;
        public bool SawPulse, FalsePulse;
        public bool? PotionSucceeded;
    }
    internal static readonly List<Cast> Casts = new();
    internal static readonly List<object> PotionFlights = new();
    internal static Player? Owner;
    internal static bool Watching;
    internal static Transform2D? Visual, Body;
    public static void Initialize()
    {
        new Harmony("karen.temporary.banana.probe").PatchAll();
        ((SceneTree)Engine.GetMainLoop()).ProcessFrame += Sample;
        GD.Print("[Banana Probe] Loaded; real card casts only.");
    }
    [HarmonyPatch(typeof(NPotionContainer), nameof(NPotionContainer.AnimatePotion))]
    private static class PotionFlight
    {
        private static void Postfix(NPotionContainer __instance, PotionModel potion, Vector2? startPosition)
        {
            if (!Watching || !NKarenBananaVfx.TryGetPotionOrigin(potion, out var expected)) return;
            float error = -1;
            foreach (var child in __instance.FindChildren("*", "", true, false))
            {
                if (child is not NPotion node || node.Model != potion) continue;
                var container = node.GetNode<Control>("Container");
                var imageOffset = node.Image.GetGlobalTransform() * (node.Image.Size / 2) - container.GlobalPosition;
                if (startPosition.HasValue) error = (__instance.GetCanvasTransform() * (startPosition.Value + imageOffset)).DistanceTo(expected);
                break;
            }
            PotionFlights.Add(new { hasOrigin = startPosition.HasValue, centreError = error });
        }
    }
    private static void Sample()
    {
        if (!Watching || Owner == null) return;
        var room = NCombatRoom.Instance;
        if (room == null) return;
        foreach (var effect in room.CombatVfxContainer.GetChildren().OfType<NKarenBananaVfx>())
        {
            if (Casts.Any(c => c.Effect == effect)) continue;
            Casts.Add(new Cast { Effect = effect, Kind = effect.Kind.ToString(), Before = Owner.Creature.GetPowerAmount<StrengthPower>(),
                Steam = effect.BackLayer, Hud = effect.HudLayer });
        }
        foreach (var cast in Casts)
        {
            if (!GodotObject.IsInstanceValid(cast.Effect)) continue;
            int strength = Owner.Creature.GetPowerAmount<StrengthPower>();
            cast.Maximum = Math.Max(cast.Maximum, strength - cast.Before);
            cast.SawPulse |= cast.Effect.HasGainPulse;
            cast.FalsePulse |= cast.Effect.HasGainPulse && strength <= cast.Before;
            cast.PotionSucceeded = cast.Effect.PotionSucceeded;
        }
    }
    internal static object Status(Player player) => new {
        strength = player.Creature.GetPowerAmount<StrengthPower>(),
        speed = SaveManager.Instance.PrefsSave.FastMode.ToString(),
        live = Casts.Count(c => GodotObject.IsInstanceValid(c.Effect) && c.Effect.IsInsideTree()),
        steamLive = Casts.Count(c => GodotObject.IsInstanceValid(c.Steam) && c.Steam.IsInsideTree()),
        hudLive = Casts.Count(c => GodotObject.IsInstanceValid(c.Hud) && c.Hud.IsInsideTree()),
        potionFlights = PotionFlights,
        casts = Casts.Select(c => new { kind = c.Kind, gain = c.Maximum, pulse = c.SawPulse, falsePulse = c.FalsePulse, potionSucceeded = c.PotionSucceeded }),
        resources = new [] { "shared_use/smoke_vfx.png", "shared_use/ash_particle.png", "vfx_power_up/beam.png", "vfx_power_up/base_ring.png", "vfx_power_up/base_glow.png", "vfx_power_up/sparkle.png" }
            .Select(p => new { path = p, exists = ResourceLoader.Exists("res://images/vfx/" + p) }),
        shader = ResourceLoader.Exists("res://shaders/karen_benefit_flow.gdshader")
    };
}

public sealed class BananaProbeCmd : AbstractConsoleCmd
{
    public override string CmdName => "banana_probe";
    public override string Args => "watch|status|upgrade ID|speed normal/fast/instant|scale small/large/flip/normal|cancel";
    public override string Description => "Observe the three actual Banana card casts in an isolated test.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player?.PlayerCombatState == null || args.Length == 0) return new(false, Args);
        switch (args[0])
        {
            case "watch": BananaProbe.Casts.Clear(); BananaProbe.PotionFlights.Clear(); BananaProbe.Owner = player; BananaProbe.Watching = true; break;
            case "status": return new(true, JsonSerializer.Serialize(BananaProbe.Status(player)));
            case "upgrade": CardCmd.Upgrade(PileType.Hand.GetPile(player).Cards.First(c => c.Id.Entry == args[1])); break;
            case "speed": SaveManager.Instance.PrefsSave.FastMode = args[1] switch { "fast" => FastModeType.Fast, "instant" => FastModeType.Instant, _ => FastModeType.Normal }; break;
            case "cancel":
                foreach (var effect in NCombatRoom.Instance!.CombatVfxContainer.GetChildren().OfType<NKarenBananaVfx>()) effect.QueueFree();
                break;
            case "scale":
                var creature = NCombatRoom.Instance!.GetCreatureNode(player.Creature)!;
                BananaProbe.Visual ??= creature.Visuals.Transform; BananaProbe.Body ??= creature.Body.Transform;
                creature.Visuals.Transform = BananaProbe.Visual.Value; creature.Body.Transform = BananaProbe.Body.Value;
                if (args[1] == "small") creature.ScaleTo(.5f, 0);
                if (args[1] == "large") creature.ScaleTo(1.5f, 0);
                if (args[1] == "flip") creature.Body.Scale *= new Vector2(-1, 1);
                break;
            default: return new(false, Args);
        }
        return new(true, args[0]);
    }
}
