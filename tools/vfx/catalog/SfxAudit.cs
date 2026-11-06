using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Modding;
using ShoujoKagekiAijoKaren.src.Core.Audio;

// Observation only: the recording fixture never suppresses or replaces audio.
[ModInitializer(nameof(Initialize))]
public static class SfxAudit
{
    private sealed record Cue(string File, string Caller);
    private static readonly List<Cue> Cues = new();
    public static void Initialize()
    {
        new Harmony("karen.recording.sfx-audit").Patch(
            AccessTools.Method(typeof(KarenAudioManager), nameof(KarenAudioManager.PlaySfxOnGameBus)),
            prefix: new HarmonyMethod(typeof(SfxAudit), nameof(Observe)));
    }
    private static void Observe(string fileName)
    {
        var caller = new StackTrace().GetFrames()
            .Select(f => f.GetMethod()?.DeclaringType)
            .FirstOrDefault(t => t?.Name.StartsWith("NKaren") == true)?.Name ?? "Other";
        Cues.Add(new(fileName, caller));
    }
    public static void Reset() => Cues.Clear();
    public static string Status() => JsonSerializer.Serialize(new { events = Cues.ToArray() });
}
public sealed class SfxAuditCommand : AbstractConsoleCmd
{
    public override string CmdName => "sfx_audit";
    public override string Args => "reset|status";
    public override string Description => "Observe actual Karen SFX playback requests without changing audio.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (args.FirstOrDefault() == "reset") { SfxAudit.Reset(); return new(true, "reset"); }
        return new(true, SfxAudit.Status());
    }
}
