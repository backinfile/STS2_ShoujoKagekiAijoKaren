using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace ShoujoKagekiAijoKaren.src.Core.Multiplayer;

internal static class RoomText
{
    public static string Get(string key)
    {
        var text = new LocString("settings_ui", "SHOUJOKAGEKIAIJOKAREN-ROOMS." + key);
        return text.Exists() ? text.GetFormattedText() : key;
    }
    public static string Error(Exception error) => Get(error is RoomServerException server ? server.Code :
        error is OperationCanceledException ? "timeout" : "connection_failed");
}

internal static class RoomUi
{
    public static void Message(string text)
    {
        if (!GodotObject.IsInstanceValid(NGame.Instance)) return;
        var dialog = new AcceptDialog { Title = RoomText.Get("title"), DialogText = text, MinSize = new Vector2I(620, 200) };
        dialog.Confirmed += dialog.QueueFree; dialog.Canceled += dialog.QueueFree; dialog.CloseRequested += dialog.QueueFree;
        NGame.Instance.AddChild(dialog); dialog.PopupCentered();
    }

    public static void Join(NJoinFriendScreen screen)
    {
        screen.GetNode<Control>("%ButtonContainer").Visible = false;
        screen.GetNode<Control>("%RefreshButton").Visible = false;
        screen.GetNode<Control>("%LoadingIndicator").Visible = false;
        screen.GetNode<Control>("%NoFriendsText").Visible = false;
        screen.GetNode<Control>("%LoadingOverlay").Visible = false;
        if (screen.GetNodeOrNull<Control>("KarenRoomJoin") is { } existing) { existing.Visible = true; return; }
        var panel = new VBoxContainer { Name = "KarenRoomJoin", AnchorLeft = .5f, AnchorRight = .5f,
            AnchorTop = .35f, AnchorBottom = .35f, OffsetLeft = -300, OffsetRight = 300, OffsetBottom = 300 };
        panel.AddThemeConstantOverride("separation", 22);
        panel.AddChild(new Label { Text = RoomText.Get("join_hint"), HorizontalAlignment = HorizontalAlignment.Center });
        var input = new LineEdit { Name = "RoomCode", PlaceholderText = RoomText.Get("code_placeholder"),
            MaxLength = RoomSocket.CodeLength, CustomMinimumSize = new Vector2(600, 64), Alignment = HorizontalAlignment.Center,
            VirtualKeyboardEnabled = true, VirtualKeyboardType = LineEdit.VirtualKeyboardTypeEnum.Number };
        input.AddThemeFontSizeOverride("font_size", 36);
        var join = new Button { Name = "JoinRoom", Text = RoomText.Get("join"), CustomMinimumSize = new Vector2(600, 64) };
        join.AddThemeFontSizeOverride("font_size", 28);
        panel.AddChild(input); panel.AddChild(join);
        panel.AddChild(new Label { Text = RoomText.Get("expires"), HorizontalAlignment = HorizontalAlignment.Center });
        async Task Submit()
        {
            if (join.Disabled) return;
            string code = input.Text.Trim();
            if (code.Length != RoomSocket.CodeLength || !System.Linq.Enumerable.All(code, c => c is >= '0' and <= '9'))
            { Message(RoomText.Get("invalid_code")); return; }
            join.Disabled = true; input.Editable = false;
            try { await screen.JoinGameAsync(new RoomClientInitializer(code)); }
            finally
            {
                if (GodotObject.IsInstanceValid(join)) join.Disabled = false;
                if (GodotObject.IsInstanceValid(input)) input.Editable = true;
            }
        }
        join.Pressed += () => TaskHelper.RunSafely(Submit());
        input.TextSubmitted += _ => TaskHelper.RunSafely(Submit());
        screen.AddChild(panel);
        input.GrabFocus();
    }

    public static void AttachBadge(Node? parent)
    {
        if (parent is not Control control || RoomSession.Code is not { } code) return;
        var old = control.GetNodeOrNull<Control>("KarenRoomBadge"); old?.QueueFree();
        var box = new HBoxContainer { Name = "KarenRoomBadge", AnchorLeft = .5f, AnchorRight = .5f,
            OffsetLeft = -210, OffsetRight = 210, OffsetTop = 80, OffsetBottom = 132 };
        box.AddThemeConstantOverride("separation", 16);
        var label = new Label { Text = RoomText.Get("code") + " " + code };
        label.AddThemeFontSizeOverride("font_size", 30);
        var copy = new Button { Text = RoomText.Get("copy"), CustomMinimumSize = new Vector2(90, 48) };
        copy.Pressed += () => { DisplayServer.ClipboardSet(code); copy.Text = RoomText.Get("copied"); };
        box.AddChild(label); box.AddChild(copy); control.AddChild(box);
    }
}
