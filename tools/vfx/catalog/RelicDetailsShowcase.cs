using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using MegaCrit.Sts2.Core.Saves;

// Recording fixture only: arrange unmodified native relic inspection popups.
public static class RelicDetailsShowcase
{
    private static readonly List<(RelicModel Model, NInspectRelicScreen Screen)> Screens = new();
    private static object? Field(object target, string name) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target);

    public static void Show(string[] ids)
    {
        if (ids.Length is < 1 or > 3) throw new ArgumentException("Show one to three native details.");
        Clear();
        var original = NGame.Instance!.GetInspectRelicScreen();
        original.Hide();
        var parent = original.GetParent();
        float scale = ids.Length == 3 ? .70f : .95f;
        float width = 864 * scale, gap = ids.Length == 3 ? 18 : 70;
        float left = (1920 - width * ids.Length - gap * (ids.Length - 1)) / 2;
        for (int i = 0; i < ids.Length; i++)
        {
            var relic = ModelDb.AllRelics.Single(r => r.Id.Entry == ids[i]);
            SaveManager.Instance.MarkRelicAsSeen(relic);
            var screen = NInspectRelicScreen.Create()!;
            parent.AddChild(screen);
            screen.Open(new[] { relic }, relic);
            // Only this isolated recording fixture exposes the requested models.
            ((HashSet<RelicModel>)Field(screen, "_allUnlockedRelics")!).Add(relic.CanonicalInstance);
            typeof(NInspectRelicScreen).GetMethod("SetRelic", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(screen, new object[] { 0 });
            (Field(screen, "_screenTween") as Tween)?.Kill();
            (Field(screen, "_popupTween") as Tween)?.Kill();
            screen.SetProcessInput(false);
            var backstop = screen.GetNode<Control>("%Backstop");
            backstop.Visible = i == 0;
            backstop.Modulate = new Color(0, 0, 0, .9f);
            var popup = screen.GetNode<Control>("%Popup");
            popup.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
            popup.Size = new Vector2(864, 864);
            popup.Scale = Vector2.One * scale;
            popup.Position = new Vector2(left + i * (width + gap), (1080 - width) / 2);
            popup.Modulate = Colors.White;
            Screens.Add((relic, screen));
        }
        NHoverTipSet.Clear();
    }

    public static string Status() => JsonSerializer.Serialize(new
    {
        nativeScene = "NInspectRelicScreen",
        panels = Screens.Select(item => new
        {
            id = item.Model.Id.Entry,
            title = item.Screen.GetNode<Label>("%RelicName").Text,
            rarity = item.Screen.GetNode<Label>("%Rarity").Text,
            description = item.Screen.GetNode<RichTextLabel>("%RelicDescription").GetParsedText(),
            flavor = item.Screen.GetNode<RichTextLabel>("%FlavorText").GetParsedText(),
            rect = item.Screen.GetNode<Control>("%Popup").GetGlobalRect().ToString(),
            scale = item.Screen.GetNode<Control>("%Popup").Scale.X,
            visible = item.Screen.IsVisibleInTree()
        }).ToArray()
    });

    public static void Clear()
    {
        foreach (var item in Screens)
            if (GodotObject.IsInstanceValid(item.Screen)) { item.Screen.Close(); item.Screen.QueueFree(); }
        Screens.Clear();
        NHoverTipSet.Clear();
    }
}

public sealed class RelicDetailsShowcaseCommand : AbstractConsoleCmd
{
    public override string CmdName => "relic_details";
    public override string Args => "show ID[,ID...]|status|close";
    public override string Description => "Arrange native relic inspection pages for recording.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (args.FirstOrDefault() == "show" && args.Length == 2)
        {
            RelicDetailsShowcase.Show(args[1].Split(','));
            return new(true, RelicDetailsShowcase.Status());
        }
        if (args.FirstOrDefault() == "close") { RelicDetailsShowcase.Clear(); return new(true, "closed"); }
        return new(true, RelicDetailsShowcase.Status());
    }
}
