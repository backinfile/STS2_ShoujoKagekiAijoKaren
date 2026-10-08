using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public sealed class FuelVfxCmd : AbstractConsoleCmd
{
    private static FuelProbe? _probe;
    public override string CmdName => "fuel_vfx";
    public override string Args => "watch|report";
    public override string Description => "Check Fuel feedback timing against actual energy and cleanup.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (player == null || args.Length != 1) return new(false, Args);
        if (args[0] == "watch")
        {
            _probe?.Dispose();
            _probe = new FuelProbe(player);
            return new(true, "Watching actual energy and fuel nodes");
        }
        if (args[0] != "report" || _probe == null) return new(false, Args);
        _probe.Dispose();
        var p = _probe;
        return new(p.FireFrames > 0 && p.FlashFrames > 0 && p.EarlyFlashFrames == 0 && p.Remaining == 0 && p.MaxParticleLayers >= 4,
            JsonSerializer.Serialize(new { p.FireFrames, p.FlashFrames, p.EarlyFlashFrames, p.Remaining,
                p.FirstFire, p.FirstFlash, p.FirstEnergy, p.InitialEnergy, p.MaxParticleLayers, p.EmittedEffects, p.Seeds, p.Sources, finalEnergy = player.PlayerCombatState?.Energy }));
    }
}
public sealed class FuelProbe : IDisposable
{
    private readonly Player _player;
    private readonly SceneTree _tree;
    private double _time;
    private bool _disposed;
    public int FireFrames, FlashFrames, EarlyFlashFrames, Remaining;
    public double? FirstFire, FirstFlash, FirstEnergy;
    public int InitialEnergy;
    public int MaxParticleLayers, EmittedEffects;
    public readonly List<float> Seeds = new();
    public readonly List<float[]> Sources = new();
    private readonly HashSet<ulong> _seen = new();
    public FuelProbe(Player player)
    {
        _player = player;
        InitialEnergy = player.PlayerCombatState!.Energy;
        _tree = (SceneTree)Engine.GetMainLoop();
        _tree.ProcessFrame += Sample;
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _tree.ProcessFrame -= Sample;
    }
    private void Sample()
    {
        _time += NRun.Instance!.GetProcessDeltaTime();
        var nodes = NRun.Instance.GlobalUi.GetChildren();
        int fire = nodes.Count(n => n.GetType().Name == "NKarenDropFuelVfx");
        int flash = nodes.OfType<NKarenDropFuelVfx>().Count(n => n.HasEnergyPulse);
        bool gained = _player.PlayerCombatState?.Energy > InitialEnergy;
        if (gained) FirstEnergy ??= _time;
        if (fire > 0) { FireFrames++; FirstFire ??= _time; }
        if (flash > 0) { FlashFrames++; FirstFlash ??= _time; if (!gained) EarlyFlashFrames++; }
        int layers = 0;
        foreach (var effect in nodes.OfType<NKarenDropFuelVfx>())
        {
            var particles = effect.GetChildren().SelectMany(n => n.GetChildren()).OfType<GpuParticles2D>().ToArray();
            layers += particles.Length;
            if (particles.Length > 0 && _seen.Add(effect.GetInstanceId()))
            {
                EmittedEffects++;
                var material = (ShaderMaterial)particles[0].ProcessMaterial;
                Seeds.Add(material.GetShaderParameter("flow_seed").AsSingle());
                var source = material.GetShaderParameter("source_position").AsVector2();
                Sources.Add(new[] { source.X, source.Y });
            }
        }
        MaxParticleLayers = Math.Max(MaxParticleLayers, layers);
        Remaining = fire;
    }
}
