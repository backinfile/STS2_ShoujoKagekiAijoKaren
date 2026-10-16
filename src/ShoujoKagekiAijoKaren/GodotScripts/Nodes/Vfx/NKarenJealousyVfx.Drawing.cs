using Godot;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

public partial class NKarenJealousyVfx
{
    private static readonly Color Hot = new("#f4fff9"), Mint = new("#b5f4d7");

    private void Halo(Vector2 point, float radius, Color color, float alpha)
    {
        if (alpha > .001f) _light.DrawTextureRect(_soft!, new Rect2(point - Vector2.One * radius * _unit,
            Vector2.One * radius * 2 * _unit), false, new Color(color, alpha));
    }
    // Only the attack's main shape is drawn here; decoration lives in native particle emitters.
    private void DrawLight()
    {
        float flash = Mathf.Exp(-_age * 40);
        Halo(_target, 65, Mint, flash * .48f);
        float fadeOut = 1 - Mathf.SmoothStep(.08f, .21f, _age);
        if (fadeOut < .005f) return;
        float end = Mathf.SmoothStep(0, .085f, _age);
        var previous = CutPoint(0);
        for (int i = 1; i <= 28; i++) {
            float t = end * i / 28f;
            var point = CutPoint(t);
            float taper = Mathf.Pow(Mathf.Sin(t * Mathf.Pi), .8f);
            if (previous.DistanceTo(point) > .1f * _unit && taper > .01f) {
                _light.DrawLine(previous, point, new Color(Mint, fadeOut * .38f), (6 + 25 * taper) * _unit, true);
                _light.DrawLine(previous, point, new Color(Hot, fadeOut * .92f), (1 + 11 * taper) * _unit, true);
            }
            previous = point;
        }
    }
    private Vector2 CutPoint(float t) => _target + new Vector2(_side * Mathf.Lerp(-170, 165, t),
        Mathf.Lerp(-105, 70, t) + Mathf.Sin(t * Mathf.Pi) * 48) * _unit;
}
