using Godot;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenPromiseStarNode
{
    private static Texture2D? _towerTexture;
    private const float PaintedScale = 0.3475f;
    private static readonly Vector2 PaintedTipPixel = new(516, 19);
    private static readonly Vector2 PaintedSourcePixel = new(516, 718);

    // Pixel coordinates of the selected 03 artwork's outer legs and main deck.
    // Keep the light routes in the same coordinate space as the sprite.
    private static readonly Vector2[] PaintedLeftLeg =
    {
        new(162, 1465), new(263, 1310), new(338, 1140), new(387, 995),
        new(421, 850), new(445, 734)
    };
    private static readonly Vector2[] PaintedRightLeg =
    {
        new(861, 1453), new(763, 1310), new(696, 1140), new(645, 995),
        new(611, 850), new(588, 736)
    };
    private static readonly Vector2[] PaintedJunctions =
    {
        PaintedLeftLeg[0], PaintedRightLeg[0], new(418, 705), new(614, 705),
        new(472, 386), new(564, 386)
    };

    public override void _Ready()
    {
        _towerTexture ??= KarenResourceLoader.LoadTexture(
            "res://images/vfx/promise/painted_tower.png", nameof(NKarenPromiseStarNode));
        TextureFilter = TextureFilterEnum.LinearWithMipmaps;
    }

    private static Vector2 PaintedPoint(Vector2 pixel)
        => TowerOffset + new Vector2(0, DesignPoint(0, 18).Y)
            + (pixel - PaintedTipPixel) * PaintedScale;

    private static Vector2 PaintedSource => PaintedPoint(PaintedSourcePixel);

    private void DrawPaintedTowerStrip(float from, float to, Vector2 offset, Color color)
    {
        if (_towerTexture == null) return;
        var size = _towerTexture.GetSize();
        from = Mathf.Clamp(from, 0, size.Y);
        to = Mathf.Clamp(to, from, size.Y);
        if (to <= from) return;
        var source = new Rect2(0, from, size.X, to - from);
        var target = new Rect2(PaintedPoint(source.Position) + offset, source.Size * PaintedScale);
        DrawTextureRectRegion(_towerTexture, target, source, color);
    }

    private static Vector2 ReplenishPath(int side, float progress)
    {
        var leg = side < 0 ? PaintedLeftLeg : PaintedRightLeg;
        if (progress > 0.82f)
            return PaintedPoint(leg[^1].Lerp(PaintedSourcePixel,
                Mathf.Clamp((progress - 0.82f) / 0.18f, 0, 1)));
        float length = 0;
        for (int i = 1; i < leg.Length; i++) length += leg[i - 1].DistanceTo(leg[i]);
        float remaining = Mathf.Clamp(progress / 0.82f, 0, 1) * length;
        for (int i = 1; i < leg.Length; i++)
        {
            float segment = leg[i - 1].DistanceTo(leg[i]);
            if (remaining <= segment)
                return PaintedPoint(leg[i - 1].Lerp(leg[i], remaining / segment));
            remaining -= segment;
        }
        return PaintedPoint(leg[^1]);
    }

    private void DrawTower(float alpha)
    {
        // Recess the painted prop behind Karen without baking lights or count into it.
        DrawPaintedTowerStrip(0, 1536, Vector2.Zero, new Color(0.90f, 0.90f, 0.90f, alpha * 0.92f));
        DrawVoidScan(alpha);
        for (int side = -1; side <= 1; side += 2)
        {
            float t = Mathf.PosMod(_time * 0.18f, 1);
            var from = ReplenishPath(side, t * 0.82f);
            var to = ReplenishPath(side, Mathf.Min(t + 0.04f, 1) * 0.82f);
            DrawLine(from, to, new Color(White, alpha * (0.14f + _arrival * 0.45f)), 1.2f, true);
        }
        DrawStar(PaintedPoint(PaintedTipPixel), 5 + _pulse * 3, White,
            alpha * (0.65f + Mathf.Sin(_time * 2.4f) * 0.12f));
        foreach (var pixel in PaintedJunctions)
        {
            var point = PaintedPoint(pixel);
            float light = alpha * (0.28f + _pulse * 0.45f);
            DrawCircle(point, 7, new Color(TowerRed, light * 0.18f));
            DrawCircle(point, 2, new Color(White, light));
        }
        if (!UsingVoidIconOrbit && (_arrival > 0 || _departure > 0))
            DrawStar(PaintedSource, 8 + 8 * Mathf.Max(_arrival, _departure), White,
                alpha * Mathf.Max(_arrival, _departure));
    }
}
