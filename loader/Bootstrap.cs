using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using ShoujoKageki.Core.Loader;

namespace ShoujoKagekiAijoKaren.Loader;

[ModInitializer(nameof(Initialize))]
public static class Bootstrap
{
    private const string ModId = "ShoujoKagekiAijoKaren";
    private static readonly Logger Log = new(ModId, LogType.Generic);
    public static void Initialize() => CharacterLoader.Initialize(
        ModId, "ShoujoKagekiAijoKaren.MainFile", SelectVariant, Log);

    // Android packs embed a specific game; desktop follows the latest applicable baseline.
    internal static string SelectVariant(SemanticVersion version)
    {
        var beta = new SemanticVersion(0, 113, 0);
        var stable = new SemanticVersion(0, 107, 1);
#if KAREN_ANDROID
        if (version.CompareTo(beta) == 0) return "Beta";
        if (version.CompareTo(stable) == 0) return "Stable";
        throw new NotSupportedException($"Karen Android supports game v0.107.1 and v0.113.0; found {version}. Select a matching game and Android compatibility pack.");
#else
        if (version.CompareTo(stable) < 0)
            throw new NotSupportedException($"Karen requires game v0.107.1 or newer; found {version}.");
        var variant = version.CompareTo(beta) >= 0 ? "Beta" : "Stable";
        var baseline = variant == "Beta" ? beta : stable;
        if (version.CompareTo(baseline) > 0)
            Log.Warn($"[KarenLoader] Game {version} is newer than the {variant} reference build {baseline}; trying {variant} implementation.");
        return variant;
#endif
    }
}
