using System;
using System.Threading;
using System.Threading.Tasks;
using BaseLib.Config;
using Godot;
using MegaCrit.Sts2.Core.Helpers;

namespace ShoujoKagekiAijoKaren.src.Core.Multiplayer;

internal static class RoomConfigUi
{
    public static void Add(Control container)
    {
        var column = new VBoxContainer { Name = "KarenRoomServerSettings", CustomMinimumSize = new Vector2(700, 170) };
        column.AddThemeConstantOverride("separation", 12);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 18);
        row.AddChild(new Label { Text = RoomText.Get("mode"), CustomMinimumSize = new Vector2(220, 52) });
        var mode = new OptionButton { Name = "RoomMode", CustomMinimumSize = new Vector2(300, 52) };
        mode.AddItem(RoomText.Get("native"), 0); mode.AddItem(RoomText.Get("title"), 1);
        mode.Select(KarenModConfig.UseRoomServer ? 1 : 0);
        var probe = new Button { Name = "ProbeRoomServer", Text = RoomText.Get("probe"), CustomMinimumSize = new Vector2(180, 52) };
        var status = new Label { Name = "RoomServerStatus", Text = RoomText.Get("switch_hint"), AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(700, 48) };
        var expires = new Label { Text = RoomText.Get("expires") };
        row.AddChild(mode); row.AddChild(probe); column.AddChild(row); column.AddChild(status); column.AddChild(expires);
        container.AddChild(column);
        bool busy = false;
        async Task Check(bool switchMode)
        {
            if (busy) return;
            busy = true;
            bool previous = KarenModConfig.UseRoomServer;
            // Selection is committed only after the actual TLS/WebSocket protocol probe.
            mode.Select(previous ? 1 : 0);
            mode.Disabled = true; probe.Disabled = true; status.Text = RoomText.Get("probing");
            using var cancel = new CancellationTokenSource();
            column.TreeExiting += cancel.Cancel;
            try
            {
                int milliseconds = await RoomSocket.Probe(RoomSettings.Endpoint, cancel.Token);
                if (!GodotObject.IsInstanceValid(column) || !column.IsVisibleInTree()) return;
                if (switchMode)
                {
                    KarenModConfig.UseRoomServer = true;
                    ModConfig.SaveDebounced<KarenModConfig>(0);
                    mode.Select(1);
                }
                status.Text = RoomText.Get("available") + $" {milliseconds} ms";
            }
            catch (OperationCanceledException) when (cancel.IsCancellationRequested) { }
            catch (Exception error)
            {
                if (GodotObject.IsInstanceValid(status))
                    status.Text = RoomText.Get("unavailable") + " " + RoomText.Error(error);
            }
            finally
            {
                busy = false;
                if (GodotObject.IsInstanceValid(column))
                {
                    column.TreeExiting -= cancel.Cancel;
                    mode.Disabled = false; probe.Disabled = false;
                    mode.Select(KarenModConfig.UseRoomServer ? 1 : 0);
                }
            }
        }
        mode.ItemSelected += selected =>
        {
            if (selected == 0)
            {
                KarenModConfig.UseRoomServer = false;
                ModConfig.SaveDebounced<KarenModConfig>(0);
                status.Text = RoomText.Get("native_selected");
            }
            else TaskHelper.RunSafely(Check(true));
        };
        probe.Pressed += () => TaskHelper.RunSafely(Check(false));
    }
}
