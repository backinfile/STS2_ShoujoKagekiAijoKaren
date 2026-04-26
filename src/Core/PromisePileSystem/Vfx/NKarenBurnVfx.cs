using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenBurnVfx : Node2D
{
    private static readonly Texture2D GlowSparkTexture = LoadTexture("res://images/vfx/sts/glow_spark.png");
    private static readonly Texture2D ExhaustLargeTexture = LoadTexture("res://images/vfx/sts/exhaust_l.png");

    private NCreature? _creatureNode;
    private float _particleTimer;
    private float _auraTimer;
    private bool _stopping;

    public void Init(NCreature creatureNode)
    {
        _creatureNode = creatureNode;
        ZAsRelative = true;
        ZIndex = 1;
    }

    public void Restart()
    {
        _stopping = false;
        Modulate = Colors.White;
        Visible = true;
        _particleTimer = 0f;
        _auraTimer = 0f;
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
        _particleTimer -= d;
        if (_particleTimer <= 0f)
        {
            _particleTimer = 0.05f;
            AddChild(new NKarenWrathParticle(GlowSparkTexture));
        }

        _auraTimer -= d;
        if (_auraTimer <= 0f)
        {
            _auraTimer = (float)GD.RandRange(0.3, 0.4);
            AddChild(new NKarenStanceAura(ExhaustLargeTexture));
        }
    }

    private static Texture2D LoadTexture(string path)
    {
        var image = Image.LoadFromFile(path);
        return ImageTexture.CreateFromImage(image);
    }
}

internal partial class NKarenWrathParticle : Sprite2D
{
    private const float PlayerWidth = 240f;
    private const float PlayerHeight = 320f;

    private readonly float _totalDuration;
    private readonly float _durDiv2;
    private readonly float _baseScale;
    private float _duration;
    private float _elapsed;

    public NKarenWrathParticle(Texture2D texture)
    {
        Texture = texture;
        Centered = true;

        _totalDuration = (float)GD.RandRange(1.3, 1.8);
        _duration = _totalDuration;
        _durDiv2 = _totalDuration / 2f;
        _baseScale = (float)GD.RandRange(0.6, 1.0);
        ZIndex = GD.Randf() < 0.2f + _baseScale - 0.5f ? 0 : 1;

        Position = new Vector2(
            (float)GD.RandRange(-PlayerWidth / 2f - 30f, PlayerWidth / 2f + 30f),
            (float)GD.RandRange(-PlayerHeight / 2f + 10f, PlayerHeight / 2f - 10f)
        );
        RotationDegrees = (float)GD.RandRange(-8.0, 8.0);
        Modulate = new Color((float)GD.RandRange(0.5, 1.0), 0f, (float)GD.RandRange(0.0, 0.2), 0f);
        Scale = new Vector2(_baseScale * 0.8f, 0.1f);

        var material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        Material = material;
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;
        _elapsed += d;

        if (_duration > _durDiv2)
            Modulate = WithAlpha(Modulate, Fade(1f, 0f, (_duration - _durDiv2) / _durDiv2));
        else
            Modulate = WithAlpha(Modulate, Fade(0f, 1f, _duration / _durDiv2));

        Position = new Vector2(Position.X, Position.Y - d * 40f);
        Scale = new Vector2(_baseScale * 0.8f, 0.1f + _elapsed * 2f * _baseScale);

        _duration -= d;
        if (_duration < 0f)
            GodotTreeExtensions.QueueFreeSafely(this);
    }

    private static float Fade(float from, float to, float t)
    {
        t = Mathf.Clamp(t, 0f, 1f);
        t = t * t * (3f - 2f * t);
        return Mathf.Lerp(from, to, t);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.R, color.G, color.B, alpha);
    }
}

internal partial class NKarenStanceAura : Sprite2D
{
    private const float PlayerWidth = 240f;
    private const float PlayerHeight = 320f;
    private static bool switcher = true;

    private readonly float _rotationVelocity;
    private float _duration = 2f;

    public NKarenStanceAura(Texture2D texture)
    {
        Texture = texture;
        Centered = true;
        Scale = Vector2.One * (float)GD.RandRange(2.5, 2.7);
        Modulate = new Color((float)GD.RandRange(0.6, 0.7), 0f, (float)GD.RandRange(0.1, 0.2), 0f);
        Position = new Vector2(
            (float)GD.RandRange(-PlayerWidth / 16f, PlayerWidth / 16f),
            (float)GD.RandRange(-PlayerHeight / 16f, PlayerHeight / 12f)
        );
        RotationDegrees = (float)GD.RandRange(0.0, 360.0);

        switcher = !switcher;
        if (switcher)
        {
            ZIndex = 0;
            _rotationVelocity = (float)GD.RandRange(0.0, 40.0);
        }
        else
        {
            ZIndex = 1;
            _rotationVelocity = (float)GD.RandRange(-40.0, 0.0);
        }

        var material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        Material = material;
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;

        if (_duration > 1f)
            Modulate = WithAlpha(Modulate, Fade(0.3f, 0f, _duration - 1f));
        else
            Modulate = WithAlpha(Modulate, Fade(0f, 0.3f, _duration));

        RotationDegrees += d * _rotationVelocity;
        _duration -= d;

        if (_duration < 0f)
            GodotTreeExtensions.QueueFreeSafely(this);
    }

    private static float Fade(float from, float to, float t)
    {
        t = Mathf.Clamp(t, 0f, 1f);
        t = t * t * (3f - 2f * t);
        return Mathf.Lerp(from, to, t);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.R, color.G, color.B, alpha);
    }
}
