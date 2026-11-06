using BaseLib.Config;
using BaseLib.Config.UI;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using System;

namespace ShoujoKagekiAijoKaren;

public class KarenModConfig : SimpleModConfig
{
    private static KarenVfxLevel _vfxLevel = KarenVfxLevel.Full;
    [ConfigHideInUI]
    public static KarenVfxLevel VfxLevel
    {
        get => _vfxLevel;
        set
        {
            var level = Enum.IsDefined(value) ? value : KarenVfxLevel.Full;
            if (_vfxLevel == level) return;
            _vfxLevel = level;
            KarenVfxQuality.ApplyLevel();
        }
    }

    public override void SetupConfigUI(Control optionContainer)
    {
        AddRestoreDefaultsButton(optionContainer);
        optionContainer.AddChild(GenerateOptionFromProperty(typeof(KarenModConfig).GetProperty(nameof(VfxLevel))!));
        NConfigOptionRow row = null!;
        row = CreateButton("UnlockAllButton", "UnlockAction", () => UnlockAll((NConfigButton)row.SettingControl));
        optionContainer.AddChild(row);
        SetupFocusNeighbors(optionContainer);
    }

    private static void UnlockAll(NConfigButton button)
    {
        string title = GetSettingsText("SHOUJOKAGEKIAIJOKAREN-UNLOCK_ALL_BUTTON.title", "Unlock All");
        // A run captures unlock state at creation; apply the new progress to the next run.
        if (GodotObject.IsInstanceValid(NRun.Instance))
        {
            ShowMessage(title, GetSettingsText("SHOUJOKAGEKIAIJOKAREN-UNLOCK_ALL.inRun", "Return to the main menu before unlocking."));
            return;
        }
        button.Disable();
        try
        {
            string backup = KarenUnlockAll.Apply();
            var message = new LocString("settings_ui", "SHOUJOKAGEKIAIJOKAREN-UNLOCK_ALL.complete");
            message.AddObj("Backup", backup);
            ShowMessage(title, message.GetFormattedText());
        }
        catch (Exception error)
        {
            MainFile.Logger.Error($"Unlock all failed: {error}");
            var message = new LocString("settings_ui", "SHOUJOKAGEKIAIJOKAREN-UNLOCK_ALL.failed");
            message.AddObj("Error", error.Message);
            ShowMessage(title, message.GetFormattedText());
        }
        finally
        {
            if (GodotObject.IsInstanceValid(button)) button.Enable();
        }
    }

    internal static void ShowMessage(string title, string message)
    {
        if (NGame.Instance == null)
        {
            MainFile.Logger.Info($"{title}: {message}");
            return;
        }

        var dialog = new AcceptDialog
        {
            Title = title,
            DialogText = message,
            MinSize = new Vector2I(560, 220),
        };

        dialog.Confirmed += dialog.QueueFree;
        dialog.Canceled += dialog.QueueFree;
        dialog.CloseRequested += dialog.QueueFree;

        NGame.Instance.AddChild(dialog);
        dialog.PopupCentered();
    }

    private static string GetSettingsText(string key, string fallback)
    {
        try
        {
            return new LocString("settings_ui", key).GetRawText();
        }
        catch
        {
            return fallback;
        }
    }
}
