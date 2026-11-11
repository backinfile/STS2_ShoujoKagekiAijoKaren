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

    // Android launchers can import a different PC game version. Reject it before
    // loading an incompatible implementation; desktop keeps its existing fallback.
    internal static string SelectVariant(SemanticVersion version)
    {
        if (version.CompareTo(new SemanticVersion(0, 111, 0)) == 0) return "Beta";
        if (version.CompareTo(new SemanticVersion(0, 107, 1)) == 0) return "Stable";
#if KAREN_ANDROID
        throw new NotSupportedException($"Karen Android supports game v0.107.1 and v0.111.0; found {version}. Select a matching game and Android compatibility pack.");
#else
        Log.Warn($"[KarenLoader] Unknown game version {version}; falling back to Stable implementation built for v0.107.1.");
        return "Stable";
#endif
    }

}
