using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Saves;
using ShoujoKagekiAijoKaren.src.Models.Characters;

namespace ShoujoKagekiAijoKaren.src.Core.Multiplayer;

internal static class AndroidPolicy
{
#if KAREN_ANDROID
    public const bool KarenOnly = true;
#else
    public const bool KarenOnly = false;
#endif
}

internal static class KarenCharacterPolicy
{
    public static bool Allowed(CharacterModel character) => !AndroidPolicy.KarenOnly || character is Karen;
    public static void Apply(NCharacterSelectButton button)
    {
        if (!AndroidPolicy.KarenOnly) return;
        if (button.Character is Karen)
        {
            button.DebugUnlock();
            return;
        }
        AccessTools.Field(typeof(NCharacterSelectButton), "_isLocked").SetValue(button, true);
        button.GetNode<TextureRect>("%Icon").Texture = button.Character.CharacterSelectLockedIcon;
        button.GetNode<Control>("%Lock").Visible = true;
        button.Disable();
        button.FocusMode = Control.FocusModeEnum.None;
    }
}

[HarmonyPatch(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.Init))]
internal static class AndroidCharacterInitPatch
{
    private static void Postfix(NCharacterSelectButton __instance) => KarenCharacterPolicy.Apply(__instance);
}

[HarmonyPatch]
internal static class AndroidCharacterUnlockPatch
{
    private static IEnumerable<MethodBase> TargetMethods() =>
        new[] { AccessTools.Method(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.DebugUnlock)),
                AccessTools.Method(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.UnlockIfPossible)) };
    private static bool Prefix(NCharacterSelectButton __instance) => KarenCharacterPolicy.Allowed(__instance.Character);
}

[HarmonyPatch(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.Select))]
internal static class AndroidCharacterSelectPatch
{
    private static bool Prefix(NCharacterSelectButton __instance)
    {
        if (KarenCharacterPolicy.Allowed(__instance.Character)) return true;
        // Native menus select their first button on opening. Redirect that default to Karen.
        __instance.GetParent()?.GetChildren().OfType<NCharacterSelectButton>()
            .FirstOrDefault(b => b.Character is Karen)?.Select();
        return false;
    }
}

[HarmonyPatch(typeof(StartRunLobby), nameof(StartRunLobby.SetLocalCharacter))]
internal static class AndroidLobbyCharacterPatch
{
    private static void Prefix(ref CharacterModel character)
    {
        if (AndroidPolicy.KarenOnly) character = ModelDb.Character<Karen>();
    }
}

[HarmonyPatch]
internal static class AndroidLobbyInitialCharacterPatch
{
    private static IEnumerable<MethodBase> TargetMethods() =>
        new[] { AccessTools.Method(typeof(StartRunLobby), nameof(StartRunLobby.AddLocalHostPlayerInternal)),
                AccessTools.Method(typeof(StartRunLobby), nameof(StartRunLobby.InitializeFromMessage)) };
    private static void Postfix(StartRunLobby __instance)
    {
        if (AndroidPolicy.KarenOnly && __instance.LocalPlayer.character is not Karen)
            __instance.SetLocalCharacter(ModelDb.Character<Karen>());
    }
}

[HarmonyPatch(typeof(StartRunLobby), "ChangeCharacter")]
internal static class AndroidLobbyFinalCharacterPatch
{
    private static void Prefix(StartRunLobby __instance, ulong playerId, ref CharacterModel character)
    {
        if (AndroidPolicy.KarenOnly && playerId == __instance.NetService.NetId)
            character = ModelDb.Character<Karen>();
    }
}

[HarmonyPatch(typeof(NMainMenu), "OnContinueButtonPressed")]
internal static class AndroidSavedCharacterPatch
{
    private static bool Prefix()
    {
        if (!AndroidPolicy.KarenOnly) return true;
        var save = SaveManager.Instance.LoadRunSave();
        if (save.SaveData?.Players.Any(p => p.CharacterId != ModelDb.Character<Karen>().Id) != true)
            return true;
        KarenModConfig.ShowMessage(
            new LocString("characters", "KAREN.title").GetFormattedText(),
            new LocString("settings_ui", "SHOUJOKAGEKIAIJOKAREN-ANDROID.karen_only_save").GetFormattedText());
        return false;
    }
}
