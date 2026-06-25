using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLib.Config;
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

        ModConfigRegistry.Register(ModId, new KarenModConfig());

        Harmony harmony = new(ModId);
        harmony.PatchAll();
        Logger.Info("[KarenDiagnostics] Harmony PatchAll completed.");
    }
}

[HarmonyPatch(typeof(ModelDb), "AllCharacters", MethodType.Getter)]
public class ModelDbAllCharactersPatch
{
    private static bool _loggedFirstCall;

    public static void Postfix(ref IEnumerable<CharacterModel> __result)
    {
        var charactersList = __result.ToList();

        var karen = ModelDb.Character<Karen>();
        if (charactersList.All(character => character.Id != karen.Id))
        {
            charactersList.Add(karen);
        }

        __result = charactersList;

        typeof(ModelDb).GetField("_allCharacterCardPools", BindingFlags.Static | BindingFlags.NonPublic)
            ?.SetValue(null, null);
        typeof(ModelDb).GetField("_allCards", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);

        if (!_loggedFirstCall)
        {
            _loggedFirstCall = true;
            MainFile.Logger.Info(
                $"[KarenDiagnostics] ModelDb.AllCharacters patched. containsKaren={charactersList.Any(c => c.Id == karen.Id)}, count={charactersList.Count}");
        }
    }
}
