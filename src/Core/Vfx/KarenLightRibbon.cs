using System;
using Godot;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>A short, tapered strip on an existing draw layer. Head may pass 1 to retract its tail.</summary>
internal static class KarenLightRibbon
{
    public static Vector2 Curve(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
    {
        float v = 1 - t;
        return a * (v * v * v) + b * (3 * v * v * t) + c * (3 * v * t * t) + d * (t * t * t);
    }

    public static void Draw(Node2D layer, Func<float, Vector2> path, float head, float length, float width, Color color, float alpha)
    {
        float first = Mathf.Clamp(head - length, 0, 1), last = Mathf.Clamp(head, 0, 1);
        if (alpha <= 0 || last - first < .001f) return;
        const int segments = 20;
        for (int i = 0; i < segments; i++)
        {
            float u = i / (float)segments, v = (i + 1f) / segments;
            var a = path(Mathf.Lerp(first, last, u)); var b = path(Mathf.Lerp(first, last, v));
            var normal = (b - a).Orthogonal().Normalized();
            float wa = width * Mathf.Sin(u * Mathf.Pi), wb = width * Mathf.Sin(v * Mathf.Pi);
            var ca = new Color(color, alpha * (.20f + .65f * u));
            var cb = new Color(color, alpha * (.20f + .65f * v));
            layer.DrawPolygon([a - normal * wa, a + normal * wa, b + normal * wb, b - normal * wb], [ca, ca, cb, cb]);
            layer.DrawLine(a, b, new Color(color.Lerp(Colors.White, .65f), alpha * v * .60f), Mathf.Max(.7f, width * .20f), true);
        }
    }
}
