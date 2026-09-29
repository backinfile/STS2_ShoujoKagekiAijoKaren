using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenPromiseStarNode
{
    private float _voidStarted = -100;
    private NCombatRoom? _voidRoom;
    private bool _voidPending;
    private static readonly Color EchoGold = new("#ffe6b6");

    public void StartVoidActivation()
    {
        _voidStarted = _time;
        _voidRoom = NCombatRoom.Instance;
        _voidPending = true;
    }

    public void FinishVoidActivation() => _voidPending = false;

    private float VoidAge => _time - _voidStarted;
    private float VoidFront => Mathf.Lerp(18, 375, Mathf.Clamp((VoidAge - 0.18f) / 0.55f, 0, 1));
    private static float Ease(float value) => Mathf.SmoothStep(0, 1, value);

    private void DrawVoidScanLine(Vector2 from, Vector2 to, float alpha)
    {
        float age = VoidAge;
        if (age < 0.14f || age > 0.98f) return;
        float fade = Ease((age - 0.14f) / 0.075f) * (1 - Ease((age - 0.75f) / 0.23f)) * alpha;
        int steps = Mathf.Max(1, Mathf.CeilToInt(from.DistanceTo(to) / 8));
        for (int i = 0; i < steps; i++)
        {
            var a = from.Lerp(to, (float)i / steps);
            var b = from.Lerp(to, (float)(i + 1) / steps);
            float behind = VoidFront - (a.Y + b.Y) * 0.5f;
            float light = Ease((behind + 12) / 24) * (1 - Ease((behind - 12) / 85)) * fade;
            if (light > 0.001f) TowerLine(DesignPoint(a.X, a.Y), DesignPoint(b.X, b.Y), White, light, 3.2f);
        }
    }

    private void DrawVoidEcho(float alpha)
    {
        if ((_mode & PromisePileMode.Void) == 0 && !_voidPending) return;
        float age = VoidAge;
        float opacity = alpha * (0.43f + 0.09f * Mathf.Sin((age - 1.4f) * Mathf.Tau / 3));
        float shift = 8 * Ease((age - 0.18f) / 0.4f);
        void Line(Vector2 a, Vector2 b)
        {
            float front = VoidFront;
            if (Mathf.Min(a.Y, b.Y) > front) return;
            // Clip exactly at the descending front, including diagonal legs.
            if (a.Y > front) a = b.Lerp(a, (front - b.Y) / (a.Y - b.Y));
            if (b.Y > front) b = a.Lerp(b, (front - a.Y) / (b.Y - a.Y));
            TowerLine(DesignPoint(a.X + shift, a.Y), DesignPoint(b.X + shift, b.Y), EchoGold, opacity, 1.8f);
        }
        Line(new(0, 18), new(0, 42));
        for (int side = -1; side <= 1; side += 2)
        {
            Line(new(0, 42), new(side * 11, 120));
            Line(new(side * 11, 120), new(side * 21, 179));
            var previous = new Vector2(side * 21, 179);
            for (int i = 1; i <= 24; i++)
            {
                float t = i / 24f;
                var p = (1 - t) * (1 - t) * new Vector2(side * 21, 179)
                    + 2 * (1 - t) * t * new Vector2(side * 36, 268) + t * t * new Vector2(side * 88, 363);
                Line(previous, p);
                previous = p;
            }
        }
        foreach (var deck in new[] { new Vector3(179, 30, 5), new Vector3(245, 47, 7) })
        {
            float y = deck.X, w = deck.Y, h = deck.Z;
            Vector2[] points = { new(-w, y), new(-w + 5, y - h), new(w - 5, y - h), new(w, y), new(w - 5, y + h), new(-w + 5, y + h) };
            for (int i = 0; i < points.Length; i++) Line(points[i], points[(i + 1) % points.Length]);
        }
    }

    private void DrawVoidProjection(float alpha)
    {
        float age = VoidAge;
        if (age < 0.7f || age > 1.75f || _voidRoom != NCombatRoom.Instance
            || !GodotObject.IsInstanceValid(_voidRoom) || _creature?.Entity.Player is not { } player
            || !LocalContext.IsMe(player)) return;
        var pile = _voidRoom!.Ui.DrawPile;
        if (!GodotObject.IsInstanceValid(pile) || !pile.IsVisibleInTree()) return;

        // Convert both ends to viewport coordinates. The HUD is not in the
        // creature's canvas/scale; never project a remote player onto local HUD.
        var canvas = GetGlobalTransformWithCanvas();
        var destination = pile.GetGlobalTransformWithCanvas() * (pile.Size * 0.5f);
        DrawSetTransformMatrix(canvas.AffineInverse());
        float fade = Ease((age - 0.7f) / 0.055f) * (1 - Ease((age - 1.03f) / 0.2f)) * alpha;
        float progress = Mathf.Clamp((age - 0.72f) / 0.29f, 0, 1);
        for (int side = -1; side <= 1; side += 2)
        {
            var source = canvas * (TowerOffset + DesignPoint(side * 88, 363));
            var control = new Vector2(Mathf.Lerp(source.X, destination.X, 0.6f), Mathf.Max(source.Y, destination.Y) + 22);
            Vector2 Point(float t) => (1 - t) * (1 - t) * source + 2 * (1 - t) * t * control + t * t * destination;
            for (int i = 18; i >= 0; i--)
            {
                float t = progress - i * 0.025f;
                if (t < 0) continue;
                var a = Point(Mathf.Max(0, t - 0.025f));
                var b = Point(t);
                float light = fade * (1 - i / 20f);
                DrawLine(a, b, new Color(EchoGold, light * 0.16f), 15, true);
                DrawLine(a, b, new Color(EchoGold, light * 0.4f), 7, true);
                DrawLine(a, b, new Color(White, light), 3, true);
                if (i == 0) DrawStar(b, 13, White, fade);
            }
        }
        // Small four-point lights assemble across the icon's face. Stagger their
        // arrival and release so the reforming pile never becomes a solid flash.
        var pileCanvas = pile.GetGlobalTransformWithCanvas();
        for (int i = 0; i < 24; i++)
        {
            float seed = ((i * 13) % 23) / 22f;
            float birth = 0.89f + (i % 6) * 0.028f;
            float flight = Ease((age - birth) / 0.28f);
            float light = Ease((age - birth) / 0.075f)
                * (1 - Ease((age - birth - 0.34f) / 0.3f)) * alpha;
            if (light <= 0) continue;
            var local = new Vector2((i % 4 - 1.5f) * 0.14f + (seed - 0.5f) * 0.04f,
                (i / 4 - 2.5f) * 0.115f - 0.045f) * pile.Size;
            var target = pileCanvas * (pile.Size * 0.5f + local);
            float angle = i * 2.399963f;
            var source = destination + new Vector2(12 + Mathf.Cos(angle) * (30 + seed * 20),
                -32 - seed * 42);
            source.X = Mathf.Max(7, source.X);
            var position = source.Lerp(target, flight);
            position.X += Mathf.Sin(flight * Mathf.Pi) * (seed - 0.5f) * 14;
            DrawStar(position, 2.8f + seed * 2.4f, i % 3 == 0 ? EchoGold : White, light);
        }
        DrawSetTransform(Vector2.Zero);
    }
}
