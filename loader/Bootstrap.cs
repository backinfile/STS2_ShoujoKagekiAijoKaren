using System.Reflection;
using System.Runtime.Loader;
using HarmonyLib;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace ShoujoKagekiAijoKaren.Loader;

[ModInitializer(nameof(Initialize))]
public static class Bootstrap
{
    private const string ModId = "ShoujoKagekiAijoKaren";
    private static readonly Logger Log = new(ModId, LogType.Generic);
    private static Assembly? _selectedAssembly;
    private static bool _usesStableApi;

    public static void Initialize()
    {
        if (_selectedAssembly is not null) return;

        var version = ReleaseInfoManager.Instance.SemVer;
        var isStable = version.CompareTo(new SemanticVersion(0, 107, 1)) == 0;
        var isBeta = version.CompareTo(new SemanticVersion(0, 111, 0)) == 0;
        var variant = isBeta ? "Beta" : "Stable";
        _usesStableApi = !isBeta;
        if (!isStable && !isBeta)
            Log.Warn($"[KarenLoader] Unknown game version {version}; falling back to Stable implementation built for v0.107.1.");

        var loaderAssembly = typeof(Bootstrap).Assembly;
        var loaderDir = Path.GetDirectoryName(loaderAssembly.Location)
            ?? throw new InvalidOperationException("Cannot locate Karen mod installation.");
        var variantPath = Path.Combine(loaderDir, "lib", $"{ModId}.{variant}.dll");
        if (!File.Exists(variantPath))
            throw new FileNotFoundException($"Missing Karen {variant} implementation.", variantPath);

        var context = AssemblyLoadContext.GetLoadContext(loaderAssembly) ?? AssemblyLoadContext.Default;
        var implementation = context.LoadFromAssemblyPath(Path.GetFullPath(variantPath));
        _selectedAssembly = implementation;

        // v0.111 has a public association API. v0.107's Mod record holds only
        // its root assembly, so both branches also use the ModTypes bridge.
        try
        {
            typeof(ModManager).GetMethod("AssociateAssemblyWithMod", BindingFlags.Public | BindingFlags.Static,
                null, [typeof(string), typeof(Assembly)], null)?.Invoke(null, [ModId, implementation]);
        }
        catch (Exception ex)
        {
            Log.Warn($"[KarenLoader] Game assembly association failed; using ModTypes bridge: {ex.Message}");
        }
        new Harmony(ModId + ".Loader").PatchAll(loaderAssembly);

        var entry = implementation.GetType("ShoujoKagekiAijoKaren.MainFile", throwOnError: true)!;
        entry.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null);
        Log.Info($"[KarenLoader] Loaded {variant} implementation for game {version}.");
    }

    internal static Type[] SelectedTypes => _selectedAssembly?.GetTypes() ?? [];
    internal static bool UsesStableApi => _usesStableApi;
}

[HarmonyPatch(typeof(ReflectionHelper), nameof(ReflectionHelper.ModTypes), MethodType.Getter)]
internal static class KarenModTypesBridge
{
    private static void Postfix(ref Type[] __result)
    {
        var selectedTypes = Bootstrap.SelectedTypes;
        if (selectedTypes.Length == 0) return;

        var seen = new HashSet<Type>(__result);
        __result = [.. __result, .. selectedTypes.Where(seen.Add)];
    }
}

// v0.107 builds the multiplayer model-ID table from each mod's single root
// assembly. Include the selected implementation when it scans this loader.
[HarmonyPatch(typeof(ReflectionHelper), nameof(ReflectionHelper.GetSubtypesFromAssembly))]
internal static class KarenAssemblySubtypesBridge
{
    private static void Postfix(Assembly assembly, Type parentType, ref IEnumerable<Type> __result)
    {
        if (!Bootstrap.UsesStableApi || assembly != typeof(Bootstrap).Assembly) return;

        var selectedTypes = Bootstrap.SelectedTypes
            .Where(type => !type.IsAbstract && !type.IsInterface && parentType.IsAssignableFrom(type));
        __result = __result.Concat(selectedTypes);
    }
}
