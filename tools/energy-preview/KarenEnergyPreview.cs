using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

[ModInitializer(nameof(Initialize))]
public static class KarenEnergyPreview
{
    public static string Style { get; private set; } = "original";
    public static Texture2D? Texture { get; private set; }
    private static readonly Dictionary<string, Texture2D> Textures = new();
    private static readonly Dictionary<ulong, (Texture2D? Original, Dictionary<string, bool> Visible)> Counters = new();
    private static ulong _lastCounter;
    private static readonly string[] OtherLayers = {"Layers/RotationLayers", "Layers/Layer4", "Layers/Layer5", "MyEnergyVfxBack", "MyEnergyVfxFront", "BurstBack", "BurstFront"};
    public static void Initialize()
    {
        new Harmony("KarenEnergyPreview").PatchAll(Assembly.GetExecutingAssembly());
        RenderingServer.FramePostDraw += ObserveCounter;
        GD.Print("[KarenEnergyPreview] Loaded. Actual Karen card TextureRect and native energy counter replacement; no overlay.");
    }
    public static bool IsKaren(CardModel model) => model.VisualCardPool.EnergyColorName == "karen";
    private static Texture2D GetTexture(string style)
    {
        if (Textures.TryGetValue(style, out var cached)) return cached;
        var root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var image = Image.LoadFromFile(Path.Combine(root, style + ".png"));
        if (image == null || image.IsEmpty()) throw new InvalidOperationException("Missing preview texture: " + style);
        image.GenerateMipmaps();
        var source = ImageTexture.CreateFromImage(image);
        image.Dispose();
        Texture2D texture = source;
        if (style is "a" or "b" or "c" or "d" or "p9")
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "styles.json")));
            var asset = document.RootElement.GetProperty(style);
            float x = asset.GetProperty("x").GetSingle(), y = asset.GetProperty("y").GetSingle();
            float width = asset.GetProperty("width").GetSingle(), height = asset.GetProperty("height").GetSingle();
            // Wrap the unchanged generated PNG. Normalize transparent padding in the engine,
            // preserving the native EnergyIcon node position, pivot, size, and label.
            float side = Math.Max(width, height) / 0.95f;
            texture = new AtlasTexture {
                Atlas = source,
                Region = new Rect2(x, y, width, height),
                Margin = new Rect2((side-width)/2f, (side-height)/2f, side-width, side-height)
            };
        }
        Textures.Add(style, texture);
        return texture;
    }

    public static NEnergyCounter? Counter() => NCombatRoom.Instance?.Ui.EnergyCounterContainer.GetChildren()
        .OfType<NEnergyCounter>().FirstOrDefault(n => n.GetType().Name == "SNEnergyCounter");
    private static void ObserveCounter()
    {
        var counter = Counter();
        if (counter == null || !GodotObject.IsInstanceValid(counter)) { _lastCounter = 0; return; }
        if (_lastCounter == counter.GetInstanceId()) return;
        _lastCounter = counter.GetInstanceId();
        if (Style != "original") ApplyCounter(counter);
    }
    private static void ApplyCounter(NEnergyCounter counter)
    {
        var primary = counter.GetNode<TextureRect>("Layers/Layer1");
        var id = counter.GetInstanceId();
        if (!Counters.TryGetValue(id, out var saved))
        {
            saved = (primary.Texture, OtherLayers.Where(p => counter.GetNodeOrNull<CanvasItem>(p) != null)
                .ToDictionary(p => p, p => counter.GetNode<CanvasItem>(p).Visible));
            Counters.Add(id, saved);
            counter.TreeExiting += () => Counters.Remove(id);
        }
        primary.Texture = Style == "original" ? saved.Original : Texture;
        primary.TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps;
        foreach (var pair in saved.Visible)
            if (counter.GetNodeOrNull<CanvasItem>(pair.Key) is { } node)
                node.Visible = Style == "original" && pair.Value;
        // Keep native Layers/Label, 128x128 Size, energy notifications and zero-energy darkening.
    }
    private static IEnumerable<Node> Walk(Node node)
    {
        yield return node;
        foreach (var child in node.GetChildren())
            foreach (var descendant in Walk(child)) yield return descendant;
    }
    public static string Select(string style)
    {
        if (style is not ("original" or "crown" or "tomato" or "a" or "b" or "c" or "d" or "p9"))
            throw new ArgumentException("Style must be original, crown, tomato, a, b, c, d or p9.");
        Style = style;
        Texture = style == "original" ? null : GetTexture(style);
        if (Counter() is { } counter) ApplyCounter(counter);
        var root = ((SceneTree)Engine.GetMainLoop()).Root;
        foreach (var card in Walk(root).OfType<NCard>())
        {
            if (card.Model == null || !IsKaren(card.Model)) continue;
            if (card.GetNodeOrNull<TextureRect>("%EnergyIcon") is { } icon)
            {
                icon.Texture = card.Model.EnergyIcon;
                icon.TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps;
            }
        }
        GD.Print("[KarenEnergyPreview] Actual style set to " + style);
        return Report();
    }
    private static object? Layout(Control? node) => node == null ? null : new {
        position = new { x = node.Position.X, y = node.Position.Y },
        size = new { x = node.Size.X, y = node.Size.Y },
        scale = new { x = node.Scale.X, y = node.Scale.Y },
        pivot = new { x = node.PivotOffset.X, y = node.PivotOffset.Y },
        rotation = node.Rotation
    };
    private static object? RelativeToFrame(NCard card, TextureRect? icon)
    {
        var frame = card.GetNodeOrNull<Control>("%Frame");
        if (frame == null || icon == null) return null;
        var map = frame.GetGlobalTransform().AffineInverse() * icon.GetGlobalTransform();
        var top = map * Vector2.Zero;
        var center = map * (icon.Size / 2f);
        return new {
            topLeft = new { x = top.X, y = top.Y },
            center = new { x = center.X, y = center.Y }
        };
    }

    public static string Report()
    {
        var root = ((SceneTree)Engine.GetMainLoop()).Root;
        var cards = Walk(root).OfType<NCard>()
            .Where(c => c.Model != null && IsKaren(c.Model))
            .Select(c => {
                var icon = c.GetNodeOrNull<TextureRect>("%EnergyIcon");
                return new {
                    model = c.Model!.Id.ToString(), visible = c.IsVisibleInTree(),
                    cardSize = new { x = NCard.defaultSize.X, y = NCard.defaultSize.Y },
                    iconSize = icon == null ? null : new { x = icon.Size.X, y = icon.Size.Y },
                    usingCandidate = icon != null && Texture != null && icon.Texture == Texture,
                    globalIcon = icon == null ? null : new { x = icon.GetGlobalRect().Size.X, y = icon.GetGlobalRect().Size.Y },
                    iconLayout = Layout(icon), frameLayout = Layout(c.GetNodeOrNull<Control>("%Frame")),
                    labelLayout = Layout(icon?.GetNodeOrNull<Control>("EnergyLabel")),
                    iconRelativeToFrame = RelativeToFrame(c, icon)
                };
            }).ToArray();
        var counter = Counter();
        return JsonSerializer.Serialize(new {
            style = Style, normalizedPadding = Texture is AtlasTexture, cards,
            counter = counter == null ? null : new {
                size = new { x = counter.Size.X, y = counter.Size.Y },
                primarySize = new { x = counter.GetNode<Control>("Layers/Layer1").Size.X, y = counter.GetNode<Control>("Layers/Layer1").Size.Y },
                usingCandidate = Texture != null && counter.GetNode<TextureRect>("Layers/Layer1").Texture == Texture,
                label = counter.GetNode<Label>("Label").Text,
                modulate = counter.GetNode<Control>("Layers").Modulate.ToHtml(),
                rotationLayersVisible = counter.GetNode<CanvasItem>("Layers/RotationLayers").Visible,
                materialActive = counter.GetNode<Control>("Layers/Layer1").Material != null
            }
        });
    }
}
[HarmonyPatch(typeof(CardModel), "get_EnergyIcon")]
public static class KarenEnergyCardTexturePatch
{
    public static void Postfix(CardModel __instance, ref Texture2D __result)
    {
        if (KarenEnergyPreview.Texture != null && KarenEnergyPreview.IsKaren(__instance))
            __result = KarenEnergyPreview.Texture;
    }
}
public sealed class KarenEnergyPreviewCmd : AbstractConsoleCmd
{
    public override string CmdName => "karen_energy_preview";
    public override string Args => "original|crown|tomato|a|b|c|d|p9|status|energy N";
    public override string Description => "Developer-only actual card/counter texture A/B preview at native control sizes.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        try
        {
            if (args.Length == 0 || args[0] == "status") return new(true, KarenEnergyPreview.Report());
            if (args[0] == "energy" && args.Length == 2 && int.TryParse(args[1], out var amount) && amount >= 0 && player != null)
                return new(PlayerCmd.SetEnergy(amount, player), true, "Native PlayerCmd.SetEnergy: " + amount);
            return new(true, KarenEnergyPreview.Select(args[0]));
        }
        catch (Exception error) { return new(false, error.ToString()); }
    }
}
