using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;

[ModInitializer(nameof(Initialize))]
public static class BackgroundTestGuard
{
    public static void Initialize()
    {
        new Harmony("karen.temporary.background.test").PatchAll();
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.Unfocusable = true;
        tree.ProcessFrame += () => RenderingServer.ForceDraw(false);
        GD.Print("[Background Test Guard] Display restore blocked; offscreen rendering active.");
    }
    [HarmonyPatch(typeof(NGame), nameof(NGame.ApplyDisplaySettings))]
    private static class PreserveMinimized
    {
        private static bool Prefix()
        {
            DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.NoFocus, true);
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Minimized);
            return false;
        }
    }
}
