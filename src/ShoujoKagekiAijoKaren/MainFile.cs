using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Models.Characters;

namespace ShoujoKagekiAijoKaren;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "ShoujoKagekiAijoKaren";
    private const string BuildMarker = "2026-05-11-character-select-diagnostics";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();
        Logger.Info($"[KarenDiagnostics] Initialize marker={BuildMarker}, assembly={assembly.Location}");

        ScriptManagerBridge.LookupScriptsInAssembly(assembly);

        ShoujoKageki.Core.V1.CoreApi.Configuration.Changed += (previous, current) =>
        {
            if (previous.VfxLevel != current.VfxLevel) src.Core.Vfx.KarenVfxQuality.ApplyLevel();
        };
        ShoujoKageki.Core.V1.CoreApi.Get<ShoujoKageki.Core.V1.IMusicPlayback>().Changed += _ =>
            src.Core.Audio.KarenAudioManager.UpdateMusicVolume(MegaCrit.Sts2.Core.Saves.SaveManager.Instance?.SettingsSave?.VolumeBgm ?? 1f);

        src.Core.GlobalMoveSystem.Patches.GlobalMovePatch.Initialize();
        src.Core.PromisePileSystem.Patches.PromisePileHookPatch.Initialize();
        Harmony harmony = new(ModId);
        harmony.PatchAll();
        Logger.Info("[KarenDiagnostics] Harmony PatchAll completed.");
    }
}
