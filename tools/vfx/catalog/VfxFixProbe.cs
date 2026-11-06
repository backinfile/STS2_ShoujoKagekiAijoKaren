using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.Core;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

// Test-only: inspect rendered uniforms and exercise a connection removed before teardown.
public sealed class VfxFixProbeCommand : AbstractConsoleCmd
{
    public override string CmdName => "vfx_fix";
    public override string Args => "watch|status|detach|interrupt";
    public override string Description => "Regression observations for bridge composition and entrance cleanup";
    public override bool IsNetworked => false;
    private static NCombatRoom? _watchedRoom;
    private static bool _watching;
    private static int _frames;
    private static float _peakLight, _peakAtmosphere, _minHeldLight;
    private static NKarenBridgeVfx[] Stages(NCombatRoom room) => room.BackCombatVfxContainer.GetChildren().OfType<NKarenBridgeVfx>()
        .Where(n => !n.IsQueuedForDeletion() && n.Visible && !n.IsSpotlight && n.HasCards).ToArray();
    private static float Uniform(NKarenBridgeVfx n, string field, string uniform) =>
        typeof(NKarenBridgeVfx).GetField(field, Fields)!.GetValue(n) is ShaderMaterial m ? m.GetShaderParameter(uniform).AsSingle() : 0;
    private static void Sample()
    {
        if (!GodotObject.IsInstanceValid(_watchedRoom) || NCombatRoom.Instance != _watchedRoom) return;
        var stages = Stages(_watchedRoom!);
        float light = stages.Sum(n => Uniform(n, "_mainLightMaterial", "intensity"));
        _peakLight = Math.Max(_peakLight, light);
        _peakAtmosphere = Math.Max(_peakAtmosphere, stages.Sum(n => Uniform(n, "_atmosphereMaterial", "strength")));
        if (stages.Any(n => n.IsHolding && n.Age >= .7f)) _minHeldLight = Math.Min(_minHeldLight, light);
        _frames++;
    }
    private const BindingFlags Fields = BindingFlags.NonPublic | BindingFlags.Instance;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (NCombatRoom.Instance is not { } room || args.Length != 1) return new(false, Args);
        if (args[0] == "watch")
        {
            if (_watching) RenderingServer.FramePostDraw -= Sample;
            _watching = true; _watchedRoom = room; _frames = 0; _peakLight = _peakAtmosphere = 0; _minHeldLight = 10;
            RenderingServer.FramePostDraw += Sample;
            return new(true, "Observing actual rendered light and atmosphere uniforms");
        }
        var actor = room.CreatureNodes.Single(n => n.Entity.Player == player);
        if (args[0] is "detach" or "interrupt")
        {
            if (actor.Visuals is not SNCreatureVisuals visuals) return new(false, "Karen required");
            var animation = visuals.GetNode<AnimationPlayer>("AnimationPlayer");
            var callbacks = animation.GetSignalConnectionList(AnimationMixer.SignalName.AnimationStarted)
                .Select(c => c["callable"].AsCallable())
                .Where(c => c.Delegate?.Target == visuals && c.Delegate.Method.Name == "OnEntranceNativeAnimation").ToArray();
            bool connected = callbacks.Length == 1;
            if (args[0] == "detach")
            {
                if (!connected) return new(false, "Expected the real entrance callback connection");
                animation.Disconnect(AnimationMixer.SignalName.AnimationStarted, callbacks.Single());
                return new(true, "Detached actual entrance callback before native exit");
            }
            visuals.BeginKarenEntrance();
            bool began = visuals.IsEntrancePoseActive;
            actor.SetAnimationTrigger("Attack");
            return new(true, JsonSerializer.Serialize(new { connected, began, cancelled = !visuals.IsEntrancePoseActive }));
        }
        if (args[0] != "status") return new(false, Args);
        var stages = Stages(room);
        return new(true, JsonSerializer.Serialize(new {
            coordinators = room.BackCombatVfxContainer.GetChildren().Count(n => n.GetType().Name == "StageLighting" && !n.IsQueuedForDeletion()),
            frames = _frames, peakLight = _peakLight, peakAtmosphere = _peakAtmosphere, minHeldLight = _minHeldLight,
            count = stages.Length, ages = stages.Select(n => n.Age),
            light = stages.Sum(n => Uniform(n, "_mainLightMaterial", "intensity")),
            atmosphere = stages.Sum(n => Uniform(n, "_atmosphereMaterial", "strength")),
            materialIds = stages.Select(n => ((ShaderMaterial)typeof(NKarenBridgeVfx).GetField("_mainLightMaterial", Fields)!.GetValue(n)!).GetInstanceId()),
            character = player?.Character.Id.Entry
        }));
    }
}
