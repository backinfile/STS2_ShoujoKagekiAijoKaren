using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem.Vfx;

public partial class NDisableRelicAbsorbVfx : Node2D
{
    public enum TargetKind { Player, Energy, DrawPile }
    private const int StarCount = 6;
    private const float ScatterDuration = 0.22f;
    private const float FlyDuration = 0.78f;
    private static readonly Texture2D? StarTexture = KarenResourceLoader.LoadTexture("res://images/packed/vfx/star_guide/star.png", nameof(NDisableRelicAbsorbVfx));

    private readonly List<NDisableRelicAbsorbStar> _stars = new();
    private readonly NCombatRoom _room;
    private readonly Player _owner;
    private readonly TargetKind _destination;
    private Vector2 _target;
    private float _elapsed;

    private NDisableRelicAbsorbVfx(Player owner, NCombatRoom room, Vector2 source, Vector2 target, TargetKind destination)
    {
        _owner = owner; _room = room; _destination = destination; _target = target;
        Name = "KarenRelicAbsorb";
        ZAsRelative = false;
        ZIndex = 120;

        for (int i = 0; i < StarCount; i++)
        {
            var star = new NDisableRelicAbsorbStar(StarTexture, source, destination == TargetKind.Player,
                room.GetViewport().GetVisibleRect().Size.Y / 720f);
            _stars.Add(star);
            AddChild(star);
        }
    }

    public static void Play(Player player, Vector2 source, TargetKind destination = TargetKind.Player)
    {
        if (player == null) return;
        if (NonInteractiveMode.IsActive || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant) return;
        if (NRun.Instance?.GlobalUi == null || NCombatRoom.Instance is not {} room) return;
        // Remote players must never consume the local player's HUD icons.
        if (!LocalContext.IsMe(player)) destination = TargetKind.Player;
        if (GetTarget(player, room, destination) is not {} target) return;

        var vfx = new NDisableRelicAbsorbVfx(player, room, source, target, destination);
        NRun.Instance.GlobalUi.AddChildSafely(vfx);
    }

    public override void _Ready() => GlobalTransform = GetCanvasTransform().AffineInverse();

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_room) || !_room.IsInsideTree() || NCombatRoom.Instance != _room
            || !CombatManager.Instance.IsInProgress || GetTarget(_owner, _room, _destination) is not {} target)
        { Hide(); QueueFree(); return; }
        // All coordinates are viewport coordinates, including moving/scaled HUD controls.
        GlobalTransform = GetCanvasTransform().AffineInverse();
        _target = target;
        _elapsed += (float)delta / (SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? .7f : 1f);
        foreach (var star in _stars.Where(s => GodotObject.IsInstanceValid(s) && !s.IsQueuedForDeletion()))
            star.UpdateMotion(_elapsed, ScatterDuration, FlyDuration, _target);

        if (_elapsed >= ScatterDuration + FlyDuration + 0.05f)
            GodotTreeExtensions.QueueFreeSafely(this);
    }

    private static Vector2? GetTarget(Player player, NCombatRoom room, TargetKind destination)
    {
        Control? icon = destination switch {
            TargetKind.Energy => room.Ui.EnergyCounterContainer.GetChildren().OfType<NEnergyCounter>().FirstOrDefault(),
            TargetKind.DrawPile => room.Ui.DrawPile.GetNodeOrNull<TextureRect>("Icon"),
            _ => room.GetCreatureNode(player.Creature)?.Hitbox
        };
        if (icon == null || !GodotObject.IsInstanceValid(icon) || !icon.IsInsideTree()) return null;
        var point = icon.GetGlobalTransformWithCanvas() * (icon.Size * .5f);
        return destination == TargetKind.Player ? point + new Vector2(0, -35) : point;
    }
}

