using Godot;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System.Collections.Generic;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

// Actual deliveries keep card identity; the activation surge is drawing only.
public partial class NKarenPromiseStarNode
{
    private sealed class ReproductionFlight
    {
        public required CardModel Card;
        public float StartedAt;
        public float Duration;
        public float Seed;
        public float Height;
        public bool Launched;
        public float ArrivesAt => StartedAt + Duration;
    }

    private static readonly Texture2D? ContinuePortrait = KarenResourceLoader.LoadTexture(
        "res://images/packed/card_portraits/token/karen_continue.png", nameof(NKarenPromiseStarNode));
    private static readonly Color ReproductionGold = new("#ffe9b2");
    private static readonly Color ReproductionRed = new("#c73750");
    private static readonly Color ReproductionPaper = new("#e7d2ba");
    private readonly List<ReproductionFlight> _reproductionFlights = new();
    private readonly List<AudioStreamPlayer> _reproductionFlightSounds = new();
    private float _reproductionAge = 100;
    private float _reproductionVisibility;
    private float _reproductionNextBirth;
    private int _reproductionSequence;
    private bool _reproductionActive;
    private AudioStreamPlayer? _reproductionSound;
    private const int ReproductionSurgeSlots = 26;
    private float ReproductionSurgeVisibility => Mathf.SmoothStep(0, 0.1f / 3, _reproductionAge)
        * (1 - Mathf.SmoothStep(2.15f / 3, 2.65f / 3, _reproductionAge));
    private Vector2 ReproductionGate => UsingVoidIconOrbit
        ? FromVoidIcon(_voidOrbitIcon!.Size * 0.5f) : PaintedSource;

    public void PlayReproductionActivation()
    {
        if (!HasReproductionMode()) return;
        EndCombatEntrance();
        _reproductionActive = true;
        _reproductionAge = 0;
        // Keep the real deliveries from EnterMode. Only this explicit card play
        // starts the dense activation surge; ordinary refills never start it.
        StopReproductionSound(ref _reproductionSound);
        _reproductionSound = KarenAudioManager.PlaySfx("karen_reproduction.wav", 0.45f);
    }

    private bool HasReproductionMode() => GodotObject.IsInstanceValid(_creature)
        && _creature!.Entity.GetPower<KarenPromisePilePower>()?.IsInfiniteReinforcement == true;

    private ReproductionFlight? QueueReproduction(CardModel card)
    {
        if (!HasReproductionMode()) return null;
        // The source reports actual generated cards, including the first fill.
        // Consecutive callbacks append deliveries instead of resetting a pulse.
        var existing = _reproductionFlights.Find(flight => flight.Card == card);
        if (existing != null) return existing;
        _reproductionActive = true;
        int sequence = ++_reproductionSequence;
        float seed = Mathf.PosMod(sequence * 0.618033989f, 1);
        float height = Mathf.PosMod(sequence * 0.754877666f, 1);
        var flight = new ReproductionFlight
        {
            Card = card,
            StartedAt = Mathf.Max(_time + 0.025f, _reproductionNextBirth),
            Duration = 0.28f + height * 0.12f,
            Seed = seed,
            Height = height
        };
        _reproductionNextBirth = flight.StartedAt + 0.038f + sequence % 3 * 0.008f;
        _reproductionFlights.Add(flight);
        return flight;
    }

    private void UpdateReproduction(float delta)
    {
        bool active = HasReproductionMode();
        if (!active && _reproductionActive) StopReproductionSounds();
        _reproductionActive = active;
        _reproductionVisibility = Mathf.MoveToward(_reproductionVisibility, active ? 1 : 0, delta * 5);
        _reproductionAge += delta;
        if (!active && _reproductionVisibility <= 0)
        {
            _reproductionFlights.Clear();
            _reproductionNextBirth = _time;
        }
        foreach (var flight in _reproductionFlights)
        {
            if (!active || flight.Launched || _time < flight.StartedAt) continue;
            flight.Launched = true;
            var sound = KarenAudioManager.PlaySfx("karen_reproduction_feed.wav", 0.3f);
            if (sound != null) _reproductionFlightSounds.Add(sound);
        }
        _reproductionFlights.RemoveAll(flight => _time > flight.ArrivesAt + 0.28f);
        _reproductionFlightSounds.RemoveAll(sound => !GodotObject.IsInstanceValid(sound));
    }

    private void ClearReproduction()
    {
        _reproductionAge = 100;
        _reproductionVisibility = 0;
        _reproductionNextBirth = _time;
        _reproductionSequence = 0;
        _reproductionActive = false;
        _reproductionFlights.Clear();
        StopReproductionSounds();
    }

    private void StopReproductionSounds()
    {
        StopReproductionSound(ref _reproductionSound);
        foreach (var sound in _reproductionFlightSounds)
            if (GodotObject.IsInstanceValid(sound)) { sound.Stop(); sound.QueueFree(); }
        _reproductionFlightSounds.Clear();
    }

