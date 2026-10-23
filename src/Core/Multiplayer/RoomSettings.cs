using System;
using System.IO;
using System.Linq;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using System.Security.Cryptography;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Platform.Null;

namespace ShoujoKagekiAijoKaren.src.Core.Multiplayer;

internal static class AndroidPolicy
{
#if KAREN_ANDROID
    public const bool KarenOnly = true;
#else
    public const bool KarenOnly = false;
#endif
}

internal static class RoomSettings
{
    public const string DefaultEndpoint = "wss://47.95.213.214:8443/rooms";
    // Environment override is for isolated tests; public endpoints must use TLS.
    public static Uri Endpoint => new(System.Environment.GetEnvironmentVariable("KAREN_ROOM_ENDPOINT") ?? DefaultEndpoint);
    public static bool Enabled => KarenModConfig.UseRoomServer;
    public static string Version => $"karen-room-1|{ReleaseInfoManager.Instance.SemVer}|{ModelIdSerializationCache.Hash}";
    private static string? _credential;
    public static string Credential
    {
        get
        {
            if (_credential != null) return _credential;
            string path = ProjectSettings.GlobalizePath("user://karen-room-identity");
            if (File.Exists(path))
            {
                string saved = File.ReadAllText(path).Trim();
                if (saved.Length != 64 || saved.Any(c => !Uri.IsHexDigit(c)))
                    throw new IOException("Invalid room identity file; restore its backup.");
                return _credential = saved;
            }
            string value = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var stream = new FileStream(path, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream);
            writer.Write(value);
            return _credential = value;
        }
    }
    public static ulong PlayerId
    {
        get
        {
            byte[] hash = SHA256.HashData(Convert.FromHexString(Credential));
            ulong id = System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(hash) & 0x7fffffffffffffffUL;
            return id == 0 ? 2UL : id;
        }
    }
}

internal static class RoomSession
{
    public static INetGameService? Service { get; private set; }
    public static string? Code => Service?.IsConnected == true ? Service.GetRawLobbyIdentifier() : null;
    // Keep identity until this run closes, including native save-on-disconnect.
    public static ulong? LocalId { get; private set; }
    public static void ResetForSingleplayer() { Service = null; LocalId = null; }
    public static void Activate(INetGameService service) { Service = service; LocalId = service.NetId; }
}

[HarmonyPatch(typeof(NullPlatformUtilStrategy), nameof(NullPlatformUtilStrategy.GetLocalPlayerId))]
internal static class RoomLocalIdentityPatch
{
    private static void Postfix(ref ulong __result)
    {
        if (RoomSession.LocalId is { } id) __result = id;
    }
}

[HarmonyPatch(typeof(NullPlatformUtilStrategy), nameof(NullPlatformUtilStrategy.GetPlayerName))]
internal static class RoomPlayerNamePatch
{
    private static void Postfix(ulong playerId, ref string __result)
    {
        if (RoomSession.LocalId != null) __result = $"Player {playerId % 10000:0000}";
    }
}