internal partial class NDisableRelicAbsorbStar : Node2D
{
    private readonly Vector2 _source;
    private readonly Vector2 _scatterTarget;
    private readonly Vector2 _targetOffset;
    private readonly float _bend, _lift;
    private readonly float _rotationSpeed;
    private readonly float _scale;
    private readonly Color _color;

    public NDisableRelicAbsorbStar(Texture2D? texture, Vector2 source, bool scatterLanding, float unit)
    {
        _source = source;
        _targetOffset = scatterLanding ? new Vector2((float)GD.RandRange(-36.0, 36.0), (float)GD.RandRange(-44.0, 24.0)) * unit : Vector2.Zero;

        float scatterAngle = (float)GD.RandRange(0.0, Mathf.Tau);
        float scatterDistance = (float)GD.RandRange(22.0, 58.0) * unit;
        _scatterTarget = source + Vector2.FromAngle(scatterAngle) * scatterDistance;

        _bend = (float)GD.RandRange(65.0, 160.0) * (GD.Randf() < .5f ? -1f : 1f) * unit;
        _lift = (float)GD.RandRange(-100.0, 20.0) * unit;

        _rotationSpeed = (float)GD.RandRange(-10.0, 10.0);
        _scale = (float)GD.RandRange(0.075, 0.11) * unit;
        _color = new Color(1f, (float)GD.RandRange(0.82, 0.96), 0.18f, 1f);
        Position = _source;
        Scale = Vector2.One;

        if (texture != null)
        {
            AddChild(new Sprite2D
            {
                Texture = texture,
                Centered = true,
                Scale = Vector2.One * _scale,
                Modulate = _color,
                Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add }
            });
        }
        else
        {
            AddChild(CreateFallbackStar());
        }
    }

    public void UpdateMotion(float elapsed, float scatterDuration, float flyDuration, Vector2 destination)
    {
        if (elapsed < scatterDuration)
        {
            float t = Mathf.Clamp(elapsed / scatterDuration, 0f, 1f);
            Position = _source.Lerp(_scatterTarget, t);
            Scale = Vector2.One;
            Modulate = Colors.White;
            Rotation += _rotationSpeed * (float)GetProcessDeltaTime();
            return;
        }

        float flyTime = elapsed - scatterDuration;
        float flyT = Mathf.Clamp(flyTime / flyDuration, 0f, 1f);
        float easedFly = 1f - Mathf.Pow(1f - flyT, 2f);
        var target = destination + _targetOffset;
        var direction = (target - _scatterTarget).Normalized();
        var control = (_scatterTarget + target) * .5f + new Vector2(-direction.Y, direction.X) * _bend + new Vector2(0, _lift);
        Position = Bezier(_scatterTarget, control, target, easedFly);

        float lookAhead = Mathf.Clamp(easedFly + 0.03f, 0f, 1f);
        var next = Bezier(_scatterTarget, control, target, lookAhead);
        Rotation = Mathf.LerpAngle(Rotation, (next - Position).Angle() + Mathf.Pi / 2f, 0.25f);
        float shrinkT = Mathf.Clamp((flyT - 0.72f) / 0.28f, 0f, 1f);
        Scale = Vector2.One * Mathf.Lerp(1f, 0.15f, Mathf.Pow(shrinkT, 2f));
        Modulate = Colors.White;

        if (flyT >= 1f)
            GodotTreeExtensions.QueueFreeSafely(this);
    }

    private Polygon2D CreateFallbackStar()
    {
        var vertices = new Vector2[8];
        for (int i = 0; i < vertices.Length; i++)
        {
            float angle = i * Mathf.Pi / 4f - Mathf.Pi / 2f;
            float radius = i % 2 == 0 ? 5f : 2f;
            vertices[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }

        return new Polygon2D
        {
            Polygon = vertices,
            Color = _color,
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add }
        };
    }

    private static Vector2 Bezier(Vector2 start, Vector2 control, Vector2 end, float t)
    {
        float inv = 1f - t;
        return inv * inv * start + 2f * inv * t * control + t * t * end;
    }
}
