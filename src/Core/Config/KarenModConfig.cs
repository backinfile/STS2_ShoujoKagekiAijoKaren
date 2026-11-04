using BaseLib.Config;
using BaseLib.Config.UI;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using System;

namespace ShoujoKagekiAijoKaren;

public class KarenModConfig : SimpleModConfig
{
    private static bool _isGeneratingModInfoHtml;

    private static volatile bool _enableErrorUpload = true;
    public static bool EnableErrorUpload
    {
        get => _enableErrorUpload;
        set
        {
            _enableErrorUpload = value;
            src.Core.Telemetry.KarenErrorReporter.ConfigurationChanged();
        }
    }

    /// <summary>
    /// 上传对局数据
    /// </summary>
    private static bool _enableDataUpload = true;
    public static bool EnableDataUpload
    {
        get => _enableDataUpload;
        set
        {
            _enableDataUpload = value;
            if (value) src.Core.Patches.RunHistoryUploadPatch.ResumePendingUploads();
        }
    }

    /// <summary>
    /// 显示长颈鹿启动画面
    /// </summary>
    [ConfigHideInUI]
    public static bool ShowSplashScreen { get; set; } = true;

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

    private static bool _enableMusicPlayer = true;
    [ConfigHideInUI]
    public static bool EnableMusicPlayer
    {
        get => _enableMusicPlayer;
        set { _enableMusicPlayer=value; src.Core.MusicPlayer.NKarenMusicPlayer.ConfigurationChanged(); }
    }

    private static string _musicDirectory = "";
    [ConfigHideInUI]
    public static string MusicDirectory
    {
        get => _musicDirectory;
        set { _musicDirectory=value ?? ""; src.Core.MusicPlayer.NKarenMusicPlayer.ConfigurationChanged(); }
    }

    [ConfigHideInUI]
    public static float MusicPlayerVolume { get; set; } = .7f;
    [ConfigHideInUI]
    public static bool MusicPlayerMinimized { get; set; } = true;
    private static src.Core.MusicPlayer.KarenMusicPlayMode _musicPlayMode;
    [ConfigHideInUI]
    public static src.Core.MusicPlayer.KarenMusicPlayMode MusicPlayMode
    {
        get => _musicPlayMode;
        set { _musicPlayMode=Enum.IsDefined(value)?value:src.Core.MusicPlayer.KarenMusicPlayMode.List; src.Core.MusicPlayer.NKarenMusicPlayer.ConfigurationChanged(); }
    }
    [ConfigHideInUI]
    public static string MusicPlayPauseKey { get; set; } = "F8";
    [ConfigHideInUI]
    public static string MusicNextKey { get; set; } = "F9";
    [ConfigHideInUI]
    public static string MusicPreviousKey { get; set; } = "F7";



