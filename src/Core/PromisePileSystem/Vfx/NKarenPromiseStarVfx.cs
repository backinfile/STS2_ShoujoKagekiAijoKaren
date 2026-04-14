using Godot;
using MegaCrit.Sts2.Core.Helpers;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>
/// 约定牌堆星星特效：用 Polygon2D 绘制四角星，可环绕指定半径旋转
/// </summary>
public partial class NKarenPromiseStarVfx : Polygon2D
{
    public float OrbitRadius { get; set; } = 28f;
    public float OrbitAngle { get; set; }
    public float OrbitSpeed { get; set; } = 1.8f;
    public float FlyInSpeedMultiplier { get; set; } = 1.5f;
    public bool IsOrbiting { get; private set; }

    private float _flyInProgress;

    public NKarenPromiseStarVfx()
    {
        Color = new Color("#f9f2d5");
        DrawStar(10f, 4f, 4);
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
