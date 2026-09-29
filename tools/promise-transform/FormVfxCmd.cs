using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public sealed class FormVfxCmd : AbstractConsoleCmd
{
    public override string CmdName => "form_vfx";
    public override string Args => "check|cycle|absent";
    public override string Description => "Inspect the Form wind, owner, screen coordinates and music.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || args.Length != 1) return new(false, "Player and action required");
        if (args[0] == "cycle")
        {
            KarenFormVfxManager.Stop(player);
            KarenFormVfxManager.Start(player);
            return new(true, "Stop/start queued; check after 0.3 seconds");
        }
        var creature = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        var nodes = creature?.GetChildren().OfType<NKarenFormVfx>().ToArray() ?? [];
        var music = typeof(CombatBgmReplacementManager).GetField("_currentPlayer", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) as AudioStreamPlayer;
        bool playing = GodotObject.IsInstanceValid(music) && music!.Playing;
        if (args[0] == "absent") return new(nodes.Length == 0 && !playing,
            JsonSerializer.Serialize(new { nodes = nodes.Length, playing }));
        if (args[0] != "check" || nodes.Length != 1) return new(false, $"Expected one wind node, got {nodes.Length}");
        var wind = nodes[0];
        var screen = wind.GetCanvasTransform() * wind.GlobalTransform;
        bool fixedScreen = screen.Origin.Length() < 0.01f && screen.X.DistanceTo(Vector2.Right) < 0.001f
            && screen.Y.DistanceTo(Vector2.Down) < 0.001f;
        bool alive = !wind.IsQueuedForDeletion() && wind.Visible && wind.Modulate.A > 0.99f && wind.BelongsTo(creature!);
        var particles = wind.GetChildren().OfType<Sprite2D>().ToArray();
        float height = wind.GetViewportRect().Size.Y;
        bool withinBand = particles.All(p => p.Position.Y >= height * 0.16f && p.Position.Y <= height * 0.73f);
        return new(fixedScreen && alive && withinBand && playing && particles.Length > 0,
            JsonSerializer.Serialize(new { fixedScreen, alive, withinBand, playing, particles = particles.Length }));
    }
}