    /// <summary>
    /// 生成卡牌截图与卡牌信息索引，供 mod 信息 HTML 使用。
    /// </summary>
    public override void SetupConfigUI(Control optionContainer)
    {
        AddRestoreDefaultsButton(optionContainer);
        var reset = optionContainer.GetNodeOrNull<Control>("ResetDefaultsButtonContainer");

        var effects = CreateCollapsibleSection("VisualEffects");
        effects.Name = "VisualEffectsSettings";
        optionContainer.AddChild(effects);
        if (reset != null) optionContainer.MoveChild(effects, reset.GetIndex());
        var type = typeof(KarenModConfig);
        foreach (string name in new[] { nameof(ShowSplashScreen), nameof(VfxLevel) })
        {
            if (effects.ContentContainer.GetChildCount() > 0) effects.ContentContainer.AddChild(CreateDividerControl());
            effects.ContentContainer.AddChild(GenerateOptionFromProperty(type.GetProperty(name)!));
        }

        var music = CreateCollapsibleSection("MusicPlayer", collapsedByDefault: true);
        music.Name = "MusicPlayerSettings";
        optionContainer.AddChild(music);
        if (reset != null) optionContainer.MoveChild(music, reset.GetIndex());
        void AddMusicRow(Control row)
        {
            if (music.ContentContainer.GetChildCount() > 0) music.ContentContainer.AddChild(CreateDividerControl());
            music.ContentContainer.AddChild(row);
        }
        AddMusicRow(CreateToggleOption(type.GetProperty(nameof(EnableMusicPlayer))!));
        AddMusicRow(CreateStandardOption(p => new src.Core.MusicPlayer.NKarenMusicSetting(this, p, CreateRawButtonControl), type.GetProperty(nameof(MusicDirectory))!));
        AddMusicRow(CreateDropdownOption(type.GetProperty(nameof(MusicPlayMode))!));
        foreach (string name in new[] { nameof(MusicPlayPauseKey), nameof(MusicNextKey), nameof(MusicPreviousKey) })
            AddMusicRow(CreateStandardOption(p => new src.Core.MusicPlayer.NKarenMusicSetting(this, p, CreateRawButtonControl), type.GetProperty(name)!));

        var other = CreateCollapsibleSection("Other", collapsedByDefault: true);
        other.Name = "OtherSettings";
        optionContainer.AddChild(other);
        if (reset != null) optionContainer.MoveChild(other, reset.GetIndex());

        void AddActionRow(string labelKey, string actionKey, Action<NConfigButton> action)
        {
            NConfigOptionRow row = null!;
            row = CreateButton(labelKey, actionKey, () => action((NConfigButton)row.SettingControl));
            if (other.ContentContainer.GetChildCount() > 0) other.ContentContainer.AddChild(CreateDividerControl());
            other.ContentContainer.AddChild(row);
        }
        AddActionRow("GenerateModInfoHtmlButton", "ExportAction", GenerateModInfoHtml);
        AddActionRow("ExportJsonOnlyButton", "ExportAction", ExportJsonOnly);
        AddActionRow("UnlockAllButton", "UnlockAction", UnlockAll);
        other.ContentContainer.AddChild(CreateDividerControl());
        // The property stays visible to BaseLib; create its control directly in this group.
        other.ContentContainer.AddChild(GenerateOptionFromProperty(type.GetProperty(nameof(EnableDataUpload))!));
        other.ContentContainer.AddChild(CreateDividerControl());
        other.ContentContainer.AddChild(GenerateOptionFromProperty(type.GetProperty(nameof(EnableErrorUpload))!));

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

    private static async void GenerateModInfoHtml(NConfigButton button)
    {
        if (_isGeneratingModInfoHtml)
        {
            ShowExportMessage(false, "busy");
            return;
        }

        var confirmed = await ConfirmGenerateModInfoHtml();
        if (!confirmed)
            return;

        _isGeneratingModInfoHtml = true;
        button.Disable();

        const string outputPath = "user://KarenCardExports/";
        try
        {
            var result = await CardPngExportUtil.ExportAllKarenCardsAsync(outputPath, 1f, msg =>
            {
                MainFile.Logger.Info(msg);
            });

            if (result.Success)
            {
                ShowExportMessage(false, "htmlComplete",
                    ("OutputDirectory", result.OutputDirectory), ("CardCount", result.CardCount),
                    ("ReferenceCardCount", result.ReferenceCardCount), ("SavedImages", result.SavedImages));
            }
            else
            {
                ShowExportMessage(false, "failed", ("Error", result.Error ?? GetExportText("unknownError")));
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Generate mod info html failed: {ex}");
            ShowExportMessage(false, "failed", ("Error", ex.Message));
        }
        finally
        {
            _isGeneratingModInfoHtml = false;
            if (GodotObject.IsInstanceValid(button))
                button.Enable();
        }
    }

    private static async void ExportJsonOnly(NConfigButton button)
    {
        if (_isGeneratingModInfoHtml)
        {
            ShowExportMessage(true, "busy");
            return;
        }

        _isGeneratingModInfoHtml = true;
        button.Disable();

        const string outputPath = "user://KarenCardExports/";
        try
        {
            var result = await CardPngExportUtil.ExportJsonOnlyAsync(outputPath, msg =>
            {
                MainFile.Logger.Info(msg);
            });

            if (result.Success)
            {
                ShowExportMessage(true, "jsonComplete",
                    ("OutputDirectory", result.OutputDirectory), ("CardCount", result.CardCount));
            }
            else
            {
                ShowExportMessage(true, "failed", ("Error", result.Error ?? GetExportText("unknownError")));
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Export json only failed: {ex}");
            ShowExportMessage(true, "failed", ("Error", ex.Message));
        }
        finally
        {
            _isGeneratingModInfoHtml = false;
            if (GodotObject.IsInstanceValid(button))
                button.Enable();
        }
    }

    private static async System.Threading.Tasks.Task<bool> ConfirmGenerateModInfoHtml()
    {
        var confirmationModal = NGenericPopup.Create();
        if (confirmationModal == null || NModalContainer.Instance == null)
            return await ConfirmGenerateModInfoHtmlFallback();

        NModalContainer.Instance.Add(confirmationModal);
        return await confirmationModal.WaitForConfirmation(
            body: new LocString("settings_ui", "SHOUJOKAGEKIAIJOKAREN-GENERATE_MOD_INFO_HTML_CONFIRM.body"),
            header: new LocString("settings_ui", "SHOUJOKAGEKIAIJOKAREN-GENERATE_MOD_INFO_HTML_CONFIRM.header"),
            noButton: new LocString("main_menu_ui", "GENERIC_POPUP.cancel"),
            yesButton: new LocString("main_menu_ui", "GENERIC_POPUP.confirm")
        );
    }

    private static async System.Threading.Tasks.Task<bool> ConfirmGenerateModInfoHtmlFallback()
    {
        if (NGame.Instance == null)
            return true;

        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
        var dialog = new ConfirmationDialog
        {
            Title = GetSettingsText("SHOUJOKAGEKIAIJOKAREN-GENERATE_MOD_INFO_HTML_CONFIRM.header", "Confirm Export"),
            DialogText = GetSettingsText("SHOUJOKAGEKIAIJOKAREN-GENERATE_MOD_INFO_HTML_CONFIRM.body", "Export card screenshots and index files? This may take some time."),
            MinSize = new Vector2I(640, 240),
        };

        dialog.Confirmed += () =>
        {
            tcs.TrySetResult(true);
            dialog.QueueFree();
        };
        dialog.Canceled += () =>
        {
            tcs.TrySetResult(false);
            dialog.QueueFree();
        };
        dialog.CloseRequested += () =>
        {
            tcs.TrySetResult(false);
            dialog.QueueFree();
        };

        NGame.Instance.AddChild(dialog);
        dialog.PopupCentered();
        return await tcs.Task;
    }

    private static string GetExportText(string entry, params (string Name, object Value)[] variables)
    {
        var text = new LocString("settings_ui", $"SHOUJOKAGEKIAIJOKAREN-EXPORT.{entry}");
        foreach (var (name, value) in variables)
            text.AddObj(name, value);
        return text.GetFormattedText();
    }

    private static void ShowExportMessage(bool jsonOnly, string entry, params (string Name, object Value)[] variables)
    {
        var title = jsonOnly
            ? GetSettingsText("SHOUJOKAGEKIAIJOKAREN-EXPORT_JSON_ONLY_BUTTON.title", "Export JSON Only")
            : GetSettingsText("SHOUJOKAGEKIAIJOKAREN-GENERATE_MOD_INFO_HTML_BUTTON.title", "Export Mod Info");
        ShowMessage(title, GetExportText(entry, variables));
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
