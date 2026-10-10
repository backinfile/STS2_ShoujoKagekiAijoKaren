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
        PaintedLeftLeg[0], PaintedRightLeg[0], new(420, 723), new(618, 730),
        new(470, 403), new(566, 404)
    };
    // Trace the cream fascia below the windows, including each perspective corner.
    private static readonly Vector2[] PaintedUpperDeckEdge =
    {
        new(463, 404), new(534, 390), new(572, 407)
    };
    private static readonly Vector2[] PaintedLowerDeckEdge =
    {
        new(410, 729), new(420, 723), new(553, 707), new(622, 732)
    };
    private static readonly Vector2[] PaintedLegLamps =
    {
        new(338, 1140), new(696, 1140), new(263, 1310), new(763, 1310)
    };
    private static readonly Color LampAmber = new("#ffbe70");
    private static readonly Vector2[] PaintedFireSites =
    {
        new(225, 1360), new(338, 1140), new(390, 990), new(430, 830),
        new(800, 1360), new(696, 1140), new(640, 990), new(602, 830),
        new(445, 718), new(516, 718), new(588, 718), new(486, 394), new(550, 394)
    };
    internal static int BurnSiteCount => PaintedFireSites.Length;
    internal static Vector2 BurnSite(int index) => PaintedPoint(PaintedFireSites[index % PaintedFireSites.Length]);

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

    private static Vector2 TowerRunnerPoint(int side, float progress)
    {
        var leg = side < 0 ? PaintedLeftLeg : PaintedRightLeg;
        // The idle light joins the fascia directly above its leg. Reproduction
        // continues to use its separate route into the central card opening.
        var deckLeft = PaintedLowerDeckEdge[side < 0 ? 1 : 2];
        var deckRight = PaintedLowerDeckEdge[side < 0 ? 2 : 3];
        var contact = deckLeft.Lerp(deckRight, (leg[^1].X - deckLeft.X) / (deckRight.X - deckLeft.X));
        float length = leg[^1].DistanceTo(contact);
        for (int i = 1; i < leg.Length; i++) length += leg[i - 1].DistanceTo(leg[i]);
        float remaining = Mathf.Clamp(progress, 0, 1) * length;
        for (int i = 1; i <= leg.Length; i++)
        {
            var next = i < leg.Length ? leg[i] : contact;
            float segment = leg[i - 1].DistanceTo(next);
            if (remaining <= segment)
                return PaintedPoint(leg[i - 1].Lerp(next, remaining / segment));
            remaining -= segment;
        }
        return PaintedPoint(contact);
    }

    private void DrawTower(float alpha)
    {
        // Recess the painted prop behind Karen without baking lights or count into it.
        DrawPaintedTowerStrip(0, 1536, Vector2.Zero, new Color(0.90f, 0.90f, 0.90f, alpha * 0.92f));
        DrawVoidScan(alpha);
        DrawDeckLights(PaintedUpperDeckEdge, alpha, .7f);
        DrawDeckLights(PaintedLowerDeckEdge, alpha, 0);
        for (int side = -1; side <= 1; side += 2)
        for (int runner = 0; runner < 2; runner++)
        {
            float t = Mathf.PosMod(_time * .19f + (side > 0 ? .28f : 0) + runner * .5f, 1);
            const float arrival = .88f;
            var to = TowerRunnerPoint(side, t / arrival);
            float glow = Mathf.SmoothStep(0, .06f, t)
                * (1 - Mathf.SmoothStep(arrival, 1, t)) * alpha;
            // Keep full light while climbing. Only fade after reaching the deck,
            // and let the tail catch up there before this runner restarts.
            var from = TowerRunnerPoint(side, (t - .10f) / arrival);
            for (int i = 1; i <= 5; i++)
            {
                float tail = Mathf.Lerp(t - .10f, t, i / 5f);
                var next = TowerRunnerPoint(side, tail / arrival);
                if (from.DistanceSquaredTo(next) > .01f)
                {
                    DrawLine(from, next, new Color(LampAmber, glow * .14f), 22, true);
                    DrawLine(from, next, new Color(LampAmber, glow * .55f), 8, true);
                    DrawLine(from, next, new Color(White, glow * (.85f + _arrival * .15f)), 3.5f, true);
                }
                from = next;
            }
            DrawTowerLamp(to, 5, glow);
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
        for (int i = 0; i < PaintedLegLamps.Length; i++)
        {
            var point = PaintedPoint(PaintedLegLamps[i]);
            float breath = .5f + .5f * Mathf.Sin(_time * 2.1f - i * .65f);
            float light = alpha * (.22f + breath * .78f);
            DrawTowerLamp(point, 3.5f + breath * 1.3f, light);
        }
        if (!UsingVoidIconOrbit && (_arrival > 0 || _departure > 0))
            DrawStar(PaintedSource, 8 + 8 * Mathf.Max(_arrival, _departure), White,
                alpha * Mathf.Max(_arrival, _departure));
    }

    private void DrawDeckLights(Vector2[] edge, float alpha, float phase)
    {
        float totalLength = 0;
        for (int side = 1; side < edge.Length; side++)
            totalLength += edge[side - 1].DistanceTo(edge[side]);
        float travelled = 0;
        for (int side = 1; side < edge.Length; side++)
        {
            var left = edge[side - 1];
            var right = edge[side];
            float length = left.DistanceTo(right);
            int segments = Mathf.Max(2, Mathf.CeilToInt(length / 16));
            // Split at the artwork's corners; the wave continues by distance
            // around the edge, while each stroke stays on its own visible face.
            for (int i = 0; i < segments; i++)
            {
                float local = (float)i / segments;
                float t = (travelled + length * local) / totalLength;
                float wave = .5f + .5f * Mathf.Sin(_time * 2.1f - t * Mathf.Tau + phase);
                float light = alpha * (.35f + wave * .65f);
                var from = PaintedPoint(left.Lerp(right, local));
                var to = PaintedPoint(left.Lerp(right, (i + 1f) / segments));
                DrawLine(from, to, new Color(LampAmber, light * .13f), 20, true);
                DrawLine(from, to, new Color(LampAmber, light * .7f), 7, true);
                DrawLine(from, to, new Color(White, light), 3, true);
                if (i % 3 == 1) DrawTowerLamp(from, 2.5f, light);
            }
            travelled += length;
        }
    }

    private void DrawTowerLamp(Vector2 point, float radius, float light)
    {
        for (int layer = 5; layer > 0; layer--)
            DrawCircle(point, radius * layer, new Color(LampAmber, light * .055f * (6 - layer)));
        DrawCircle(point, radius, new Color(LampAmber, light * .8f));
        DrawCircle(point, radius * .5f, new Color(White, light));
    }
}
