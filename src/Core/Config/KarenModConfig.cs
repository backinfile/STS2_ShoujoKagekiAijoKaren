using BaseLib.Config;
using Godot;
using MegaCrit.Sts2.Core.Nodes;
using ShoujoKagekiAijoKaren.src.Core.Vfx;

namespace ShoujoKagekiAijoKaren;

public static class KarenModConfig
{
    [ConfigIgnore]
    public static KarenVfxLevel VfxLevel
    {
        get => (KarenVfxLevel)(int)ShoujoKageki.Core.V1.CoreApi.Configuration.Current.VfxLevel;
        set => ShoujoKageki.Core.V1.CoreApi.Configuration.Update(
            ShoujoKageki.Core.V1.CoreApi.Configuration.Current with
            {
                VfxLevel = (ShoujoKageki.Core.V1.CoreVfxLevel)(int)value
            });
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

}
