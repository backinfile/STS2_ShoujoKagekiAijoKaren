using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenLastWordVfx : Node2D
{
    private const float SpawnInterval = 0.035f;
    private const float SpawnDuration = 5.5f;
    private const float ParticleDuration = 5.5f;
    private const int ParticlesPerWave = 7;

    private static readonly Texture2D?[] Textures =
    [
        LoadTexture("res://images/packed/vfx/last_word/t01.png"),
        LoadTexture("res://images/packed/vfx/last_word/t02.png"),
        LoadTexture("res://images/packed/vfx/last_word/t03.png"),
        LoadTexture("res://images/packed/vfx/last_word/t04.png")
    ];

    private float _duration = SpawnDuration;
    private float _spawnTimer = SpawnInterval;

    public static void Play()
    {
        if (NCombatRoom.Instance == null) return;
        PlayOn(NCombatRoom.Instance.CombatVfxContainer);
    }

    public static NKarenLastWordVfx PlayOn(Node parent)
    {
        var vfx = new NKarenLastWordVfx();
        parent.AddChildSafely(vfx);
        vfx.GlobalPosition = Vector2.Zero;
        return vfx;
    }

    public override void _Ready()
    {
        ZAsRelative = true;
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;
        _duration -= d;

        if (_duration > 0f)
        {
            _spawnTimer -= d;
            while (_spawnTimer <= 0f)
            {
                _spawnTimer += SpawnInterval;
                var source = GetSourcePosition();
                var viewportSize = GetViewportSize();
                for (int i = 0; i < ParticlesPerWave; i++)
                    AddChild(new NKarenLastWordTParticle(PickTexture(), source, viewportSize));
            }
        }

        if (_duration < -ParticleDuration && GetChildCount() == 0)
            GodotTreeExtensions.QueueFreeSafely(this);
    }

    private static Texture2D? PickTexture()
    {
        if (Textures.Length == 0) return null;
        return Textures[(int)GD.RandRange(0, Textures.Length - 1)];
    }

    private static Texture2D? LoadTexture(string path)
    {
        if (ResourceLoader.Exists(path))
        {
            var texture = KarenResourceLoader.LoadTexture(path, nameof(NKarenLastWordVfx));
            if (texture != null)
                return texture;
        }

        var image = Image.LoadFromFile(path);
        return image == null ? null : ImageTexture.CreateFromImage(image);
    }

    private Vector2 GetSourcePosition()
    {
        var player = NCombatRoom.Instance?.CreatureNodes.FirstOrDefault(creature => creature.Entity.IsPlayer);
        if (player != null)
            return ToLocal(player.GlobalPosition + new Vector2(0f, -120f));

        var viewportSize = GetViewportSize();
        return new Vector2(viewportSize.X * 0.18f, viewportSize.Y * 0.62f);
    }

    private static Vector2 GetViewportSize()
    {
        var tree = Engine.GetMainLoop() as SceneTree;
        return tree?.Root.GetViewport().GetVisibleRect().Size ?? new Vector2(1920f, 1080f);
    }
}

internal partial class NKarenLastWordTParticle : Sprite2D
{
    private const float BaseWidth = 1920f;
    private static readonly CanvasItemMaterial AdditiveMaterial = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add
    };

    private readonly Vector2 _velocity;
    private readonly float _rotationSpeed;
    private readonly float _scaleY;
    private float _duration = 5.5f;

    public NKarenLastWordTParticle(Texture2D? texture, Vector2 source, Vector2 viewportSize)
    {
        Texture = texture;
        Centered = true;
        Material = AdditiveMaterial;
        ZAsRelative = true;

        float scale = GetViewportScale(viewportSize.X);
        float baseScale = (float)GD.RandRange(0.9, 2.2) * scale;

        Position = source + new Vector2(
            (float)GD.RandRange(-28.0, 28.0) * scale,
            (float)GD.RandRange(-36.0, 36.0) * scale
        );
        RotationDegrees = (float)GD.RandRange(-25.0, 25.0);
        if (GD.Randf() < 0.5f)
            RotationDegrees += 180f;

        var target = PickTarget(viewportSize);
        var direction = (target - Position).Normalized();
        if (direction == Vector2.Zero)
            direction = Vector2.Right.Rotated((float)GD.RandRange(0.0, Mathf.Tau));

        _scaleY = (float)GD.RandRange(1.0, 1.2);
        Scale = new Vector2(baseScale, baseScale * _scaleY);
        _velocity = direction * (float)GD.RandRange(360.0, 760.0) * scale;
        _rotationSpeed = (float)GD.RandRange(-80.0, 80.0);

        Modulate = new Color(251f / 255f, 84f / 255f, 88f / 255f, 1f);
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;
        Position += _velocity * d;
        RotationDegrees += _rotationSpeed * d;

        _duration -= d;
        if (_duration < 1f)
            Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, Mathf.Max(0f, _duration));

        if (_duration < 0f)
            GodotTreeExtensions.QueueFreeSafely(this);
    }

    private static Vector2 PickTarget(Vector2 viewportSize)
    {
        if (GD.Randf() < 0.72f)
        {
            return new Vector2(
                (float)GD.RandRange(viewportSize.X * -0.08f, viewportSize.X * 1.08f),
                (float)GD.RandRange(viewportSize.Y * -0.08f, viewportSize.Y * 1.08f)
            );
        }

        return (int)GD.RandRange(0, 3) switch
        {
            0 => new Vector2((float)GD.RandRange(0.0, viewportSize.X), -viewportSize.Y * 0.08f),
            1 => new Vector2(viewportSize.X * 1.08f, (float)GD.RandRange(0.0, viewportSize.Y)),
            2 => new Vector2((float)GD.RandRange(0.0, viewportSize.X), viewportSize.Y * 1.08f),
            _ => new Vector2(-viewportSize.X * 0.08f, (float)GD.RandRange(0.0, viewportSize.Y))
        };
    }

    private static float GetViewportScale(float width)
    {
        return Mathf.Max(0.5f, width / BaseWidth);
    }
}
