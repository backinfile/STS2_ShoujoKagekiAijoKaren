using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Models.Characters;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>A red stage tower holds card lights and follows native card transfers.</summary>
public partial class NKarenPromiseStarNode : Node2D
{
    private sealed class Star
    {
        public required CardModel Card;
        public int Slot;
        public float Light;
        public float Pulse;
        public bool Present;
        public NCard? Flight;
        public bool Incoming;
        public float FlightTime;
        public float OrbitOffset;
        public GuidedEntry? Entry;
        public readonly List<Vector2> Trail = new();
    }
    private sealed record GuidedEntry(Vector2 Source, float StartedAt, float Duration, bool LocalSource = false);
    private readonly Dictionary<CardModel, Star> _stars = new();
    private readonly List<CardModel> _expired = new();
    private readonly Vector2[] _starPolygon = new Vector2[8];
    private NCreature? _creature;
    private bool _alwaysVisible;
    private PromisePileMode _mode;
    private int _count;
    private float _time;
    private float _pulse;
    private float _visibility;
    private float _arrival;
    private float _departure;
    private float _replenishStarted = -10;
    private float _replenishUntil = -10;
    private static readonly Vector2 TowerOffset = new(-45, -15);
    private static readonly Vector2 TransferAnchor = new(0, -145);
    // Coordinates from the selected SVG 01, uniformly scaled to the existing height.
    private static Vector2 DesignPoint(float x, float y) => new(x * 1.5f, 170 + (y - 363) * 1.5f);
    private static readonly Vector2[] Junctions =
    {
        DesignPoint(-88, 363), DesignPoint(88, 363), DesignPoint(-21, 179), DesignPoint(21, 179),
        DesignPoint(-37, 245), DesignPoint(37, 245), DesignPoint(0, 42)
    };
    private static readonly Color TowerRed = new("#ff526a");
    private static readonly Color White = new("#fff5db");

    public void Init(NCreature creatureNode)
    {
        _creature = creatureNode;
        // Relative to the creature: all tower geometry and orbiting lights stay behind its body.
        ZIndex = 0;
        ShowBehindParent = true;
        _alwaysVisible = creatureNode.Entity.Player?.Character is Karen;
        if (_alwaysVisible) _visibility = 1;
        UpdatePlacement();
    }
    private void UpdatePlacement()
    {
        if (!GodotObject.IsInstanceValid(_creature)) return;
        // Shrink changes Visuals, not NCreature (our parent). Use the visual basis
        // lengths: the stage stays upright even when the body/visuals face left.
        var visual = _creature!.Visuals.GlobalTransform;
        var size = new Vector2(Mathf.Max(visual.X.Length(), 0.001f), Mathf.Max(visual.Y.Length(), 0.001f));
        GlobalTransform = new Transform2D(new Vector2(size.X, 0), new Vector2(0, size.Y), _creature.VfxSpawnPosition);
    }
    public Vector2 TransferGlobalPosition
    {
        get { UpdatePlacement(); return ToGlobal(TowerOffset + TransferAnchor); }
    }

