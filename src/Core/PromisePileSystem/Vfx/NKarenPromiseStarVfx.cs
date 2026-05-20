using Godot;
using MegaCrit.Sts2.Core.Helpers;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>
/// 约定牌堆星星特效：用 Polygon2D 绘制四角星，可环绕指定半径旋转
/// </summary>
public partial class NKarenPromiseStarVfx : Polygon2D
{
    public float OrbitRadius { get; set; } = 100f;
    public float OrbitAngle { get; set; }
    public float OrbitSpeed { get; set; } = 1.8f;
    public float FlyInSpeedMultiplier { get; set; } = 1.5f;
    public bool IsOrbiting { get; private set; }

    private float _flyInProgress;
    private CpuParticles2D? _trail;

    public NKarenPromiseStarVfx()
    {
        Color = new Color("#f9f2d5");
        DrawStar(5f, 2f, 4);

        var trail = new CpuParticles2D
        {
            Amount = 32,
            Lifetime = 0.5f,
            Emitting = true,
            Gravity = Vector2.Zero,
            InitialVelocityMin = 0f,
            InitialVelocityMax = 0f,
            ScaleAmountMin = 0.4f,
            ScaleAmountMax = 1.0f,
            Color = new Color("#f9f2d5", 0.5f)
        };
        var curve = new Curve();
        curve.AddPoint(new Vector2(0, 1));
        curve.AddPoint(new Vector2(1, 0));
        trail.ScaleAmountCurve = curve;
        AddChild(trail);
        _trail = trail;
    }

    private void DrawStar(float outerRadius, float innerRadius, int points)
    {
        var vertices = new Vector2[points * 2];
        for (int i = 0; i < points * 2; i++)
        {
            float angle = i * Mathf.Pi / points - Mathf.Pi / 2f;
            float radius = (i % 2 == 0) ? outerRadius : innerRadius;
            vertices[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }
        Polygon = vertices;
    }

    public async void FlyIn(float targetAngle)
    {
        Scale = Vector2.Zero;
        IsOrbiting = false;
        OrbitAngle = targetAngle;
        _flyInProgress = 0f;

        using var tween = CreateTween().SetParallel();
        tween.TweenMethod(new Callable(this, nameof(SetFlyInProgress)), 0.0, 1.0, 0.4f)
            .SetTrans(Tween.TransitionType.Linear);
        tween.TweenProperty(this, "scale", Vector2.One, 0.35f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);

        await ToSignal(tween, Tween.SignalName.Finished);
        IsOrbiting = true;
    }

    private void SetFlyInProgress(float progress)
    {
        _flyInProgress = progress;
    }

    public async void FlyOut()
    {
        IsOrbiting = false;
        using var tween = CreateTween().SetParallel();
        tween.TweenProperty(this, "position", Vector2.Zero, 0.3f)
            .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(this, "scale", Vector2.Zero, 0.25f)
            .SetEase(Tween.EaseType.In);
        await ToSignal(tween, Tween.SignalName.Finished);

        if (_trail != null && IsInstanceValid(_trail))
        {
            _trail.Emitting = false;
            RemoveChild(_trail);
            _trail.TopLevel = true;
            _trail.GlobalPosition = GlobalPosition;
            var tree = GetTree();
            if (tree != null)
            {
                tree.Root.AddChild(_trail);
                tree.CreateTimer(_trail.Lifetime).Timeout += () => _trail?.QueueFree();
            }
            else
            {
                _trail.QueueFree();
            }
        }

        GodotTreeExtensions.QueueFreeSafely(this);
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;
        float speed = IsOrbiting ? OrbitSpeed : OrbitSpeed * FlyInSpeedMultiplier;
        OrbitAngle += speed * d;

        float radius = IsOrbiting ? OrbitRadius : _flyInProgress * OrbitRadius;
        Position = new Vector2(Mathf.Cos(OrbitAngle) * radius, Mathf.Sin(OrbitAngle) * radius);
        Rotation = OrbitAngle + Mathf.Pi / 2f;
    }
}
