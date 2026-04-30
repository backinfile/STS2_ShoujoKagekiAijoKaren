using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Audio;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterRoomEntered))]
internal static class KarenFormMusicRoomPatch
{
    private static void Postfix(AbstractRoom room)
    {
        KarenFormMusicManager.RestoreGameMusicIfNeeded();
    }
}
