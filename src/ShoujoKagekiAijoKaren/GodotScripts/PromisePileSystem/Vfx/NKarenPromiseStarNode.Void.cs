using Godot;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenPromiseStarNode
{
    private float _voidStarted = -100;
    private bool _voidPending;
    private const float VoidScanEnd = 0.98f;
    private const float VoidTowerFadeDuration = 0.24f;
    private const float VoidProjectionStart = 0.72f;
    private const float VoidProjectionDuration = 0.29f;
    private static readonly Color VoidEchoGold = new("#ffe6b6");

    public void StartVoidActivation()
    {
        _voidStarted = _time;
        _voidPending = true;
        BeginVoidIconTransfer();
    }

    public void FinishVoidActivation() => _voidPending = false;

    private float VoidAge => _time - _voidStarted;
    private bool IsVoidPresentation => (_mode & PromisePileMode.Void) != 0 || _voidPending;
    // Keep the physical prop intact until the downward scan and its tail finish.
    // Then retire it before the inventory stars emerge around the Draw icon.
    // The node keeps updating that icon's inventory, trails and recall effects.
    internal float TowerVisualAlpha => _visibility * (IsVoidPresentation
        ? 1 - Ease((VoidAge - VoidScanEnd) / VoidTowerFadeDuration) : 1);
    private static float VoidScanPixelY(float age)
        => Mathf.Lerp(19, 1510, Mathf.Clamp((age - 0.18f) / 0.55f, 0, 1));
    private static float Ease(float value) => Mathf.SmoothStep(0, 1, value);

    private static Vector2 VoidProjectionFoot(int lane)
        => PaintedPoint(lane == 0 ? PaintedLeftLeg[0] : PaintedRightLeg[0]);

    private static float VoidProjectionProgress(float age)
        => Mathf.Clamp((age - VoidProjectionStart) / VoidProjectionDuration, 0, 1);

    // The light ribbon and its card stars use this same canvas-space route.
    private Vector2 VoidProjectionPoint(int lane, float progress)
    {
        var source = GetGlobalTransformWithCanvas() * VoidProjectionFoot(lane);
        var icon = _voidOrbitIcon!;
        var destination = icon.GetGlobalTransformWithCanvas() * (icon.Size * 0.5f);
        var control = new Vector2(Mathf.Lerp(source.X, destination.X, 0.6f),
            Mathf.Max(source.Y, destination.Y) + 22);
        return (1 - progress) * (1 - progress) * source
            + 2 * (1 - progress) * progress * control + progress * progress * destination;
    }

    private void DrawVoidScan(float alpha)
    {
        float age = VoidAge;
        if (age < 0.14f || age > VoidScanEnd) return;
        float fade = Ease((age - 0.14f) / 0.075f)
            * (1 - Ease((age - 0.75f) / (VoidScanEnd - 0.75f))) * alpha;
        float front = VoidScanPixelY(age);
        // Sweep the artwork's actual alpha, rather than the retired wireframe.
        for (float y = Mathf.Max(0, front - 400); y < Mathf.Min(1536, front + 60); y += 24)
        {
            float behind = (front - (y + 12)) / 4.18f;
            float light = Ease((behind + 12) / 24) * (1 - Ease((behind - 12) / 85)) * fade;
            if (light > 0.001f)
                DrawPaintedTowerStrip(y, y + 24, Vector2.Zero, new Color(2.5f, 2.5f, 2.1f, light * 0.48f));
        }
    }

    private void DrawVoidProjection(CanvasItem surface, float alpha)
    {
        float age = VoidAge;
        if (!UsingVoidIconOrbit || age < 0.7f || age > 1.75f) return;
        // This belongs to the HUD handoff, not the retiring physical tower.
        // Both ends are canvas coordinates, including creature shrink/flip.
        var icon = _voidOrbitIcon!;
        var iconCanvas = icon.GetGlobalTransformWithCanvas();
        var destination = iconCanvas * (icon.Size * 0.5f);
        surface.DrawSetTransformMatrix(surface.GetGlobalTransformWithCanvas().AffineInverse());
        float fade = Ease((age - 0.7f) / 0.055f)
            * (1 - Ease((age - 1.03f) / 0.2f)) * alpha;
        float progress = VoidProjectionProgress(age);
        for (int lane = 0; lane < 2; lane++)
        {
            for (int i = 18; i >= 0; i--)
            {
                float t = progress - i * 0.025f;
                if (t < 0) continue;
                var a = VoidProjectionPoint(lane, Mathf.Max(0, t - 0.025f));
                var b = VoidProjectionPoint(lane, t);
                float light = fade * (1 - i / 20f);
                surface.DrawLine(a, b, new Color(VoidEchoGold, light * 0.16f), 15, true);
                surface.DrawLine(a, b, new Color(VoidEchoGold, light * 0.4f), 7, true);
                surface.DrawLine(a, b, new Color(White, light), 3, true);
                if (i == 0) DrawStar(surface, b, 13, White, fade);
            }
        }
        // These brief lights rebuild the icon, even with an empty inventory.
        // They never enter _stars, which remains one live star per actual card.
        for (int i = 0; i < 24; i++)
        {
            float seed = ((i * 13) % 23) / 22f;
            float birth = 0.89f + (i % 6) * 0.028f;
            float flight = Ease((age - birth) / 0.28f);
            float light = Ease((age - birth) / 0.075f)
                * (1 - Ease((age - birth - 0.34f) / 0.3f)) * alpha;
            if (light <= 0) continue;
            var local = new Vector2((i % 4 - 1.5f) * 0.14f + (seed - 0.5f) * 0.04f,
                (i / 4 - 2.5f) * 0.115f - 0.045f) * icon.Size;
            var target = iconCanvas * (icon.Size * 0.5f + local);
            float angle = i * 2.399963f;
            var source = destination + new Vector2(12 + Mathf.Cos(angle) * (30 + seed * 20),
                -32 - seed * 42);
            source.X = Mathf.Max(7, source.X);
            var position = source.Lerp(target, flight);
            position.X += Mathf.Sin(flight * Mathf.Pi) * (seed - 0.5f) * 14;
            DrawStar(surface, position, 2.8f + seed * 2.4f,
                i % 3 == 0 ? VoidEchoGold : White, light);
        }
        surface.DrawSetTransform(Vector2.Zero);
    }

}
