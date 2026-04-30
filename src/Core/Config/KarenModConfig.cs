using BaseLib.Config;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System;

namespace ShoujoKagekiAijoKaren;

public class KarenModConfig : SimpleModConfig
{
    private static bool _isGeneratingModInfoHtml;

    /// <summary>
    /// 上传对局数据
    /// </summary>
    public static bool EnableDataUpload { get; set; } = true;

    /// <summary>
    /// 显示长颈鹿启动画面
    /// </summary>
    public static bool ShowSplashScreen { get; set; } = true;

    /// <summary>
    /// 生成卡牌截图与卡牌信息索引，供 mod 信息 HTML 使用。
    /// </summary>
    public override void SetupConfigUI(Control optionContainer)
    {
        base.SetupConfigUI(optionContainer);

        var button = new KarenConfigImageButton(
            GetSettingsText("SHOUJOKAGEKIAIJOKAREN-GENERATE_MOD_INFO_HTML_BUTTON.title", "生成mod信息html"),
            GenerateModInfoHtml);
        var jsonButton = new KarenConfigImageButton(
            GetSettingsText("SHOUJOKAGEKIAIJOKAREN-EXPORT_JSON_ONLY_BUTTON.title", "仅导出JSON"),
            ExportJsonOnly);

        var centerContainer = new CenterContainer
        {
            CustomMinimumSize = new Vector2(0, 88),
        };

        var buttonRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        buttonRow.AddThemeConstantOverride("separation", 18);
        buttonRow.AddChild(button);
        buttonRow.AddChild(jsonButton);
        centerContainer.AddChild(buttonRow);
        optionContainer.AddChild(centerContainer);
        optionContainer.MoveChild(centerContainer, 0);
    }

    private static async void GenerateModInfoHtml(KarenConfigImageButton button)
    {
        if (_isGeneratingModInfoHtml)
        {
            ShowMessage("生成mod信息html", "正在生成中，请稍等。");
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
                ShowMessage(
                    "生成mod信息html",
                    $"生成完成。\n输出目录：{result.OutputDirectory}\nKaren 卡牌：{result.CardCount}\n引用卡牌：{result.ReferenceCardCount}\n图片：{result.SavedImages}");
            }
            else
            {
                ShowMessage(
                    "生成mod信息html",
                    $"生成时遇到问题：{result.Error ?? "未知错误"}\n请查看日志获取详细信息。");
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Generate mod info html failed: {ex}");
            ShowMessage("生成mod信息html", $"生成失败：{ex.Message}");
        }
        finally
        {
            _isGeneratingModInfoHtml = false;
            if (GodotObject.IsInstanceValid(button))
                button.Enable();
        }
    }

    private static async void ExportJsonOnly(KarenConfigImageButton button)
    {
        if (_isGeneratingModInfoHtml)
        {
            ShowMessage("仅导出JSON", "正在生成中，请稍等。");
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
                ShowMessage(
                    "仅导出JSON",
                    $"导出完成。\n输出目录：{result.OutputDirectory}\nKaren 卡牌：{result.CardCount}");
            }
            else
            {
                ShowMessage(
                    "仅导出JSON",
                    $"导出时遇到问题：{result.Error ?? "未知错误"}\n请查看日志获取详细信息。");
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Export json only failed: {ex}");
            ShowMessage("仅导出JSON", $"导出失败：{ex.Message}");
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
            Title = "确认导出",
            DialogText = "将开始导出 mod 信息 HTML 所需的卡牌截图与索引文件。这个过程可能需要一些时间，是否继续？",
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

    private static void ShowMessage(string title, string message)
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

public partial class KarenConfigImageButton : NSettingsButton
{
    private readonly Action<KarenConfigImageButton> _onPressed;

    public KarenConfigImageButton(string text, Action<KarenConfigImageButton> onPressed)
    {
        _onPressed = onPressed;

        CustomMinimumSize = new Vector2(324, 64);
        SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        SizeFlagsVertical = SizeFlags.Fill;
        FocusMode = FocusModeEnum.All;

        var image = new TextureRect
        {
            Name = "Image",
            CustomMinimumSize = new Vector2(64, 64),
            Texture = ResourceLoader.Load<Texture2D>("res://images/ui/configbutton.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = MouseFilterEnum.Ignore,
            SelfModulate = Color.FromHtml("#3b7a83"),
        };
        image.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(image);

        var font = ResourceLoader.Load<FontVariation>("res://themes/kreon_bold_shared.tres");
        var label = new MegaLabel
        {
            Name = "Label",
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
            LabelSettings = new LabelSettings
            {
                Font = font,
                FontSize = 28,
                FontColor = new Color(0.91f, 0.86f, 0.74f),
                OutlineSize = 12,
                OutlineColor = new Color(0.29f, 0.14f, 0.14f),
            },
        };
        label.AddThemeFontOverride("font", font);
        label.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(label);

        var reticleScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/selection_reticle.tscn");
        var reticle = reticleScene.Instantiate<NSelectionReticle>();
        reticle.Name = "SelectionReticle";
        reticle.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        reticle.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(reticle);
    }

    public override void _Ready()
    {
        ConnectSignals();
    }

    protected override void OnRelease()
    {
        base.OnRelease();
        _onPressed(this);
    }
}
