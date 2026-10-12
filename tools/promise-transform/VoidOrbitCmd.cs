using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

// Disposable test plugin; does not ship in Karen. Inspect after animation settles.
public sealed class VoidOrbitCmd : AbstractConsoleCmd
{
    private static VoidTransitionProbe? _transition;
    public override string CmdName => "void_orbit_check";
    public override string Args => "[presentation|watch|timeline]";
    public override string Description => "Check one live orbit star per Draw card and HUD layer ownership.";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? player, string[] args)
    {
        var room = NCombatRoom.Instance;
        var tower = player == null ? null : room?.GetCreatureNode(player.Creature)
            ?.GetChildren().OfType<NKarenPromiseStarNode>().SingleOrDefault();
        if (tower == null || player!.PlayerCombatState == null || room == null)
            return new(false, "Combat required");
        if (args.Length == 1 && args[0] == "watch")
        {
            _transition?.Dispose();
            _transition = new VoidTransitionProbe(tower, room.Ui.DrawPile.GetNode<TextureRect>("Icon"));
            return new(true, "Watching the Void tower transition on every frame");
        }
        if (args.Length == 1 && args[0] == "timeline")
            return new(_transition != null, JsonSerializer.Serialize(_transition?.Samples));
        object? Field(string name) => typeof(NKarenPromiseStarNode)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(tower);
        var stars = ((IDictionary)Field("_stars")!).Values.Cast<object>().ToArray();
        bool Present(object star) => (bool)star.GetType().GetField("Present")!.GetValue(star)!;
        CardModel Card(object star) => (CardModel)star.GetType().GetField("Card")!.GetValue(star)!;
        float Light(object star) => (float)star.GetType().GetField("Light")!.GetValue(star)!;
        var present = stars.Where(Present).ToArray();
        var models = player.PlayerCombatState.DrawPile.Cards;
        var icon = room.Ui.DrawPile.GetNode<TextureRect>("Icon");
        // Godot may assign an automatic name when a presentation is recreated
        // before its old children finish QueueFree. Verify actual ownership.
        var layers = new[] { Field("_voidOrbitBack") as Node2D, Field("_voidOrbitFront") as Node2D }
            .Where(node => node != null && GodotObject.IsInstanceValid(node) && node.GetParent() == icon)
            .Cast<Node2D>().ToArray();
        bool usingIcon = Field("_voidOrbitIcon") == icon;
        bool behindIcon = layers.Any(node => node == Field("_voidOrbitBack") && node.ShowBehindParent);
        bool frontIcon = layers.Any(node => node == Field("_voidOrbitFront") && !node.ShowBehindParent);
        float towerAlpha = (float)typeof(NKarenPromiseStarNode).GetProperty("TowerVisualAlpha",
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(tower)!;
        bool towerHit = tower.GetNode<Control>("PromiseTowerHitbox")
            ._HasPoint(new Vector2(263, 1310) * .3475f);
        if (args.Length == 1 && args[0] == "presentation")
        {
            var burn = room.GetCreatureNode(player.Creature)!.GetChildren().OfType<NKarenBurnVfx>().SingleOrDefault();
            object? BurnField(string name) => burn?.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(burn);
            return new(true, JsonSerializer.Serialize(new {
                voidMode = PromisePileManager.IsVoidMode(player), towerAlpha, towerHit, usingIcon,
                layerCount = layers.Length, starCount = present.Length, litCount = stars.Count(s => Light(s) > .01f),
                voidAge = (float)Field("_time")! - (float)Field("_voidStarted")!,
                previewAlpha = Field("_recallPreviewAlpha"), profile = Field("_transferPreview")?.ToString(),
                flameAlpha = (BurnField("_towerFlames") as CanvasItem)?.Modulate.A,
                smokeAlpha = (BurnField("_towerSmoke") as CanvasItem)?.Modulate.A,
                iconFire = BurnField("_drawFire") is Node2D fire && GodotObject.IsInstanceValid(fire),
                screen = NCapstoneContainer.Instance?.CurrentCapstoneScreen?.GetType().Name
            }));
        }
        var screen = tower.GetViewportRect();
        var center = icon.GetGlobalTransformWithCanvas() * (icon.Size * 0.5f);
        var positions = present.Select(star => tower.GetGlobalTransformWithCanvas()
            * tower.ToLocal(tower.GetStarPosition(Card(star)))).ToArray();
        bool identitiesMatch = models.ToHashSet().SetEquals(present.Select(Card));
        bool inBounds = positions.All(point => screen.Grow(-2).HasPoint(point));
        float maxDistance = positions.Select(point => point.DistanceTo(center)).DefaultIfEmpty(0).Max();
        int litCount = stars.Count(star => Light(star) > 0.01f);
        bool valid = PromisePileManager.IsVoidMode(player) && towerAlpha < .01f && !towerHit && usingIcon && identitiesMatch
            && litCount == models.Count && layers.Length == 2 && behindIcon && frontIcon
            && inBounds && maxDistance < 75;
        return new(valid, JsonSerializer.Serialize(new { modelCount = models.Count,
            starCount = present.Length, litCount, identitiesMatch, usingIcon,
            layerCount = layers.Length, behindIcon, frontIcon, inBounds, maxDistance,
            towerAlpha, towerHit, positions = positions.Select(point => new { x = point.X, y = point.Y }) }));
    }
}

