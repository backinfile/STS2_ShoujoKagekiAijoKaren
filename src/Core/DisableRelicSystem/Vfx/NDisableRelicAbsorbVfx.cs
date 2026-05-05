using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem.Vfx;

public partial class NDisableRelicAbsorbVfx : Node2D
{
    private const int StarCount = 6;
    private const float ScatterDuration = 0.22f;
    private const float FlyDuration = 0.78f;
    private static readonly Texture2D? StarTexture = KarenResourceLoader.LoadTexture("res://images/packed/vfx/star_guide/star.png", nameof(NDisableRelicAbsorbVfx));

    private readonly List<NDisableRelicAbsorbStar> _stars = new();
    private float _elapsed;

    private NDisableRelicAbsorbVfx(Vector2 source, Vector2 target)
    {
        GlobalPosition = Vector2.Zero;
        ZAsRelative = false;
        ZIndex = 120;

        for (int i = 0; i < StarCount; i++)
        {
            var star = new NDisableRelicAbsorbStar(StarTexture, source, target);
            _stars.Add(star);
            AddChild(star);
        }
    }

    public static void Play(Player player, Vector2 source)
    {
        if (player == null) return;
        if (NRun.Instance?.GlobalUi == null || NCombatRoom.Instance == null) return;

        var target = GetPlayerTarget(player);
        var vfx = new NDisableRelicAbsorbVfx(source, target);
        NRun.Instance.GlobalUi.AddChildSafely(vfx);
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        foreach (var star in _stars.Where(GodotObject.IsInstanceValid))
            star.UpdateMotion(_elapsed, ScatterDuration, FlyDuration);

        if (_elapsed >= ScatterDuration + FlyDuration + 0.05f)
            GodotTreeExtensions.QueueFreeSafely(this);
    }

    private static Vector2 GetPlayerTarget(Player player)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        if (creatureNode != null)
            return creatureNode.VfxSpawnPosition + new Vector2(0f, -35f);

        var viewportSize = NGame.Instance?.GetViewportRect().Size
            ?? (Engine.GetMainLoop() as SceneTree)?.Root.GetViewport().GetVisibleRect().Size
            ?? new Vector2(1920f, 1080f);
        return new Vector2(viewportSize.X * 0.25f, viewportSize.Y * 0.58f);
    }
}

internal partial class NDisableRelicAbsorbStar : Node2D
{
    private readonly Vector2 _source;
    private readonly Vector2 _scatterTarget;
    private readonly Vector2 _target;
    private readonly Vector2 _control;
    private readonly float _rotationSpeed;
    private readonly float _scale;
    private readonly Color _color;

    public NDisableRelicAbsorbStar(Texture2D? texture, Vector2 source, Vector2 target)
    {
        _source = source;
        _target = target + new Vector2((float)GD.RandRange(-36.0, 36.0), (float)GD.RandRange(-44.0, 24.0));

        float scatterAngle = (float)GD.RandRange(0.0, Mathf.Tau);
        float scatterDistance = (float)GD.RandRange(22.0, 58.0);
        _scatterTarget = source + Vector2.FromAngle(scatterAngle) * scatterDistance;

        var midpoint = (_scatterTarget + _target) * 0.5f;
        var direction = (_target - _scatterTarget).Normalized();
        var normal = new Vector2(-direction.Y, direction.X);
        float bend = (float)GD.RandRange(90.0, 260.0) * (GD.Randf() < 0.5f ? -1f : 1f);
        _control = midpoint + normal * bend + new Vector2(0f, (float)GD.RandRange(-160.0, 60.0));

        _rotationSpeed = (float)GD.RandRange(-10.0, 10.0);
        _scale = (float)GD.RandRange(0.075, 0.11);
        _color = new Color(1f, (float)GD.RandRange(0.82, 0.96), 0.18f, 1f);
        GlobalPosition = _source;
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

    public void UpdateMotion(float elapsed, float scatterDuration, float flyDuration)
    {
        if (elapsed < scatterDuration)
        {
            float t = Mathf.Clamp(elapsed / scatterDuration, 0f, 1f);
            GlobalPosition = _source.Lerp(_scatterTarget, t);
            Scale = Vector2.One;
            Modulate = Colors.White;
            Rotation += _rotationSpeed * (float)GetProcessDeltaTime();
            return;
        }

        float flyTime = elapsed - scatterDuration;
        float flyT = Mathf.Clamp(flyTime / flyDuration, 0f, 1f);
        float easedFly = 1f - Mathf.Pow(1f - flyT, 2f);
        GlobalPosition = Bezier(_scatterTarget, _control, _target, easedFly);

        float lookAhead = Mathf.Clamp(easedFly + 0.03f, 0f, 1f);
        var next = Bezier(_scatterTarget, _control, _target, lookAhead);
        Rotation = Mathf.LerpAngle(Rotation, (next - GlobalPosition).Angle() + Mathf.Pi / 2f, 0.25f);
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
