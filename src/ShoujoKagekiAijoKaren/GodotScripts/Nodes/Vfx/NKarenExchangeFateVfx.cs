using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Two childhood hair ornaments exchange places along opposed star trails.
/// Observes the existing SwitchHand command; never borrows or changes a native card node.</summary>
public partial class NKarenExchangeFateVfx : Node2D
{
    private static readonly Color Red = new("#f95368"), Blue = new("#63c9ff"),
        Gold = new("#ffc665"), Ivory = new("#fff0c7"), Ink = new("#60343c");
    private static GradientTexture2D? _soft;
    private readonly Node2D _light = new();
    private readonly List<AudioStreamPlayer> _sounds = new();
    private readonly Vector2[] _ribbon = new Vector2[4], _star = new Vector2[8];
    private readonly Vector2[] _orbit = new Vector2[33];
    private NCombatRoom _room = null!;
    private Player _owner = null!;
    private CardModel[] _outgoing = [], _incoming = [];
    private PileType _destination;
    private Vector2 _hand, _pile, _normal;
    private float _unit, _age, _exchangeAt = -1, _finishAt = -1, _speed;
    private bool _crossed;
    public int SentCount { get; private set; }
    public int ReceivedCount { get; private set; }
    public bool Confirmed { get; private set; }
    private bool Alive => GodotObject.IsInstanceValid(this) && IsInsideTree() && !IsQueuedForDeletion();
    private float Travel => _exchangeAt < 0 ? 0 : Mathf.Clamp((_age - _exchangeAt) / .72f, 0, 1);
    private float Alpha => Mathf.SmoothStep(0, 1, Mathf.Clamp(_age / .20f, 0, 1))
        * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp((_age - FadeAt) / .60f, 0, 1)));
    private float FadeAt => _finishAt < 0 ? 6 : Mathf.Max(_finishAt, _exchangeAt + .72f);

    public static NKarenExchangeFateVfx? Play(Player owner, CardModel[] outgoing, CardModel[] incoming)
    {
        if (NonInteractiveMode.IsActive || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant
            || NCombatRoom.Instance is not { } room || CombatManager.Instance.IsOverOrEnding
            || room.GetCreatureNode(owner.Creature) == null || outgoing.Length + incoming.Length == 0) return null;
        // Replays from the same actor replace decoration only; actors have independent anchors.
        foreach (var old in room.Ui.GetChildren().OfType<NKarenExchangeFateVfx>().Where(n => n._owner == owner))
        { old.Hide(); old.QueueFree(); }
        var cue = new NKarenExchangeFateVfx { Name = "KarenExchangeFateVfx", _owner = owner, _room = room,
            _outgoing = outgoing, _incoming = incoming,
            _destination = PromisePileManager.IsVoidMode(owner) ? PileType.Draw : KarenCustomEnum.PromisePile,
            _speed = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? .60f : 1f };
        room.Ui.AddChild(cue);
        return cue;
    }

    public static Task WaitForOpening() => Cmd.CustomScaledWait(.18f, .30f);
    public void BeginExchange() { if (Alive && _exchangeAt < 0) _exchangeAt = _age; }
    public void ConfirmTransfers()
    {
        if (!Alive) return;
        // Hooks, full hands and Infinite mode may redirect/remove cards. Confirm identities,
        // rather than presenting the planned inventory as a successful exchange.
        SentCount = _outgoing.Count(c => c.Pile?.Type == _destination);
        ReceivedCount = _incoming.Count(c => c.Pile?.Type == PileType.Hand);
        Confirmed = SentCount + ReceivedCount > 0;
    }
    public void Finish() { if (Alive && _finishAt < 0) _finishAt = _age; }

    public override void _Ready()
    {
        ZIndex = 45;
        _soft ??= new GradientTexture2D { Width = 96, Height = 96, Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new(.5f, .5f), FillTo = new(1, .5f), Gradient = new Gradient {
                Offsets = [0, .20f, .50f, 1], Colors = [Colors.White, new(1, 1, 1, .6f), new(1, 1, 1, .14f), Colors.Transparent] } };
        _light.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        _light.ShowBehindParent = true;
        AddChild(_light);
        _light.Draw += DrawLight;
        TextureFilter = TextureFilterEnum.Linear;
        KarenVfxComposition.RegisterCue(this, _owner.Creature);
        GlobalTransform = GetCanvasTransform().AffineInverse();
        _unit = GetViewportRect().Size.Y / 1080f;
        var hit = _room.GetCreatureNode(_owner.Creature)!.Hitbox;
        _hand = hit.GetGlobalTransformWithCanvas() * new Vector2(hit.Size.X * .5f, hit.Size.Y * .8f);
        // The remote player's hand is private. Only the local player uses the actual HUD.
        if (LocalContext.IsMe(_owner))
        {
            var centers = _outgoing.Select(c => NCard.FindOnTable(c)).Where(n => n != null && n.IsInsideTree())
                .Select(n => n!.Body.GetGlobalTransformWithCanvas() * (n.Body.Size * .5f)).ToArray();
            _hand = centers.Length > 0 ? centers.Aggregate(Vector2.Zero, (sum, p) => sum + p) / centers.Length
                : new Vector2(GetViewportRect().Size.X * .48f, GetViewportRect().Size.Y * .87f);
            _hand += Vector2.Up * 105 * _unit;
        }
        else _hand += new Vector2(85, -20) * _unit;
        _pile = KarenPromiseVfxStarManager.GetPileCanvasPosition(_owner)
            ?? hit.GetGlobalTransformWithCanvas() * new Vector2(hit.Size.X * .5f, -50);
        _normal = (_pile - _hand).Normalized().Orthogonal();
        Sound("karen_benefit_gather.wav", .21f, .96f);
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_room) || !_room.IsInsideTree() || NCombatRoom.Instance != _room
            || CombatManager.Instance.IsOverOrEnding || _owner.Creature.IsDead)
        { Hide(); QueueFree(); return; }
        _age += (float)delta / _speed;
        if (_age > FadeAt + .60f || _age > 7) { Hide(); QueueFree(); return; }
        GlobalTransform = GetCanvasTransform().AffineInverse();
        if (!_crossed && Travel >= .5f)
        {
            _crossed = true;
            Sound("karen_star_resolve.wav", .24f, 1.15f);
        }
        KarenVfxComposition.SetCueStrength(this, Alpha);
        QueueRedraw(); _light.QueueRedraw();
    }

    // Opposed S-curves meet at the center, then deposit each ornament at the other's origin.
    private Vector2 Path(float t, bool blue)
    {
        float s = blue ? 1 - t : t;
        float bow = Mathf.Min(_hand.DistanceTo(_pile) * .28f, 150 * _unit);
        return _hand.Lerp(_pile, s) + _normal * Mathf.Sin(s * Mathf.Tau) * bow * (blue ? -1 : 1);
    }
    private float Motion => Mathf.SmoothStep(0, 1, Travel);

    public override void _Draw()
    {
        float a = Alpha;
        if (a <= 0) return;
        if (_outgoing.Length > 0) Ribbon(false, Red, a);
        if (_incoming.Length > 0) Ribbon(true, Blue, a);
        // Ornaments are memories of the promise, not proxy card sprites.
        if (_outgoing.Length > 0) Ornament(Path(Motion, false), false, -.20f + .4f * Motion, a);
        if (_incoming.Length > 0) Ornament(Path(Motion, true), true, .24f - .5f * Motion, a);
        if (_exchangeAt >= 0)
        {
            for (int i = 0; i < 24; i++)
            {
                bool blue = i % 2 == 0;
                if ((blue ? _incoming.Length : _outgoing.Length) == 0) continue;
                float t = Motion - i % 12 * .024f;
                if (t < 0 || t > 1) continue;
                var p = Path(t, blue) + _normal * Mathf.Sin(i * 2.4f + _age * 5) * (9 + i % 3 * 6) * _unit;
                Star(this, p, (2.2f + i % 3) * _unit, (blue ? Blue : Gold) with { A = a * .8f });
            }
        }
        if (Confirmed && Travel >= 1) DrawArrivals(a);
        DrawCross(this, false);
    }

    private void Ribbon(bool blue, Color color, float a)
    {
        if (_exchangeAt < 0 || Motion < .001f) return;
        float end = Motion;
        float start = Mathf.Max(0, end - .68f);
        const int steps = 52;
        for (int i = 0; i < steps; i++)
        {
            float u = i / (float)steps, v = (i + 1f) / steps;
            float t = Mathf.Lerp(start, end, u), t2 = Mathf.Lerp(start, end, v);
            var p = Path(t, blue); var q = Path(t2, blue);
            var n = (q - p).Normalized().Orthogonal();
            float w = (1.5f + 14 * Mathf.Sin(u * Mathf.Pi)) * _unit;
            _ribbon[0] = p + n * w; _ribbon[1] = q + n * w;
            _ribbon[2] = q - n * w; _ribbon[3] = p - n * w;
            DrawColoredPolygon(_ribbon, color with { A = a * .38f * u });
            DrawLine(p, q, color with { A = a * .85f * u }, 3.2f * _unit, true);
            DrawLine(p + n * 12 * _unit, q + n * 12 * _unit, Gold with { A = a * .65f * u }, 1.2f * _unit, true);
        }
    }

    private void Ornament(Vector2 p, bool crossed, float angle, float a)
    {
        float size = _unit * (1.05f + .2f * Mathf.SmoothStep(0, 1, Mathf.Clamp(_age / .25f, 0, 1)));
        DrawSetTransform(p, angle, Vector2.One * size);
        for (int i = 0; i < _orbit.Length; i++)
        {
            float t = i / 32f * Mathf.Tau;
            _orbit[i] = new Vector2(Mathf.Cos(t) * 42, Mathf.Sin(t) * 15 + 8);
        }
        DrawPolyline(_orbit, Gold with { A = a * .45f }, 1, true);
        if (crossed)
        {
            // Two parallel pairs of slim bars match Hikari's recognizable crossed hairclip.
            foreach (float x in new[] { -8f, 8f })
            {
                DrawLine(new Vector2(x - 14, -25), new Vector2(x + 14, 25), Ink with { A = a }, 9, true);
                DrawLine(new Vector2(x - 14, -25), new Vector2(x + 14, 25), Gold with { A = a }, 6, true);
                DrawLine(new Vector2(x - 15, -25), new Vector2(x + 13, 25), Ivory with { A = a }, 1.5f, true);
            }
            foreach (float y in new[] { -7f, 7f })
            {
                DrawLine(new(-25, y + 12), new(25, y - 12), Ink with { A = a }, 10, true);
                DrawLine(new(-25, y + 12), new(25, y - 12), Gold with { A = a }, 6, true);
                DrawLine(new(-25, y + 10), new(25, y - 14), Ivory with { A = a }, 1.5f, true);
            }
        }
        else
        {
            // Three rounded enamel lobes with a gold base: the childhood crown motif.
            for (int i = 0; i < 3; i++)
            {
                Vector2 c = new((i - 1) * 15, i == 1 ? -10 : -5);
                DrawSetTransform(p + c.Rotated(angle) * size, angle + (i - 1) * .30f, new Vector2(.68f, 1) * size);
                DrawCircle(Vector2.Zero, 22, Ink with { A = a });
                DrawCircle(Vector2.Zero, 19, Gold with { A = a });
                DrawCircle(Vector2.Zero, 14, new Color(.68f, .10f, .20f, a));
                DrawArc(Vector2.Zero, 16, 3.5f, 5.6f, 14, Ivory with { A = a }, 2, true);
            }
            DrawSetTransform(p, angle, Vector2.One * size);
            DrawRect(new Rect2(-28, 8, 56, 12), Gold with { A = a });
            DrawLine(new(-24, 11), new(24, 11), Ivory with { A = a }, 2, true);
            Star(this, new(0, -36), 7, Gold with { A = a });
            for (int i = -1; i <= 1; i++) DrawCircle(new(i * 17, 15), 2.5f, Ink with { A = a });
        }
        DrawSetTransform(Vector2.Zero);
    }

    private void DrawLight()
    {
        float a = Alpha;
        if (_outgoing.Length > 0) { LightTrail(false, Red, a); Glow(_light, Path(Motion, false), 100 * _unit, Red, a * .42f); }
        if (_incoming.Length > 0) { LightTrail(true, Blue, a); Glow(_light, Path(Motion, true), 100 * _unit, Blue, a * .42f); }
        DrawCross(_light, true);
    }
    private void LightTrail(bool blue, Color color, float a)
    {
        if (_exchangeAt < 0 || Motion < .001f) return;
        float start = Mathf.Max(0, Motion - .68f);
        for (int i = 0; i < 40; i++)
        {
            float u = i / 40f;
            var p = Path(Mathf.Lerp(start, Motion, u), blue);
            var q = Path(Mathf.Lerp(start, Motion, (i + 1) / 40f), blue);
            _light.DrawLine(p, q, color with { A = a * .055f * u }, 24 * _unit, true);
            _light.DrawLine(p, q, color with { A = a * .16f * u }, 9 * _unit, true);
        }
    }
    private void DrawCross(Node2D layer, bool glow)
    {
        if (_exchangeAt < 0 || _outgoing.Length == 0 || _incoming.Length == 0) return;
        float pulse = Mathf.Max(0, 1 - Mathf.Abs(Travel - .5f) / .22f) * Alpha;
        var p = _hand.Lerp(_pile, .5f);
        if (glow) Glow(layer, p, 170 * _unit, Gold, pulse * .65f);
        else Star(layer, p, (8 + pulse * 24) * _unit, Ivory with { A = pulse });
    }
    private void DrawArrivals(float a)
    {
        foreach (var card in _incoming)
        {
            if (!LocalContext.IsMe(_owner) || card.Pile?.Type != PileType.Hand
                || NCard.FindOnTable(card) is not { } node || !node.IsVisibleInTree()) continue;
            var p = node.Body.GetGlobalTransformWithCanvas() * new Vector2(node.Body.Size.X * .5f, 30);
            Star(this, p, 8 * _unit, Ivory with { A = a * .8f });
        }
        if (SentCount > 0)
            for (int i = 0; i < 8; i++)
            {
                var p = _pile + Vector2.FromAngle(i * Mathf.Tau / 8 - _age) * (30 + (_age - FadeAt) * 18) * _unit;
                Star(this, p, 3 * _unit, Gold with { A = a * .65f });
            }
    }
    private void Star(Node2D layer, Vector2 p, float r, Color color)
    {
        for (int i = 0; i < 8; i++) _star[i] = p + Vector2.FromAngle(-Mathf.Pi / 2 + i * Mathf.Pi / 4) * r * (i % 2 == 0 ? 1 : .22f);
        layer.DrawColoredPolygon(_star, color);
    }
    private static void Glow(Node2D layer, Vector2 p, float r, Color color, float a)
        => layer.DrawTextureRect(_soft!, new Rect2(p - Vector2.One * r, Vector2.One * r * 2), false, color with { A = a });
    private void Sound(string file, float volume, float pitch)
    {
        if (!LocalContext.IsMe(_owner)) return;
        var sound = KarenAudioManager.PlaySfx(file, volume, pitch / _speed);
        if (sound != null) _sounds.Add(sound);
    }
    public override void _ExitTree()
    {
        KarenVfxComposition.RemoveCue(this);
        foreach (var sound in _sounds)
            if (GodotObject.IsInstanceValid(sound)) { sound.Stop(); sound.QueueFree(); }
        _sounds.Clear(); _outgoing = []; _incoming = [];
    }
}