// Read-only, frame-by-frame timing evidence; HTTP polling can skip the scan tail.
internal sealed class VoidTransitionProbe : IDisposable
{
    private readonly NKarenPromiseStarNode _tower;
    private readonly TextureRect _icon;
    private readonly SceneTree _tree;
    private bool _disposed;
    private static readonly FieldInfo Time = typeof(NKarenPromiseStarNode).GetField("_time", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo Started = typeof(NKarenPromiseStarNode).GetField("_voidStarted", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly PropertyInfo Alpha = typeof(NKarenPromiseStarNode).GetProperty("TowerVisualAlpha", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo Stars = typeof(NKarenPromiseStarNode).GetField("_stars", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly MethodInfo OrbitLight = typeof(NKarenPromiseStarNode).GetMethod("VoidOrbitLight", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly MethodInfo PaintedPoint = typeof(NKarenPromiseStarNode).GetMethod("PaintedPoint", BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly Vector2[] Feet = new[] { "PaintedLeftLeg", "PaintedRightLeg" }
        .Select(name => ((Vector2[])typeof(NKarenPromiseStarNode).GetField(name, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!)[0]).ToArray();
    public sealed record CardLight(int slot, float x, float y, float light);
    public sealed record Beam(float x, float y, float controlX, float controlY, float endX, float endY);
    public sealed record Sample(float voidAge, float towerAlpha, float iconAlpha, CardLight[] cardStars, Beam[] beams);
    public List<Sample> Samples { get; } = new();
    public VoidTransitionProbe(NKarenPromiseStarNode tower, TextureRect icon)
    {
        _tower = tower;
        _icon = icon;
        _tree = tower.GetTree();
        _tree.ProcessFrame += Capture;
        tower.TreeExiting += Dispose;
    }
    private void Capture()
    {
        float age = (float)Time.GetValue(_tower)! - (float)Started.GetValue(_tower)!;
        if (age >= 0 && age <= 3)
        {
            var cards = ((IDictionary)Stars.GetValue(_tower)!).Values.Cast<object>()
                .Where(star => (bool)star.GetType().GetField("Present")!.GetValue(star)!)
                .Select(star => {
                    var card = (CardModel)star.GetType().GetField("Card")!.GetValue(star)!;
                    var position = _tower.GetStarCanvasPosition(card);
                    float light = (float)star.GetType().GetField("Light")!.GetValue(star)!
                        * (float)OrbitLight.Invoke(_tower, new[] { star })!;
                    return new CardLight((int)star.GetType().GetField("Slot")!.GetValue(star)!, position.X, position.Y, light);
                }).ToArray();
            var end = _icon.GetGlobalTransformWithCanvas() * (_icon.Size * .5f);
            var beams = Feet.Select(foot => {
                var start = _tower.GetGlobalTransformWithCanvas() * (Vector2)PaintedPoint.Invoke(null, new object[] { foot })!;
                return new Beam(start.X, start.Y, Mathf.Lerp(start.X, end.X, .6f),
                    Mathf.Max(start.Y, end.Y) + 22, end.X, end.Y);
            }).ToArray();
            Samples.Add(new(age, (float)Alpha.GetValue(_tower)!, _icon.SelfModulate.A, cards, beams));
        }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (GodotObject.IsInstanceValid(_tree)) _tree.ProcessFrame -= Capture;
        if (GodotObject.IsInstanceValid(_tower)) _tower.TreeExiting -= Dispose;
    }
}
