using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Runs;

namespace ShoujoKagekiAijoKaren.src.Core.Multiplayer;

internal static class RoomMenus
{
    internal static RunSaveManager RunSaves => (RunSaveManager)AccessTools.Field(typeof(SaveManager), "_runSaveManager").GetValue(SaveManager.Instance);
    internal static bool OwnsRoomSave(SerializableRun? run) => run?.PlatformType == PlatformType.None &&
        System.Linq.Enumerable.Any(run.Players, player => player.NetId == RoomSettings.PlayerId);

    public static async Task Host(GameMode mode, Control loading, NSubmenuStack stack, SerializableRun? save = null)
    {
        RoomSocket? socket = null;
        NetHostGameService? service = null;
        loading.Visible = true;
        using var lifetime = new CancellationTokenSource();
        void CancelWhenHidden() { if (!GodotObject.IsInstanceValid(loading) || !loading.IsVisibleInTree()) lifetime.Cancel(); }
        loading.TreeExiting += lifetime.Cancel;
        loading.VisibilityChanged += CancelWhenHidden;
        try
        {
            socket = new RoomSocket();
            await socket.Connect(RoomSettings.Endpoint, RoomSettings.Credential, RoomSettings.Version, null, lifetime.Token);
            if (!loading.IsInsideTree() || !loading.IsVisibleInTree()) { socket.Dispose(); return; }
#if STS2_BETA
            service = new NetHostGameService(PeerVersionInfo.LocalDefault());
#else
            service = new NetHostGameService();
#endif
            var host = new RoomHost(service, socket);
            AccessTools.Field(typeof(NetHostGameService), "_netHost").SetValue(service, host);
            RoomSession.Activate(service);
            if (save == null)
            {
                switch (mode)
                {
                    case GameMode.Daily:
                        var daily = stack.GetSubmenuType<NDailyRunScreen>();
                        daily.InitializeMultiplayerAsHost(service); stack.Push(daily); break;
                    case GameMode.Custom:
                        var custom = stack.GetSubmenuType<NCustomRunScreen>();
                        custom.InitializeMultiplayerAsHost(service, 4); stack.Push(custom); break;
                    default:
                        var standard = stack.GetSubmenuType<NCharacterSelectScreen>();
                        standard.InitializeMultiplayerAsHost(service, 4); stack.Push(standard); break;
                }
            }
            else
            {
                switch (mode)
                {
                    case GameMode.Daily:
                        var daily = stack.GetSubmenuType<NDailyRunLoadScreen>();
                        daily.InitializeAsHost(service, save); stack.Push(daily); break;
                    case GameMode.Custom:
                        var custom = stack.GetSubmenuType<NCustomRunLoadScreen>();
                        custom.InitializeAsHost(service, save); stack.Push(custom); break;
                    default:
                        var standard = stack.GetSubmenuType<NMultiplayerLoadGameScreen>();
                        standard.InitializeAsHost(service, save); stack.Push(standard); break;
                }
            }
            // Badge is attached by the native lobby open hook.
            MainFile.Logger.Info($"[KarenRooms] Native lobby ready; room={host.RoomCode}, mode={mode}");
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { socket?.Dispose(); }
        catch (Exception error)
        {
            service?.Disconnect(NetError.InternalError, true);
            socket?.Dispose();
            MainFile.Logger.Warn($"[KarenRooms] Host failed: {error.GetType().Name}");
            RoomUi.Message(RoomText.Error(error));
        }
        finally
        {
            if (GodotObject.IsInstanceValid(loading))
            {
                loading.TreeExiting -= lifetime.Cancel;
                loading.VisibilityChanged -= CancelWhenHidden;
                loading.Visible = false;
            }
        }
    }

    public static async Task Load(NMultiplayerSubmenu screen, NSubmenuStack stack, Control loading)
    {
        // Read transport ownership before canonicalization: a foreign player ID can
        // otherwise make native validation rename a perfectly valid Steam save.
        var manager = RunSaves;
        var result = manager.LoadMultiplayerRunSave();
        if (!result.Success || result.SaveData == null)
        {
            RoomUi.Message(RoomText.Get("cannot_load")); return;
        }
        if (result.SaveData.PlatformType != PlatformType.None)
        {
            RoomUi.Message(RoomText.Get("native_save")); return;
        }
        if (!System.Linq.Enumerable.Any(result.SaveData.Players, player => player.NetId == RoomSettings.PlayerId))
        {
            RoomUi.Message(RoomText.Get("native_save")); return;
        }
        result = SaveManager.Instance.LoadAndCanonicalizeMultiplayerRunSave(RoomSettings.PlayerId);
        if (!result.Success || result.SaveData == null) { RoomUi.Message(RoomText.Get("cannot_load")); return; }
        await Host(result.SaveData.GameMode, loading, stack, result.SaveData);
    }
}

[HarmonyPatch(typeof(NMultiplayerHostSubmenu), nameof(NMultiplayerHostSubmenu.StartHostAsync))]
internal static class RoomHostMenuPatch
{
    private static bool Prefix(GameMode gameMode, Control loadingOverlay, NSubmenuStack stack, ref Task __result)
    {
        if (!RoomSettings.Enabled) { RoomSession.ResetForSingleplayer(); return true; }
        __result = RoomMenus.Host(gameMode, loadingOverlay, stack);
        return false;
    }
}

[HarmonyPatch(typeof(NMultiplayerSubmenu), "StartHostAsync")]
internal static class RoomLoadHostPatch
{
    private static bool Prefix(NMultiplayerSubmenu __instance, SerializableRun run, ref Task __result)
    {
        if (!RoomSettings.Enabled)
        {
            if (RoomMenus.OwnsRoomSave(run)) { RoomUi.Message(RoomText.Get("room_save")); __result = Task.CompletedTask; return false; }
            RoomSession.ResetForSingleplayer(); return true;
        }
        var stack = (NSubmenuStack)AccessTools.Field(typeof(NSubmenu), "_stack").GetValue(__instance);
        __result = RoomMenus.Host(run.GameMode, __instance.GetNode<Control>("%LoadingOverlay"), stack, run);
        return false;
    }
}

[HarmonyPatch(typeof(NMultiplayerSubmenu), "StartLoad")]
internal static class RoomStartLoadPatch
{
    private static bool Prefix(NMultiplayerSubmenu __instance)
    {
        if (!RoomSettings.Enabled)
        {
            if (RoomMenus.OwnsRoomSave(RoomMenus.RunSaves.LoadMultiplayerRunSave().SaveData))
            { RoomUi.Message(RoomText.Get("room_save")); return false; }
            RoomSession.ResetForSingleplayer(); return true;
        }
        var stack = (NSubmenuStack)AccessTools.Field(typeof(NSubmenu), "_stack").GetValue(__instance);
        TaskHelper.RunSafely(RoomMenus.Load(__instance, stack, __instance.GetNode<Control>("%LoadingOverlay")));
        return false;
    }
}

[HarmonyPatch(typeof(NJoinFriendScreen), nameof(NJoinFriendScreen.OnSubmenuOpened))]
internal static class RoomJoinMenuPatch
{
    private static bool Prefix(NJoinFriendScreen __instance)
    {
        if (!RoomSettings.Enabled)
        {
            RoomSession.ResetForSingleplayer();
            if (__instance.GetNodeOrNull<Control>("KarenRoomJoin") is { } panel) panel.Visible = false;
            __instance.GetNode<Control>("%ButtonContainer").Visible = true;
            __instance.GetNode<Control>("%RefreshButton").Visible = true;
            return true;
        }
        RoomUi.Join(__instance);
        return false;
    }
}

[HarmonyPatch]
internal static class RoomLobbyBadgePatch
{
    private static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
    {
        foreach (var type in new[] { typeof(NCharacterSelectScreen), typeof(NCustomRunScreen),
            typeof(NDailyRunScreen), typeof(NMultiplayerLoadGameScreen), typeof(NCustomRunLoadScreen), typeof(NDailyRunLoadScreen) })
            {
            var method = AccessTools.Method(type, nameof(NSubmenu.OnSubmenuOpened));
            if (method.DeclaringType == type) yield return method;
        }
    }
    private static void Postfix(NSubmenu __instance)
    {
        if (RoomSession.Code != null) RoomUi.AttachBadge(__instance);
    }
}

[HarmonyPatch]
internal static class RoomSingleplayerIdentityResetPatch
{
    private static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
    {
        foreach (var type in new[] { typeof(NCharacterSelectScreen), typeof(NCustomRunScreen), typeof(NDailyRunScreen) })
            yield return AccessTools.Method(type, "InitializeSingleplayer");
    }
    private static void Prefix() => RoomSession.ResetForSingleplayer();
}

// Native continue/abandon can run after restart, without an active room session.
// Only saves owned by this installation's persistent room identity are retargeted.
[HarmonyPatch(typeof(RunSaveManager), nameof(RunSaveManager.LoadAndCanonicalizeMultiplayerRunSave))]
internal static class RoomSavedIdentityPatch
{
    private static void Prefix(RunSaveManager __instance, ref ulong localPlayerId)
    {
        if (RoomMenus.OwnsRoomSave(__instance.LoadMultiplayerRunSave().SaveData))
            localPlayerId = RoomSettings.PlayerId;
    }
}
