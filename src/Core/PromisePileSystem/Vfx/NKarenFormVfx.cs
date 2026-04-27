using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenFormVfx : Node2D
{
    private static readonly Texture2D? HorizontalLineTexture = LoadTexture("res://images/vfx/sts/horizontal_line.png");

    private NCreature? _creatureNode;
    private float _timer;
    private bool _stopping;

    public void Init(NCreature creatureNode)
    {
        _creatureNode = creatureNode;
        ZAsRelative = true;
        ZIndex = 2;
    }

    public void Restart()
    {
        _stopping = false;
        Modulate = Colors.White;
        Visible = true;
        _timer = 0f;
    }

    public void Stop()
    {
        _stopping = true;
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", new Color(1f, 1f, 1f, 0f), 0.25f);
        tween.Finished += () => GodotTreeExtensions.QueueFreeSafely(this);
    }

    public override void _Ready()
    {
        Restart();
    }

    public override void _Process(double delta)
    {
        if (_creatureNode != null)
            GlobalPosition = _creatureNode.VfxSpawnPosition;

        if (_stopping) return;

        float d = (float)delta;
        _timer -= d;
        if (_timer <= 0f)
        {
            _timer += (float)GD.RandRange(0.2, 0.4);
            AddChild(new NKarenWindyParticle(HorizontalLineTexture, GlobalPosition, reverse: false));
        }
    }

    private static Texture2D? LoadTexture(string path)
    {
        return KarenResourceLoader.LoadTexture(path, nameof(NKarenFormVfx));
    }
}

internal partial class NKarenWindyParticle : Sprite2D
{
    private readonly float _velocityX;
    private readonly float _velocityY;
    private readonly float _rotationVelocity;
    private float _duration;

    public NKarenWindyParticle(Texture2D? texture, Vector2 parentGlobalPosition, bool reverse)
    {
        Texture = texture;
        Centered = true;

        var viewportSize = GetViewportSize();
        float width = viewportSize.X;
        float height = viewportSize.Y;
        float scale = GetViewportScale(width);

        float x;
        float velocityX;
        if (reverse)
        {
            x = (float)GD.RandRange(-260.0, -80.0) * scale;
            velocityX = (float)GD.RandRange(1500.0, 2500.0) * scale;
        }
        else
        {
            x = width + (float)GD.RandRange(80.0, 260.0) * scale;
            velocityX = (float)GD.RandRange(-2500.0, -1500.0) * scale;
        }

        var globalSpawnPosition = new Vector2(x, (float)GD.RandRange(0.15, 0.85) * height);
        Position = globalSpawnPosition - parentGlobalPosition;
        _velocityX = velocityX;
        _velocityY = (float)GD.RandRange(-100.0, 100.0) * scale;
        _rotationVelocity = (float)GD.RandRange(0.5, 0.0);
        _duration = 1.25f;

        RotationDegrees = 0f;
        Scale = new Vector2((float)GD.RandRange(0.5, 0.9), (float)GD.RandRange(1.0, 2.0) * scale);
        Modulate = new Color(0.28f, 0.1f, 0.08f, 0.9f);

        var material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        Material = material;
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;
        Position += new Vector2(_velocityX * d, _velocityY * d);
        RotationDegrees += _rotationVelocity * d;
        _duration -= d;

        if (_duration < 0f || Position.X > GetViewportSize().X + 500f || Position.X < -500f)
            GodotTreeExtensions.QueueFreeSafely(this);
    }

    private static Vector2 GetViewportSize()
    {
        var tree = Engine.GetMainLoop() as SceneTree;
        return tree?.Root.GetViewport().GetVisibleRect().Size ?? new Vector2(1920f, 1080f);
    }

    private static float GetViewportScale(float width)
    {
        return Mathf.Max(0.5f, width / 1920f);
    }
}
