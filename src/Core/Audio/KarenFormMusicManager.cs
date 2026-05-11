using MegaCrit.Sts2.Core.Entities.Players;

namespace ShoujoKagekiAijoKaren.src.Core.Audio;

/// <summary>
/// Compatibility wrapper for Karen Form's combat BGM replacement.
/// </summary>
public static class KarenFormMusicManager
{
    public const string FileName = "Karen_form.MP3";

    public static void PlayLoop(Player? player, float volume = 1f)
    {
        CombatBgmReplacementManager.PlayLoop(FileName, player, volume);
    }

    public static void Stop(Player? ownerPlayer = null)
    {
        CombatBgmReplacementManager.Stop(ownerPlayer);
    }

    public static void StopForCutscene(Player? ownerPlayer = null)
    {
        CombatBgmReplacementManager.StopForCutscene(ownerPlayer);
    }

    public static void StopForRunExit()
    {
        CombatBgmReplacementManager.StopForRunExit();
    }

    public static void RestoreGameMusicIfNeeded()
    {
        CombatBgmReplacementManager.RestoreGameMusicIfNeeded();
    }
}
