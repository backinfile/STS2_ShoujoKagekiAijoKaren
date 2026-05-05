using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Relics;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem.Vfx;

public partial class NDisableRelicStarBurstVfx : Node2D
{
    private const float SpawnInterval = 0.08f;
    private static readonly Texture2D? StarTexture = KarenResourceLoader.LoadTexture("res://images/packed/vfx/star_guide/star.png", nameof(NDisableRelicStarBurstVfx));

    private readonly NRelicInventoryHolder _holder;
    private float _spawnTimer;
    private bool _stopping;

    private NDisableRelicStarBurstVfx(NRelicInventoryHolder holder)
    {
        _holder = holder;
        ZAsRelative = true;
        ZIndex = 40;
    }

    public static NDisableRelicStarBurstVfx Start(NRelicInventoryHolder holder)
    {
        var vfx = new NDisableRelicStarBurstVfx(holder);
        holder.AddChildSafely(vfx);
        vfx.Position = holder.Size * 0.5f;
        return vfx;
    }

    public void Stop()
    {
        if (_stopping) return;

        _stopping = true;
        if (!IsInsideTree())
        {
            GodotTreeExtensions.QueueFreeSafely(this);
            return;
        }

        GetTree().CreateTimer(0.7).Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(this))
                GodotTreeExtensions.QueueFreeSafely(this);
        };
    }

    public override void _Process(double delta)
    {
        if (_holder != null && GodotObject.IsInstanceValid(_holder))
            Position = _holder.Size * 0.5f;

        if (_stopping) return;

        _spawnTimer -= (float)delta;
        while (_spawnTimer <= 0f)
        {
            _spawnTimer += SpawnInterval;
            float angle = (float)GD.RandRange(0.0, Mathf.Tau);
            float distance = (float)GD.RandRange(20.0, 46.0);
            AddChild(new NDisableRelicBurstStar(StarTexture, angle, distance));
        }
    }
}

internal partial class NDisableRelicBurstStar : Node2D
{
    private readonly float _angle;
    private readonly float _distance;
    private readonly float _duration;
    private readonly float _rotationSpeed;
    private float _elapsed;
    private Color _color;

    public NDisableRelicBurstStar(Texture2D? texture, float angle, float distance)
    {
        _angle = angle;
        _distance = distance;
        _duration = (float)GD.RandRange(0.45, 0.7);
        _rotationSpeed = (float)GD.RandRange(-9.0, 9.0);
        _color = new Color(1f, (float)GD.RandRange(0.82, 0.95), 0.18f, 1f);
        _elapsed = (float)GD.RandRange(0.06, 0.14);
        Position = Vector2.FromAngle(_angle) * (float)GD.RandRange(6.0, 14.0);
        Scale = Vector2.One * 0.18f;

        if (texture != null)
        {
            AddChild(new Sprite2D
            {
                Texture = texture,
                Centered = true,
                Scale = Vector2.One * (float)GD.RandRange(0.18, 0.27),
                Modulate = _color,
                Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add }
            });
        }
        else
        {
            AddChild(CreateFallbackStar());
        }
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;
        _elapsed += d;

        float t = Mathf.Clamp(_elapsed / _duration, 0f, 1f);
        float eased = 1f - Mathf.Pow(1f - t, 2f);
        Position = Vector2.FromAngle(_angle) * Mathf.Lerp(6f, _distance, eased);
        Rotation += _rotationSpeed * d;
        Scale = Vector2.One * Mathf.Lerp(0.18f, 0.48f, t);
        Modulate = new Color(1f, 1f, 1f, 1f - t);

        if (t >= 1f)
            GodotTreeExtensions.QueueFreeSafely(this);
    }

    private Polygon2D CreateFallbackStar()
    {
        var vertices = new Vector2[8];
        for (int i = 0; i < vertices.Length; i++)
        {
            float angle = i * Mathf.Pi / 4f - Mathf.Pi / 2f;
            float radius = i % 2 == 0 ? 2f : 0.8f;
            vertices[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }

        return new Polygon2D
        {
            Polygon = vertices,
            Color = _color,
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add }
        };
    }
}