    private Vector2 OrbitPoint(Star star, float time)
    {
        // A rising helix, independent of the stationary structural junctions.
        float t = Mathf.PosMod(time * 0.065f + star.Slot * 0.618034f + star.OrbitOffset, 1);
        float y = Mathf.Lerp(115, -265, t);
        float angle = t * Mathf.Tau * 2 + time * 1.15f;
        return TowerOffset + new Vector2(Mathf.Cos(angle) * (TowerWidth(y) * 0.85f + 12), y + Mathf.Sin(angle) * 9);
    }
    private Star GetStar(CardModel card)
    {
        if (_stars.TryGetValue(card, out var star)) return star;
        var occupied = _stars.Values.Where(s => s.Present || s.Flight != null || s.Entry != null).Select(s => s.Slot).ToHashSet();
        int slot = 0;
        while (occupied.Contains(slot)) slot++;
        star = new Star { Card = card, Slot = slot };
        _stars.Add(card, star);
        return star;
    }
    // The same card light is drawn throughout entry and orbit: there is no replacement particle.
    private void PlaceInOrbitGap(Star star, float arrivalTime)
    {
        // Reserve the largest free arc, including stars still entering. All stars
        // share the same angular speed, so this spacing survives after arrival.
        var phases = _stars.Values
            .Where(other => other != star && (other.Present || other.Entry != null))
            .Select(other => Mathf.PosMod(arrivalTime * 0.065f + other.Slot * 0.618034f + other.OrbitOffset, 1))
            .OrderBy(phase => phase).ToArray();
        float landing = 0.5f;
        float largest = 0;
        for (int i = 0; i < phases.Length; i++)
        {
            float next = i + 1 < phases.Length ? phases[i + 1] : phases[0] + 1;
            float gap = next - phases[i];
            if (gap <= largest) continue;
            largest = gap;
            landing = Mathf.PosMod(phases[i] + gap * 0.5f, 1);
        }
        star.OrbitOffset = landing - arrivalTime * 0.065f - star.Slot * 0.618034f;
    }

    public void GuideIntoOrbit(CardModel card, Vector2 source, bool fromCard = false)
    {
        UpdatePlacement();
        var star = GetStar(card);
        if (star.Present && !fromCard) return;
        float duration = fromCard ? 0.48f : 1.25f + (star.Slot % 5) * 0.09f;
        PlaceInOrbitGap(star, _time + duration);
        star.Entry = new GuidedEntry(source, _time, duration);
        star.Flight = null;
        star.Trail.Clear();
        star.Pulse = 0;
        star.Light = 0;
    }

    public void Replenish(IReadOnlyList<CardModel> cards)
    {
        if (cards.Count == 0) return;
        UpdatePlacement();
        if (_time > _replenishUntil + 0.12f) _replenishStarted = _time;
        float firstBirth = Mathf.Max(_time + 0.05f, _replenishStarted + 0.36f);
        for (int i = 0; i < cards.Count; i++)
        {
            var star = GetStar(cards[i]);
            float start = firstBirth + i * 0.055f;
            const float duration = 0.48f;
            PlaceInOrbitGap(star, start + duration);
            star.Entry = new GuidedEntry(TowerOffset + DesignPoint(0, 179), start, duration, true);
            star.Present = true;
            star.Flight = null;
            star.Trail.Clear();
            star.Light = star.Pulse = 0;
            _replenishUntil = Mathf.Max(_replenishUntil, start + 0.2f);
        }
    }

    private Vector2 StarPosition(Star star, float time)
    {
        var orbit = OrbitPoint(star, time);
        if (star.Entry is not { } entry) return orbit;
        float t = Mathf.Clamp((time - entry.StartedAt) / entry.Duration, 0f, 1f);
        // Quintic blend has zero first/second derivatives at both ends. At t=1,
        // position and velocity are exactly those of the moving orbit, without a stop.
        float blend = t * t * t * (t * (t * 6f - 15f) + 10f);
        var source = entry.LocalSource ? entry.Source : ToLocal(entry.Source);
        float arch = 16f * t * t * (1f - t) * (1f - t);
        var bend = entry.LocalSource
            ? new Vector2(star.Slot % 2 == 0 ? -65 : 65, -35)
            : Vector2.Up * 90;
        return source.Lerp(orbit, blend) + bend * arch;
    }

    public Vector2 GetStarPosition(CardModel card)
    {
        UpdatePlacement();
        return ToGlobal(StarPosition(GetStar(card), _time));
    }

