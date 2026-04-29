using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenLastWordVfx : Node2D
{
    private const float SpawnInterval = 0.1f;
    private const float SpawnDuration = 4f;
    private const float ParticleDuration = 4f;
    private const int ParticlesPerWave = 2;

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

        var vfx = new NKarenLastWordVfx();
        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        vfx.GlobalPosition = Vector2.Zero;
    }

    public override void _Ready()
    {
        ZAsRelative = false;
        ZIndex = 1000;
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
                for (int i = 0; i < ParticlesPerWave; i++)
                    AddChild(new NKarenLastWordTParticle(PickTexture()));
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
}

internal partial class NKarenLastWordTParticle : Sprite2D
{
    private const float BaseWidth = 1920f;
    private static readonly CanvasItemMaterial AdditiveMaterial = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add
    };

    private readonly float _velocityX;
    private readonly float _velocityY;
    private readonly float _rotationSpeed;
    private readonly float _scaleY;
    private float _duration = 4f;

    public NKarenLastWordTParticle(Texture2D? texture)
    {
        Texture = texture;
        Centered = true;
        Material = AdditiveMaterial;

        var viewportSize = GetViewportSize();
        float scale = GetViewportScale(viewportSize.X);
        float baseScale = (float)GD.RandRange(1.0, 2.5) * scale;

        Position = new Vector2(
            (float)GD.RandRange(viewportSize.X * 0.05f, viewportSize.X * 0.95f),
            -(float)GD.RandRange(20.0, 300.0) * scale
        );
        RotationDegrees = (float)GD.RandRange(-10.0, 10.0);
        if (GD.Randf() < 0.5f)
            RotationDegrees += 180f;

        _scaleY = (float)GD.RandRange(1.0, 1.2);
        Scale = new Vector2(baseScale, baseScale * _scaleY);
        _velocityY = (float)GD.RandRange(200.0, 300.0) * baseScale;
        _velocityX = (float)GD.RandRange(-100.0, 100.0) * baseScale;
        _rotationSpeed = (float)GD.RandRange(10.0, 50.0);

        Modulate = new Color(251f / 255f, 84f / 255f, 88f / 255f, 1f);
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;
        Position += new Vector2(_velocityX * d, _velocityY * d);
        RotationDegrees += _rotationSpeed * d;

        _duration -= d;
        if (_duration < 1f)
            Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, Mathf.Max(0f, _duration));

        if (_duration < 0f)
            GodotTreeExtensions.QueueFreeSafely(this);
    }

    private static Vector2 GetViewportSize()
    {
        var tree = Engine.GetMainLoop() as SceneTree;
        return tree?.Root.GetViewport().GetVisibleRect().Size ?? new Vector2(1920f, 1080f);
    }

    private static float GetViewportScale(float width)
    {
        return Mathf.Max(0.5f, width / BaseWidth);
    }
}
