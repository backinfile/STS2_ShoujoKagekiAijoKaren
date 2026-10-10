using Godot;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenPromiseStarNode
{
    private float _voidStarted = -100;
    private bool _voidPending;

    public void StartVoidActivation()
    {
        _voidStarted = _time;
        _voidPending = true;
        BeginVoidIconTransfer();
    }

    public void FinishVoidActivation() => _voidPending = false;

    private float VoidAge => _time - _voidStarted;
    private float VoidFront => Mathf.Lerp(18, 375, Mathf.Clamp((VoidAge - 0.18f) / 0.55f, 0, 1));
    private static float Ease(float value) => Mathf.SmoothStep(0, 1, value);

    private void DrawVoidScan(float alpha)
    {
        float age = VoidAge;
        if (age < 0.14f || age > 0.98f) return;
        float fade = Ease((age - 0.14f) / 0.075f) * (1 - Ease((age - 0.75f) / 0.23f)) * alpha;
        float front = Mathf.Lerp(19, 1510, (VoidFront - 18) / 357);
        // Sweep the artwork's actual alpha, rather than the retired wireframe.
        for (float y = Mathf.Max(0, front - 400); y < Mathf.Min(1536, front + 60); y += 24)
        {
            float behind = (front - (y + 12)) / 4.18f;
            float light = Ease((behind + 12) / 24) * (1 - Ease((behind - 12) / 85)) * fade;
            if (light > 0.001f)
                DrawPaintedTowerStrip(y, y + 24, Vector2.Zero, new Color(2.5f, 2.5f, 2.1f, light * 0.48f));
        }
    }

}