    public Vector2 TransferPosition(NCard card)
    {
        UpdatePlacement();
        var star = GetStar(card.Model);
        if (star.Flight != card)
        {
            star.Entry = null;
            star.Incoming = !star.Present;
            if (star.Incoming) _arrival = 1;
            else _departure = 1;
            star.Flight = card;
            star.FlightTime = 0;
            star.Trail.Clear();
            star.Pulse = 1;
            _pulse = 1;
        }
        return TransferGlobalPosition;
    }
    public void Sync(IReadOnlyList<CardModel> cards, PromisePileMode mode)
    {
        _mode = mode;
        if (cards.Count > _count) _arrival = 1;
        if (cards.Count < _count) _departure = 1;
        _count = cards.Count;
        var present = cards.ToHashSet();
        foreach (var star in _stars.Values)
        {
            if (star.Present && !present.Contains(star.Card)) star.Entry = null;
            star.Present = present.Contains(star.Card);
        }
        foreach (var card in cards)
        {
            var star = GetStar(card);
            if (!star.Present) star.Pulse = 1;
            star.Present = true;
        }
    }
    public void ClearAll()
    {
        _stars.Clear();
        _count = 0;
        _mode = PromisePileMode.None;
        _pulse = 0;
        _arrival = _departure = 0;
        _replenishStarted = _replenishUntil = -10;
        _voidStarted = -100;
        _voidRoom = null;
        _voidPending = false;
        QueueRedraw();
    }
    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_creature)) { QueueFree(); return; }
        UpdatePlacement();
        float dt = (float)delta;
        _time += dt;
        _arrival = Mathf.MoveToward(_arrival, 0, dt * 1.2f);
        _departure = Mathf.MoveToward(_departure, 0, dt * 1.8f);
        _pulse = Mathf.MoveToward(_pulse, 0, dt * 2.5f);
        _visibility = Mathf.MoveToward(_visibility, _alwaysVisible || _count > 0 || _stars.Count > 0 || _mode != PromisePileMode.None ? 1 : 0, dt * 4);
        _expired.Clear();
        foreach (var star in _stars.Values)
        {
            star.Pulse = Mathf.MoveToward(star.Pulse, 0, dt * 2.5f);
            if (star.Flight != null)
            {
                star.FlightTime += dt;
                if (GodotObject.IsInstanceValid(star.Flight) && star.Flight.IsInsideTree())
                {
                    // Keep history in world space so resizing the stage mid-flight
                    // cannot stretch already travelled parts of the card trail.
                    var point = star.Flight.GlobalPosition;
                    star.Trail.Add(point);
                    if (star.Trail.Count > 12) star.Trail.RemoveAt(0);
                    bool arrived = star.Incoming && star.FlightTime > 0.08f && point.DistanceTo(TransferGlobalPosition) < 12;
                    if (arrived || star.FlightTime > (star.Incoming ? 2f : 0.45f))
                    {
                        star.Flight = null;
                        star.Trail.Clear();
                        star.Pulse = 1;
                    }
                }
                else { star.Flight = null; star.Trail.Clear(); star.Pulse = 1; }
            }
            // Keep the last entry trajectory until all trail samples have crossed into orbit.
            if (star.Entry is { } entry && _time - entry.StartedAt > entry.Duration + 0.2f)
                star.Entry = null;
            bool visible = (star.Present || star.Entry != null) && star.Flight == null
                && (star.Entry == null || _time >= star.Entry.StartedAt);
            star.Light = Mathf.MoveToward(star.Light, visible ? 1 : 0, dt * 6);
            if (!star.Present && star.Entry == null && star.Flight == null && star.Light <= 0) _expired.Add(star.Card);
        }
        foreach (var card in _expired) _stars.Remove(card);
        QueueRedraw();
    }
    public override void _Draw()
    {
        if (_visibility <= 0) return;
        bool infinite = (_mode & PromisePileMode.InfiniteReinforcement) != 0;
        bool isVoid = (_mode & PromisePileMode.Void) != 0;
        bool burn = (_mode & PromisePileMode.Burn) != 0;
        bool past = (_mode & PromisePileMode.PastAndFuture) != 0;
        Color accent = past ? new Color("#b4f5ff") : burn ? new Color("#ffbc81") : White;
        float alpha = _visibility;
        DrawVoidEcho(alpha);
        DrawTower(alpha);
        DrawReplenishSource(alpha);
        DrawVoidProjection(alpha);
        foreach (var star in _stars.Values)
        {
            if (star.Light > 0)
            {
                var p = StarPosition(star, _time);
                float cycle = Mathf.PosMod(_time * 0.065f + star.Slot * 0.618034f + star.OrbitOffset, 1);
                float fade = Mathf.Clamp(Mathf.Min(cycle, 1 - cycle) * 14, 0, 1);
                for (int i = 1; i <= 5; i++)
                    DrawLine(StarPosition(star, _time - i * 0.035f), StarPosition(star, _time - (i - 1) * 0.035f),
                        new Color(accent, star.Light * alpha * fade * (6 - i) * 0.06f), 1.5f, true);
                float mappingFlash = (isVoid || _voidPending)
                    ? Ease((VoidAge - 0.94f) / 0.18f) * (1 - Ease((VoidAge - 1.17f) / 0.26f)) : 0;
                DrawStar(p, 8 + star.Pulse * 4 + mappingFlash * 3, accent, star.Light * alpha * fade);
                if (star.Pulse > 0) DrawArc(p, 10 + (1 - star.Pulse) * 20, 0, Mathf.Tau, 32, new Color(accent, star.Pulse * 0.5f * alpha), 1, true);
            }
            if (star.Trail.Count > 1)
            {
                for (int i = 1; i < star.Trail.Count; i++)
                    DrawLine(ToLocal(star.Trail[i - 1]), ToLocal(star.Trail[i]), new Color(accent, (float)i / star.Trail.Count * 0.75f), 4, true);
                float opacity = star.Incoming ? Mathf.Clamp(star.FlightTime * 3, 0, 1) : Mathf.Clamp(1 - star.FlightTime * 2, 0, 1);
                DrawStar(ToLocal(star.Trail[^1]), 15, accent, opacity);
            }
        }
        string text = (infinite ? "∞" : _count.ToString()) + (isVoid ? " ◇" : "");
        if (_alwaysVisible || _count > 0 || infinite || isVoid)
        {
            var font = ThemeDB.FallbackFont;
            var size = font.GetStringSize(text, HorizontalAlignment.Left, -1, 26);
            // Keep the complete count/mode label centered above the tip in
            // viewport space: creature shrink/flip cannot change its font size.
            var canvas = GetGlobalTransformWithCanvas();
            var tip = canvas * (TowerOffset + DesignPoint(0, 18));
            var viewport = GetViewportRect().Size;
            var center = new Vector2(
                Mathf.Clamp(tip.X, size.X * 0.5f + 12, viewport.X - size.X * 0.5f - 12),
                Mathf.Clamp(tip.Y - 22, 90, Mathf.Max(90, viewport.Y - 30)));
            DrawSetTransformMatrix(canvas.AffineInverse());
            var p = center - new Vector2(size.X * 0.5f, 0);
            DrawStringOutline(font, p, text, HorizontalAlignment.Left, -1, 26, 5, new Color(0.10f, 0.07f, 0.12f, alpha));
            DrawString(font, p, text, HorizontalAlignment.Left, -1, 26, new Color(White, alpha));
            DrawSetTransform(Vector2.Zero);
        }
    }
    private void DrawReplenishSource(float alpha)
    {
        float age = _time - _replenishStarted;
        if (age < 0 || _time > _replenishUntil + 0.22f) return;
        float fade = Mathf.Clamp((_replenishUntil + 0.22f - _time) / 0.22f, 0, 1) * alpha;
        float progress = Mathf.Clamp(age / 0.36f, 0, 1);
        if (age < 0.55f)
        {
            float trailFade = fade * Mathf.Clamp((0.55f - age) / 0.19f, 0, 1);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 13; i >= 0; i--)
                {
                    float t = progress - i * 0.027f;
                    if (t < 0) continue;
                    var point = ReplenishPath(side, t);
                    float brightness = trailFade * (1 - i / 15f);
                    var previous = ReplenishPath(side, Mathf.Max(0, t - 0.027f));
                    DrawLine(previous, point, new Color(White, brightness * 0.18f), 16, true);
                    DrawLine(previous, point, new Color(White, brightness * 0.4f), 9, true);
                    DrawLine(previous, point, new Color(White, brightness), 4.5f, true);
                    DrawCircle(point, i == 0 ? 6 : 3, new Color(White, brightness));
                    if (i == 0) DrawStar(point, 14, White, brightness);
                }
            }
        }
        float sourceLight = Mathf.SmoothStep(0.27f, 0.36f, age) * fade;
        var source = TowerOffset + DesignPoint(0, 179);
        DrawLine(source + new Vector2(-42, 0), source + new Vector2(42, 0),
            new Color(White, sourceLight * 0.2f), 12, true);
        DrawLine(source + new Vector2(-42, 0), source + new Vector2(42, 0),
            new Color(White, sourceLight), 3, true);
        DrawStar(source, 17 + Mathf.Sin(age * 14) * 2, White, sourceLight);
    }

    private static Vector2 ReplenishPath(int side, float progress)
    {
        // Reverse the existing curved tower leg, then travel across its deck.
        if (progress > 0.82f)
            return TowerOffset + DesignPoint(side * Mathf.Lerp(21, 0, (progress - 0.82f) / 0.18f), 179);
        float t = progress / 0.82f;
        var point = (1 - t) * (1 - t) * new Vector2(side * 88, 363)
            + 2 * (1 - t) * t * new Vector2(side * 36, 268)
            + t * t * new Vector2(side * 21, 179);
        return TowerOffset + DesignPoint(point.X, point.Y);
    }

    private void TowerLine(Vector2 from, Vector2 to, Color color, float alpha, float width = 2)
    {
        from += TowerOffset;
        to += TowerOffset;
        DrawLine(from, to, new Color(color, alpha * 0.025f), width + 10, true);
        DrawLine(from, to, new Color(color, alpha * 0.09f), width + 4, true);
        DrawLine(from, to, new Color(color, alpha), width, true);
    }

    private static float TowerWidth(float y)
    {
        float designY = (y - 170) / 1.5f + 363;
        if (designY >= 179)
        {
            float dy = Mathf.Clamp(designY - 179, 0, 184);
            float t = 2 * dy / (178 + Mathf.Sqrt(178 * 178 + 24 * dy));
            return (21 + 30 * t + 37 * t * t) * 1.5f;
        }
        if (designY >= 120) return Mathf.Lerp(11, 21, (designY - 120) / 59) * 1.5f;
        return Mathf.Lerp(0, 11, Mathf.Clamp((designY - 42) / 78, 0, 1)) * 1.5f;
    }

    private void DesignLine(Vector2 from, Vector2 to, float alpha, float width = 1.45f)
    {
        TowerLine(DesignPoint(from.X, from.Y), DesignPoint(to.X, to.Y), TowerRed, alpha, width);
        DrawVoidScanLine(from, to, _visibility);
    }

    private void DesignCurve(Vector2 from, Vector2 control, Vector2 to, float alpha)
    {
        var previous = from;
        for (int i = 1; i <= 24; i++)
        {
            float t = i / 24f;
            var point = (1 - t) * (1 - t) * from + 2 * (1 - t) * t * control + t * t * to;
            DesignLine(previous, point, alpha);
            previous = point;
        }
    }

    private void DrawTower(float alpha)
    {
        // SVG 01: curved, flared feet and a rounded inner arch, with two hollow decks.
        for (int side = -1; side <= 1; side += 2)
        {
            DesignLine(new(0, 42), new(side * 11, 120), alpha * 0.75f);
            DesignLine(new(side * 11, 120), new(side * 21, 179), alpha * 0.75f);
            DesignCurve(new(side * 21, 179), new(side * 36, 268), new(side * 88, 363), alpha * 0.75f);
            DesignLine(new(side * 88, 363), new(side * 58, 354), alpha * 0.65f);
            DesignCurve(new(side * 58, 354), new(side * 30, 296), new(0, 295), alpha * 0.65f);
            DesignLine(new(side * 64, 318), new(-side * 46, 282), alpha * 0.4f, 0.9f);
            DesignLine(new(side * 44, 272), new(-side * 31, 222), alpha * 0.4f, 0.9f);
            DesignLine(new(side * 29, 211), new(-side * 21, 179), alpha * 0.4f, 0.9f);
            DesignLine(new(side * 19, 165), new(-side * 14, 141), alpha * 0.4f, 0.9f);
            DesignLine(new(side * 10, 117), new(-side * 6, 83), alpha * 0.4f, 0.9f);
            float t = Mathf.PosMod(_time * 0.22f, 1);
            float y = Mathf.Lerp(170, -302, t);
            float nextY = Mathf.Max(y - 22, -310);
            TowerLine(new(side * TowerWidth(y), y), new(side * TowerWidth(nextY), nextY),
                White, alpha * (0.15f + _arrival * 0.5f), 1.2f);
        }
        DrawStageDeck(179, 30, 5, alpha);
        DrawStageDeck(245, 47, 7, alpha);
        DesignLine(new(0, 18), new(0, 42), alpha * 0.8f, 1.2f);
        DrawStar(TowerOffset + DesignPoint(0, 18), 6 + _pulse * 3, White,
            alpha * (0.8f + Mathf.Sin(_time * 2.4f) * 0.15f));
        foreach (var point in Junctions)
        {
            var p = TowerOffset + point;
            float light = alpha * (0.85f + Mathf.Sin(_time * 2 + point.Y * 0.02f) * 0.12f);
            DrawCircle(p, 13, new Color(TowerRed, light * 0.12f));
            DrawCircle(p, 7, new Color(TowerRed, light * 0.35f));
            DrawCircle(p, 3, new Color(White, light));
        }
        if (_arrival > 0 || _departure > 0)
            DrawStar(TowerOffset + TransferAnchor, 8 + 8 * Mathf.Max(_arrival, _departure), White,
                alpha * Mathf.Max(_arrival, _departure));
    }

    private void DrawStageDeck(float y, float width, float height, float alpha)
    {
        DesignLine(new(-width, y), new(-width + 5, y - height), alpha * 0.7f, 1.2f);
        DesignLine(new(-width + 5, y - height), new(width - 5, y - height), alpha * 0.7f, 1.2f);
        DesignLine(new(width - 5, y - height), new(width, y), alpha * 0.7f, 1.2f);
        DesignLine(new(width, y), new(width - 5, y + height), alpha * 0.7f, 1.2f);
        DesignLine(new(width - 5, y + height), new(-width + 5, y + height), alpha * 0.7f, 1.2f);
        DesignLine(new(-width + 5, y + height), new(-width, y), alpha * 0.7f, 1.2f);
        TowerLine(DesignPoint(-width + 4, y - height), DesignPoint(width - 4, y - height), White,
            alpha * (0.35f + Mathf.Max(_arrival, _departure) * 0.65f), 1);
    }

    private void DrawStar(Vector2 p, float radius, Color color, float alpha)
    {
        DrawCircle(p, radius * 2.4f, new Color(color, alpha * 0.075f));
        DrawCircle(p, radius * 1.5f, new Color(color, alpha * 0.16f));
        var points = _starPolygon;
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.Pi / 4 - Mathf.Pi / 2;
            points[i] = p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (i % 2 == 0 ? radius : radius * 0.26f);
        }
        DrawColoredPolygon(points, new Color(color, alpha));
        DrawCircle(p, 1.8f, new Color(White, alpha));
    }
}
