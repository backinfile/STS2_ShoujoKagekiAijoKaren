using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

[ModInitializer(nameof(Initialize))]
public static class ArrogantProbe
{
    private sealed class Seen(NKarenArrogantVfx node)
    {
        public readonly NKarenArrogantVfx Node = node;
        public bool Cost, Reward;
        public float Age, DestinationError;
        public int Frames, ArrivalFrames;
    }
    private static readonly List<Seen> Casts = new();
    private static bool _watching;
    public static void Initialize() => ((SceneTree)Engine.GetMainLoop()).ProcessFrame += Sample;
    private static void Sample()
    {
        if (!_watching || NCombatRoom.Instance is not {} room) return;
        foreach (var node in room.CombatVfxContainer.GetChildren().OfType<NKarenArrogantVfx>())
            if (Casts.All(c => c.Node != node)) Casts.Add(new(node));
        foreach (var c in Casts)
        {
            if (!GodotObject.IsInstanceValid(c.Node) || !c.Node.IsInsideTree()) continue;
            c.Frames++; c.Cost |= c.Node.CostConfirmed; c.Reward |= c.Node.RewardConfirmed;
            c.Age = Math.Max(c.Age, c.Node.Age);
            if (c.Node.RewardConfirmed && c.Node.Age > 1.1f)
            {
                var icon = room.FindChildren("*", "", true, false).OfType<NPower>().FirstOrDefault(p => p.Model is KarenPassionPower);
                if (icon == null) { c.DestinationError = 999; continue; }
                var texture = icon.GetNode<TextureRect>("%Icon");
                var point = texture.GetGlobalTransformWithCanvas() * (texture.Size * .5f);
                c.DestinationError = Math.Max(c.DestinationError, point.DistanceTo(c.Node.RewardPoint));
                c.ArrivalFrames++;
            }
        }
    }
    internal static void Watch() { Casts.Clear(); _watching = true; }
    internal static object Status(Player p) => new {
        live = NCombatRoom.Instance?.CombatVfxContainer.GetChildren().OfType<NKarenArrogantVfx>().Count() ?? 0,
        back = NCombatRoom.Instance?.BackCombatVfxContainer.GetChildren().Count(n => n.Name == "KarenArrogantBack") ?? 0,
        vulnerable = p.Creature.GetPowerAmount<VulnerablePower>(), passion = p.Creature.GetPowerAmount<KarenPassionPower>(),
        casts = Casts.Select(c => new { cost = c.Cost, reward = c.Reward, age = c.Age, frames = c.Frames, arrivalFrames = c.ArrivalFrames, destinationError = c.DestinationError })
    };
}
public sealed class ArrogantProbeCmd : AbstractConsoleCmd
{
    public override string CmdName => "arrogant_probe";
    public override string Args => "watch|status";
    public override string Description => "Observe real Arrogant casts in an isolated test game.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || args.Length != 1) return new(false, Args);
        if (args[0] == "watch") { ArrogantProbe.Watch(); return new(true, "Watching actual card plays"); }
        if (args[0] == "status") return new(true, JsonSerializer.Serialize(ArrogantProbe.Status(player)));
        return new(false, Args);
    }
}
