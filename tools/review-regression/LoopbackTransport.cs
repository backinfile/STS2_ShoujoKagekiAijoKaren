using Godot;
using HarmonyLib;

// Optional test-only transport restriction. No firewall or administrator changes.
[HarmonyPatch(typeof(ENetConnection), nameof(ENetConnection.CreateHostBound))]
internal static class KarenTestLoopbackHost
{
    [HarmonyPrefix]
    private static void Prefix(ref string __0) => __0 = "127.0.0.1";
}
[HarmonyPatch(typeof(ENetConnection), nameof(ENetConnection.CreateHost))]
internal static class KarenTestLoopbackClient
{
    [HarmonyPrefix]
    private static bool Prefix(ENetConnection __instance, int __0, int __1, int __2, int __3, ref Error __result)
    {
        __result = __instance.CreateHostBound("127.0.0.1", 0, __0, __1, __2, __3);
        return false;
    }
}
