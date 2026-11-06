using System;
using System.Linq;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

// Test-only: the same setting written by the vanilla Fast Mode checkbox.
public sealed class NativeSpeedCmd : AbstractConsoleCmd
{
    private static FastModeType? _original;
    private static bool? _originalBackgroundMute;
    public override string CmdName => "native_speed";
    public override string Args => "normal|fast|restore|show";
    public override string Description => "Select native Normal/Fast, never Instant or Engine.TimeScale.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (args.Length != 1) return new(false, Args);
        var prefs = SaveManager.Instance.PrefsSave;
        if (args[0] == "normal" || args[0] == "fast")
        {
            _original ??= prefs.FastMode;
            _originalBackgroundMute ??= prefs.MuteInBackground;
            prefs.MuteInBackground = false;
            prefs.FastMode = args[0] == "fast" ? FastModeType.Fast : FastModeType.Normal;
        }
        else if (args[0] == "restore")
        {
            if (_original.HasValue) prefs.FastMode = _original.Value;
            _original = null;
            if (_originalBackgroundMute.HasValue) prefs.MuteInBackground = _originalBackgroundMute.Value;
            _originalBackgroundMute = null;
        }
        else if (args[0] != "show") return new(false, Args);
        return new(true, JsonSerializer.Serialize(new { nativeMode = prefs.FastMode.ToString(), timeScale = Engine.TimeScale }));
    }
}

public sealed class FormDrawSurgeCmd : AbstractConsoleCmd
{
    private static FormSurgeProbe? _probe;
    public override string CmdName => "form_surge";
    public override string Args => "watch|report|idle";
    public override string Description => "Measure the actual frame-by-frame Form wind multiplier.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (args.Length != 1) return new(false, Args);
        var creature = player == null ? null : NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        var wind = creature?.GetChildren().OfType<NKarenFormVfx>().SingleOrDefault();
        if (wind == null) return new(false, "Active Form required");
        if (args[0] == "watch")
        {
            _probe?.Dispose();
            _probe = new FormSurgeProbe(wind);
            return new(true, "Watching actual process frames");
        }
        if (_probe == null) return new(false, "watch first");
        bool passed = args[0] == "idle" ? _probe!.Peak <= 1.001f
            : args[0] == "report" && _probe!.Peak >= 2.7f && wind.SpeedMultiplier <= 1.001f;
        return new(passed, JsonSerializer.Serialize(new { nativeMode = SaveManager.Instance.PrefsSave.FastMode.ToString(),
            peak = _probe!.Peak, current = wind.SpeedMultiplier, acceleratedSeconds = _probe.AcceleratedSeconds,
            frames = _probe.Frames, samples = _probe.Samples }));
    }
}

public sealed class FormSurgeProbe : IDisposable
{
    private readonly NKarenFormVfx Wind;
    private readonly SceneTree _tree;
    private bool _disposed;
    public FormSurgeProbe(NKarenFormVfx wind)
    {
        Wind = wind;
        _tree = wind.GetTree();
        _tree.ProcessFrame += Sample;
        Wind.TreeExiting += Dispose;
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (GodotObject.IsInstanceValid(_tree)) _tree.ProcessFrame -= Sample;
        if (GodotObject.IsInstanceValid(Wind)) Wind.TreeExiting -= Dispose;
    }
    public float Peak = 1f;
    public double AcceleratedSeconds;
    public int Frames;
    public readonly System.Collections.Generic.List<float[]> Samples = new();
    private double _elapsed;
    private double _sampleAt;
    private void Sample()
    {
        double delta = Wind.GetProcessDeltaTime();
        Frames++;
        _elapsed += delta;
        Peak = Math.Max(Peak, Wind.SpeedMultiplier);
        if (Wind.SpeedMultiplier > 1.05f) AcceleratedSeconds += delta;
        if (_elapsed >= _sampleAt)
        {
            _sampleAt = _elapsed + 0.05;
            Samples.Add(new[] { (float)_elapsed, Wind.SpeedMultiplier });
        }
    }
}

