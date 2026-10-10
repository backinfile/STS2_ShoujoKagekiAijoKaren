using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

// Disposable test plugin; does not ship in Karen. Inspect after animation settles.
public sealed class VoidOrbitCmd : AbstractConsoleCmd
{
    public override string CmdName => "void_orbit_check";
    public override string Args => "";
    public override string Description => "Check one live orbit star per Draw card and HUD layer ownership.";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? player, string[] args)
    {
        var room = NCombatRoom.Instance;
        var tower = player == null ? null : room?.GetCreatureNode(player.Creature)
            ?.GetChildren().OfType<NKarenPromiseStarNode>().SingleOrDefault();
        if (tower == null || player!.PlayerCombatState == null || room == null)
            return new(false, "Combat required");
        object? Field(string name) => typeof(NKarenPromiseStarNode)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(tower);
        var stars = ((IDictionary)Field("_stars")!).Values.Cast<object>().ToArray();
        bool Present(object star) => (bool)star.GetType().GetField("Present")!.GetValue(star)!;
        CardModel Card(object star) => (CardModel)star.GetType().GetField("Card")!.GetValue(star)!;
        float Light(object star) => (float)star.GetType().GetField("Light")!.GetValue(star)!;
        var present = stars.Where(Present).ToArray();
        var models = player.PlayerCombatState.DrawPile.Cards;
        var icon = room.Ui.DrawPile.GetNode<TextureRect>("Icon");
        var layers = icon.GetChildren().OfType<Node2D>().Where(node =>
            node.Name.ToString().StartsWith("KarenVoidOrbit", StringComparison.Ordinal)).ToArray();
        bool usingIcon = Field("_voidOrbitIcon") == icon;
        bool behindIcon = layers.Any(node => node.Name == "KarenVoidOrbitBack" && node.ShowBehindParent);
        bool frontIcon = layers.Any(node => node.Name == "KarenVoidOrbitFront" && !node.ShowBehindParent);
        var screen = tower.GetViewportRect();
        var center = icon.GetGlobalTransformWithCanvas() * (icon.Size * 0.5f);
        var positions = present.Select(star => tower.GetGlobalTransformWithCanvas()
            * tower.ToLocal(tower.GetStarPosition(Card(star)))).ToArray();
        bool identitiesMatch = models.ToHashSet().SetEquals(present.Select(Card));
        bool inBounds = positions.All(point => screen.Grow(-2).HasPoint(point));
        float maxDistance = positions.Select(point => point.DistanceTo(center)).DefaultIfEmpty(0).Max();
        int litCount = stars.Count(star => Light(star) > 0.01f);
        bool valid = PromisePileManager.IsVoidMode(player) && usingIcon && identitiesMatch
            && litCount == models.Count && layers.Length == 2 && behindIcon && frontIcon
            && inBounds && maxDistance < 75;
        return new(valid, JsonSerializer.Serialize(new { modelCount = models.Count,
            starCount = present.Length, litCount, identitiesMatch, usingIcon,
            layerCount = layers.Length, behindIcon, frontIcon, inBounds, maxDistance,
            positions = positions.Select(point => new { x = point.X, y = point.Y }) }));
    }
}
