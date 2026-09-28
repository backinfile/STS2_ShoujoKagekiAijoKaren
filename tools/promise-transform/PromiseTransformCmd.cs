using System;
using System.Linq;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

[ModInitializer(nameof(Initialize))]
public static class TransformTestInit { public static void Initialize() {} }

// Disposable test plugin: not shipped in Karen.
public sealed class PromiseTransformCmd : AbstractConsoleCmd
{
    public override string CmdName => "promise_transform";
    public override string Args => "normal|small|large|body_flip|small_flip|visual_flip|rotate|stretch|offset|check";
    public override string Description => "Set a deterministic visual transform or check tower placement.";
    public override bool IsNetworked => false;
    private static Transform2D? _visual, _body;
    public override CmdResult Process(Player? player, string[] args)
    {
        var creature = player == null ? null : NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        if (creature == null || args.Length != 1) return new(false, "Combat required");
        var tower = creature.GetChildren().OfType<NKarenPromiseStarNode>().Single();
        _visual ??= creature.Visuals.Transform;
        _body ??= creature.Body.Transform;
        if (args[0] != "check")
        {
            creature.Visuals.Transform = _visual.Value;
            creature.Body.Transform = _body.Value;
            switch (args[0]) {
                case "normal": break;
                case "small": creature.ScaleTo(0.5f, 0); break;
                case "large": creature.ScaleTo(1.5f, 0); break;
                case "body_flip": creature.Body.Scale *= new Vector2(-1, 1); break;
                case "small_flip": creature.ScaleTo(0.5f, 0); creature.Body.Scale *= new Vector2(-1, 1); break;
                case "visual_flip": creature.Visuals.Scale *= new Vector2(-1, 1); break;
                case "rotate": creature.Visuals.Rotation = 0.3f; break;
                case "stretch": creature.Visuals.Scale *= new Vector2(0.65f, 1.2f); break;
                case "offset": creature.Visuals.Position += new Vector2(80, -35); break;
                default: return new(false, "Unknown transform");
            }
            return new(true, args[0]);
        }
        var visual = creature.Visuals.GlobalTransform;
        var actual = tower.GlobalTransform;
        var expected = new Vector2(visual.X.Length(), visual.Y.Length());
        var originError = tower.GlobalPosition.DistanceTo(creature.VfxSpawnPosition);
        var scaleError = new Vector2(actual.X.Length(), actual.Y.Length()).DistanceTo(expected);
        var upright = actual.X.X > 0 && actual.Y.Y > 0 && Math.Abs(actual.X.Y) < 0.001f && Math.Abs(actual.Y.X) < 0.001f;
        var expectedAnchor = creature.VfxSpawnPosition + new Vector2(-45, -160) * expected;
        var anchorError = tower.TransferGlobalPosition.DistanceTo(expectedAnchor);
        return new(originError < 0.1f && scaleError < 0.001f && upright && anchorError < 0.1f,
            JsonSerializer.Serialize(new { originError, scaleError, upright, anchorError, expectedX=expected.X, actualX=actual.X.Length() }));
    }
}