    private static void StopReproductionSound(ref AudioStreamPlayer? sound)
    {
        if (GodotObject.IsInstanceValid(sound)) { sound!.Stop(); sound.QueueFree(); }
        sound = null;
    }

    private void DrawReproduction(float alpha, CanvasItem? surface = null)
    {
        alpha *= _reproductionVisibility;
        if (alpha <= 0) return;
        surface ??= this;
        // In Void mode the existing HUD front layer owns the drawing. Mapping
        // from the tower's coordinates keeps it above UI and attached to Icon.
        var frame = surface.GetGlobalTransformWithCanvas().AffineInverse() * GetGlobalTransformWithCanvas();
        surface.DrawSetTransformMatrix(frame);
        DrawReproductionContent(surface, frame, alpha);
        surface.DrawSetTransformMatrix(Transform2D.Identity);
    }

    private void DrawReproductionContent(CanvasItem surface, Transform2D frame, float alpha)
    {
        var gate = ReproductionGate;
        float surge = ReproductionSurgeVisibility;
        // Prefilled phases keep the supply dense even while the entire surge
        // fades. Each cycle changes its scattered source, rather than a lane.
        if (surge > 0)
        {
            for (int i = 0; i < ReproductionSurgeSlots; i++)
            {
                float phase = _reproductionAge / 0.36f + i / (float)ReproductionSurgeSlots;
                int cycle = Mathf.FloorToInt(phase);
                float seed = Mathf.PosMod(i * 0.618033989f + cycle * 0.414213562f, 1);
                float height = Mathf.PosMod(i * 0.754877666f + cycle * 0.569840291f, 1);
                DrawReproductionFlight(surface, frame, seed, height, Mathf.PosMod(phase, 1),
                    alpha * surge * 0.86f, 0.58f + height * 0.38f);
            }
        }
        float received = surge * (0.45f + 0.12f * Mathf.Sin(_time * 41));
        foreach (var flight in _reproductionFlights)
        {
            float age = _time - flight.StartedAt;
            if (age < 0) continue;
            if (age < flight.Duration)
                DrawReproductionFlight(surface, frame, flight.Seed, flight.Height, age / flight.Duration, alpha);
            float arrivalAge = _time - flight.ArrivesAt;
            if (arrivalAge >= 0 && arrivalAge < 0.28f)
                received = Mathf.Max(received, 1 - arrivalAge / 0.28f);
        }
        float light = (0.12f * KarenVfxComposition.AmbientWeight(_creature?.Entity)
            + surge * 0.16f + received * 0.7f) * alpha;
        bool atIcon = UsingVoidIconOrbit;
        float unit = atIcon ? 1 / Mathf.Max(GetGlobalTransformWithCanvas().X.Length(), 0.001f) : 1;
        if (atIcon)
        {
            surface.DrawCircle(gate, 29 * unit, new Color(ReproductionGold, light * 0.08f));
            surface.DrawArc(gate, 24 * unit, 0, Mathf.Tau, 40, new Color(ReproductionGold, light * 0.5f), 1.5f * unit, true);
        }
        else
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var edge = gate + new Vector2(side * 47, 0);
                surface.DrawLine(edge + new Vector2(side * 19, 13), edge + new Vector2(0, 4), new Color(ReproductionRed, light), 3, true);
                surface.DrawCircle(edge, 10, new Color(ReproductionGold, light * 0.07f));
                surface.DrawCircle(edge, 4, new Color(ReproductionGold, light * 0.2f));
                surface.DrawCircle(edge, 1.7f, new Color(ReproductionGold, light));
            }
        }
        if (received <= 0) return;
        float bloom = received * alpha;
        if (atIcon)
        {
            surface.DrawCircle(gate, 25 * unit, new Color(ReproductionGold, bloom * 0.15f));
            surface.DrawArc(gate, 18 * unit, 0, Mathf.Tau, 32, new Color(ReproductionGold, bloom * 0.75f), 2 * unit, true);
        }
        else
        {
            surface.DrawLine(gate + new Vector2(-49, 0), gate + new Vector2(49, 0), new Color(ReproductionGold, bloom * 0.14f), 27, true);
            surface.DrawLine(gate + new Vector2(-40, 0), gate + new Vector2(40, 0), new Color(ReproductionGold, bloom * 0.8f), 4, true);
        }
        DrawStar(surface, gate, (14 + 9 * received) * unit, ReproductionGold, bloom);
        for (int i = 0; i < 9; i++)
        {
            float phase = i * 2.399963f;
            var spread = atIcon ? new Vector2(Mathf.Cos(phase), Mathf.Sin(phase)) * 22 * unit
                : new Vector2(Mathf.Cos(phase) * 46, Mathf.Sin(phase) * 25 - 14);
            surface.DrawCircle(gate + spread * (1 - received), (1.4f + i % 3 * 0.6f) * unit,
                new Color(ReproductionGold, bloom * 0.65f));
        }
    }

    private void ReproductionPath(float seed, float height, out Vector2 source, out Vector2 bend, out Vector2 gate)
    {
        gate = ReproductionGate;
        var canvas = GetGlobalTransformWithCanvas();
        var inverse = canvas.AffineInverse();
        var screenGate = canvas * gate;
        Vector2 screenSource;
        if (UsingVoidIconOrbit)
        {
            // Approach from the right, then turn below the energy orb into the
            // draw icon. The endpoint follows the icon's hover and resize.
            screenSource = screenGate + new Vector2(230 + seed * 340, -210 - height * 320);
            bend = inverse * (screenGate + new Vector2(235 + seed * 55, -50 - height * 25));
        }
        else
        {
            var offset = new Vector2(-170 + seed * 640, -85 - height * 255);
            screenSource = canvas * (gate + offset);
            bend = gate + new Vector2(offset.X * 0.65f + (seed - 0.5f) * 120, -135 - (1 - height) * 125);
        }
        var viewport = GetViewportRect().Size;
        screenSource.X = Mathf.Clamp(screenSource.X, 45, viewport.X - 45);
        screenSource.Y = Mathf.Clamp(screenSource.Y, 85, viewport.Y - 120);
        source = inverse * screenSource;
    }

    private static Vector2 ReproductionFlightPoint(Vector2 source, Vector2 bend, Vector2 gate, float t)
    {
        float travel = Mathf.Pow(Mathf.Clamp(t, 0, 1), 1.5f);
        return (1 - travel) * (1 - travel) * source
            + 2 * (1 - travel) * travel * bend + travel * travel * gate;
    }

    private void DrawReproductionFlight(CanvasItem surface, Transform2D frame, float seed, float height,
        float t, float alpha, float size = 1)
    {
        ReproductionPath(seed, height, out var source, out var bend, out var gate);
        Vector2 Point(float phase) => ReproductionFlightPoint(source, bend, gate, phase);
        var position = Point(t);
        float opacity = Mathf.SmoothStep(0, 0.08f, t) * alpha;
        for (int i = 7; i >= 1; i--)
        {
            float previous = t - i * 0.025f;
            if (previous < 0) continue;
            var tail = Point(previous);
            float fade = (1 - i / 8f) * opacity;
            surface.DrawCircle(tail, 3 + (8 - i) * 0.7f, new Color(ReproductionGold, fade * 0.05f));
            surface.DrawLine(tail, Point(previous + 0.025f), new Color(ReproductionGold, fade * 0.5f),
                1.5f + (8 - i) * 0.3f, true);
        }
        if (UsingVoidIconOrbit) size *= 0.72f / Mathf.Max(GetGlobalTransformWithCanvas().X.Length(), 0.001f);
        float width = (62 + seed * 14) * (0.70f + 0.3f * Mathf.Sin(t * Mathf.Pi)) * size;
        width *= 1 - Mathf.SmoothStep(0.68f, 1, t) * 0.95f;
        float turn = Mathf.Cos(t * Mathf.Pi * 1.3f + seed * 2);
        float angle = (seed - 0.5f) * 0.85f + t * (seed < 0.5f ? -0.8f : 0.8f);
        DrawReproductionCard(surface, frame, position, width, angle, Mathf.Max(0.22f, Mathf.Abs(turn)),
            opacity * (1 - Mathf.SmoothStep(0.86f, 1, t)));
        surface.DrawSetTransformMatrix(frame);
        if (t > 0.68f)
            DrawStar(surface, position, (3.5f + 3 * (1 - t)) * size, ReproductionGold,
                opacity * Mathf.SmoothStep(0.68f, 0.88f, t));
    }

    private void DrawReproductionCard(CanvasItem surface, Transform2D frame, Vector2 center, float width, float angle, float flip, float alpha)
    {
        surface.DrawSetTransformMatrix(frame * new Transform2D(Vector2.FromAngle(angle) * flip,
            Vector2.FromAngle(angle + Mathf.Pi * 0.5f), center));
        var rect = new Rect2(-width * 0.5f, -width * 0.69f, width, width * 1.38f);
        surface.DrawRect(rect.Grow(5), new Color(ReproductionGold, alpha * 0.08f));
        surface.DrawRect(rect.Grow(2), new Color(ReproductionGold, alpha * 0.24f));
        surface.DrawRect(rect, new Color(ReproductionPaper, alpha * 0.9f));
        surface.DrawRect(rect.Grow(-Mathf.Min(1.5f, width * 0.2f)), new Color(0.14f, 0.08f, 0.12f, alpha * 0.92f));
        if (ContinuePortrait != null && width > 16)
        {
            surface.DrawTextureRect(ContinuePortrait, new Rect2(rect.Position + new Vector2(3, 4), new Vector2(width - 6, width * 0.89f)), false, new Color(1, 1, 1, alpha));
            surface.DrawLine(new Vector2(-width * 0.3f, width * 0.38f), new Vector2(width * 0.3f, width * 0.38f), new Color(ReproductionGold, alpha * 0.65f), 1);
            surface.DrawLine(new Vector2(-width * 0.22f, width * 0.48f), new Vector2(width * 0.22f, width * 0.48f), new Color(ReproductionGold, alpha * 0.40f), 1);
        }
    }
}
