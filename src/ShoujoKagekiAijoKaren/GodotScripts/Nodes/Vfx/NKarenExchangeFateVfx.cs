using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Room-owned flowing silk and optical dust. Native commands own all cards.</summary>
public partial class NKarenExchangeFateVfx : Node2D
{
    // Shared visual resources are prepared during the run loading screen as well as on demand.
    internal static void PrepareResources()
    {
        _soft ??= new GradientTexture2D
        {
            Width = 128,
            Height = 128,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new(.5f, .5f),
            FillTo = new(1, .5f),
            Gradient = new Gradient { Offsets = [0, .12f, .40f, 1], Colors = [Colors.White, new(1, 1, 1, .72f), new(1, 1, 1, .16f), Colors.Transparent] }
        };
        _streak ??= GD.Load<Texture2D>("res://images/vfx/sts/horizontal_line.png");
        _shader ??= GD.Load<Shader>("res://shaders/karen_fate_silk.gdshader");
    }
    internal static Godot.Resource?[] PreparedResources => [_soft, _streak, _shader];

    private const int Segments = 64;
    private const float LeadIn = .06f, SweepDuration = .46f, SwapAt = LeadIn + SweepDuration * .5f;
    private const float SweepEnd = LeadIn + SweepDuration, DrainDuration = .16f, EndAt = .76f;
    private static readonly Color Red = new("#fa5169"), Blue = new("#779fff"), Gold = new("#ffc16d"), Ivory = new("#fff4db");
    private static GradientTexture2D? _soft;
    private static Texture2D? _streak;
    private static Shader? _shader;
    private sealed class Silk(bool blue, int strand)
    {
        public readonly bool Blue = blue;
        public readonly int Strand = strand;
        public readonly ArrayMesh Mesh = new();
        public readonly MeshInstance2D Node = new();
        public readonly ShaderMaterial Material = new();
        public readonly Vector3[] Vertices = new Vector3[(Segments + 1) * 2];
        public readonly Vector2[] Uvs = new Vector2[(Segments + 1) * 2];
        public readonly int[] Indices = new int[Segments * 6];
        public readonly Godot.Collections.Array Arrays = new();
    }
    private sealed class Dust(Vector2 p, Vector2 velocity, float life, float radius, bool glint)
    {
        public Vector2 Position = p, Velocity = velocity;
        public readonly float Life = life, Radius = radius;
        public readonly bool Glint = glint;
        public float Age;
    }
    private readonly Node2D _light = new(), _front = new();
    private readonly List<Silk> _silks = new();
    private readonly List<Dust> _dust = new(180);
    private readonly TaskCompletionSource _exchangeReady = new();
    private readonly RandomNumberGenerator _random = new() { Seed = 29617 };
    private NCombatRoom _room = null!;
    private Player _owner = null!;
    private CardModel[] _outgoing = [], _incoming = [];
    private PileType _destination;
    private Vector2 _hand, _pile, _focus, _normal;
    private float _unit, _age, _exchangeAt = -1, _finishAt = -1, _speed, _dustClock;
    private bool _exiting;
    public int SentCount { get; private set; }
    public int ReceivedCount { get; private set; }
    public bool Confirmed { get; private set; }
    public bool ExchangeStarted => _exchangeAt >= 0;
    public float ExchangeProgress => Travel;
    private bool Alive => !_exiting && GodotObject.IsInstanceValid(this) && IsInsideTree() && !IsQueuedForDeletion();
    private float Travel => Mathf.SmoothStep(0, 1, Mathf.Clamp((_age - LeadIn) / SweepDuration, 0, 1));
    private float Drain => Mathf.SmoothStep(0, 1, Mathf.Clamp((_age - SweepEnd) / DrainDuration, 0, 1));
    private float Arrival => Mathf.SmoothStep(0, 1, Mathf.Clamp((_age - SweepEnd + .08f) / .16f, 0, 1));
    private float Alpha => Mathf.Clamp(_age / .045f, 0, 1) * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp((_age - .56f) / (EndAt - .56f), 0, 1)));
    private float CrossPulse => _exchangeAt < 0 || _incoming.Length == 0 || _outgoing.Length == 0 ? 0
        : Mathf.Exp(-Mathf.Pow((_age - _exchangeAt) / .065f, 2)) * Alpha;

    public static NKarenExchangeFateVfx? Play(Player owner, CardModel[] outgoing, CardModel[] incoming)
    {
        if (NonInteractiveMode.IsActive || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant
            || NCombatRoom.Instance is not { } room || CombatManager.Instance.IsOverOrEnding
            || room.GetCreatureNode(owner.Creature) == null || outgoing.Length + incoming.Length == 0) return null;
        foreach (var old in room.Ui.GetChildren().OfType<NKarenExchangeFateVfx>().Where(n => n._owner == owner)) { old.Hide(); old.QueueFree(); }
        var cue = new NKarenExchangeFateVfx
        {
            Name = "KarenExchangeFateVfx",
            _owner = owner,
            _room = room,
            _outgoing = outgoing,
            _incoming = incoming,
            _destination = PromisePileManager.IsVoidMode(owner) ? PileType.Draw : KarenCustomEnum.PromisePile,
            _speed = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? .60f : 1f
        };
        room.Ui.AddChild(cue); return cue;
    }
    public Task WaitForExchange() => _exchangeReady.Task;
    public void BeginExchange()
    {
        if (!Alive || _exchangeAt >= 0) return;
        _exchangeAt = _age;
        if (_outgoing.Length > 0 && _incoming.Length > 0)
            for (int i = 0; i < 16; i++) Emit(_focus, Vector2.FromAngle(i * Mathf.Tau / 16) * _random.RandfRange(60, 170) * _unit, i % 4 == 0);
    }
    public void ConfirmTransfers()
    {
        if (!Alive) return;
        SentCount = _outgoing.Count(c => c.Pile?.Type == _destination);
        ReceivedCount = _incoming.Count(c => c.Pile?.Type == PileType.Hand);
        Confirmed = SentCount + ReceivedCount > 0;
    }
    public void Finish() { if (Alive && _finishAt < 0) _finishAt = _age; }
    public override void _Ready()
    {
        ZIndex = 45; TextureFilter = TextureFilterEnum.Linear;
        PrepareResources();
        _light.Material = _front.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        _front.ZIndex = 6;
        AddChild(_light); AddChild(_front);
        _light.Draw += DrawLight; _front.Draw += DrawDust;
        KarenVfxComposition.RegisterCue(this, _owner.Creature);
        GlobalTransform = GetCanvasTransform().AffineInverse(); _unit = GetViewportRect().Size.Y / 1080f;
        var hit = _room.GetCreatureNode(_owner.Creature)!.Hitbox;
        var body = hit.GetGlobalTransformWithCanvas() * (hit.Size * .5f);
        _hand = body + new Vector2(85, 70) * _unit;
        // Only the local actor's actual HUD is visible. Other players keep private hands.
        if (LocalContext.IsMe(_owner))
        {
            var centers = _outgoing.Select(c => NCard.FindOnTable(c)).Where(n => n != null && n.IsInsideTree())
                .Select(n => n!.Body.GetGlobalTransformWithCanvas() * (n.Body.Size * .5f)).ToArray();
            _hand = centers.Length > 0 ? centers.Aggregate(Vector2.Zero, (sum, p) => sum + p) / centers.Length
                : new Vector2(GetViewportRect().Size.X * .48f, GetViewportRect().Size.Y * .87f);
            _hand += Vector2.Up * 105 * _unit;
        }
        _pile = KarenPromiseVfxStarManager.GetPileCanvasPosition(_owner) ?? body + Vector2.Up * 140 * _unit;
        _focus = (_hand + _pile) * .5f + new Vector2(30, -160) * _unit;
        _normal = (_pile - _hand).Normalized().Orthogonal();
        if (_normal.LengthSquared() < .1f) _normal = Vector2.Right;
        for (int b = 0; b < 2; b++) for (int s = 0; s < 3; s++) CreateSilk(b == 1, s);
    }
    private void CreateSilk(bool blue, int strand)
    {
        var s = new Silk(blue, strand);
        s.Material.Shader = _shader; s.Material.ResourceLocalToScene = true;
        s.Material.SetShaderParameter("tint", blue ? Blue : Red); s.Material.SetShaderParameter("seed", strand * 2.37f + (blue ? 13 : 2));
        s.Node.Mesh = s.Mesh; s.Node.Material = s.Material; s.Node.ZIndex = strand == 0 ? 2 : 1; AddChild(s.Node);
        for (int j = 0; j <= Segments; j++)
        {
            s.Uvs[j * 2] = new(j / (float)Segments, 0); s.Uvs[j * 2 + 1] = new(j / (float)Segments, 1);
            if (j == Segments) continue; int v = j * 2, k = j * 6;
            s.Indices[k] = v; s.Indices[k + 1] = v + 1; s.Indices[k + 2] = v + 2; s.Indices[k + 3] = v + 1; s.Indices[k + 4] = v + 3; s.Indices[k + 5] = v + 2;
        }
        s.Arrays.Resize((int)Mesh.ArrayType.Max); s.Arrays[(int)Mesh.ArrayType.TexUV] = s.Uvs; s.Arrays[(int)Mesh.ArrayType.Index] = s.Indices;
        _silks.Add(s);
    }
    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_room) || !_room.IsInsideTree() || NCombatRoom.Instance != _room
            || CombatManager.Instance.IsOverOrEnding || _owner.Creature.IsDead) { Hide(); QueueFree(); return; }
        float dt = (float)delta / _speed;
        // Hold precisely at the crossing until the awaiting card command resumes.
        // Presentation and rules share this marker even when a render frame is slow.
        _age = _exchangeAt < 0 ? Mathf.Min(_age + dt, SwapAt) : _age + dt;
        // A slow native hook may finish later; it never holds the silk on screen.
        if ((_age > EndAt && _finishAt >= 0) || _age > 3) { Hide(); QueueFree(); return; }
        GlobalTransform = GetCanvasTransform().AffineInverse();
        for (int i = _dust.Count - 1; i >= 0; i--)
        {
            var d = _dust[i]; d.Age += dt; d.Position += d.Velocity * dt; d.Velocity += Vector2.Up * 13 * _unit * dt;
            if (d.Age >= d.Life) _dust.RemoveAt(i);
        }
        _dustClock += dt;
        if (_dustClock >= .022f && _age > LeadIn && _age < SweepEnd)
        {
            _dustClock = 0;
            for (int b = 0; b < 2; b++)
            {
                bool blue = b == 1; if ((blue ? _incoming.Length : _outgoing.Length) == 0) continue;
                var p = Flow(_random.RandfRange(.65f, 1), blue);
                Emit(p, new Vector2(_random.RandfRange(-16, 16), _random.RandfRange(-28, -8)) * _unit, _random.Randf() < .10f);
            }
        }
        foreach (var silk in _silks) UpdateSilk(silk);
        KarenVfxComposition.SetCueStrength(this, Alpha);
        QueueRedraw(); _light.QueueRedraw(); _front.QueueRedraw();
        if (_age >= SwapAt) _exchangeReady.TrySetResult();
    }
    private Vector2 Path(float t, bool blue)
    {
        var axis = _pile - _hand;
        var bend = _normal * Mathf.Clamp(axis.Length() * .22f, 45 * _unit, 110 * _unit) * (blue ? -1 : 1);
        var lift = (_focus - (_hand + _pile) * .5f) * (4f / 3);
        // One uninterrupted sweep through the open space above the incoming cards.
        // At t=.5 the opposing curves meet exactly at the shared crossing marker.
        return _hand.BezierInterpolate(_hand + axis * .28f + lift + bend, _hand + axis * .72f + lift - bend, _pile, blue ? 1 - t : t);
    }
    private Vector2 Flow(float u, bool blue)
    {
        float tail = Mathf.Lerp(Mathf.Max(0, Travel - .44f), 1, Drain);
        return Path(Mathf.Lerp(tail, Travel, u), blue);
    }
    private void UpdateSilk(Silk s)
    {
        float active = (s.Blue ? _incoming.Length : _outgoing.Length) > 0 ? Alpha * (1 - Drain) : 0;
        s.Node.Visible = active > .001f; if (!s.Node.Visible) return;
        for (int j = 0; j <= Segments; j++)
        {
            float u = j / (float)Segments; var p = Flow(u, s.Blue);
            var tangent = (Flow(Mathf.Min(1, u + .003f), s.Blue) - Flow(Mathf.Max(0, u - .003f), s.Blue)).Normalized();
            if (tangent.LengthSquared() < .1f) tangent = Vector2.Up;
            var normal = tangent.Orthogonal();
            p += normal * Mathf.Sin(u * 9 - _age * 12 + s.Strand * 2.2f) * s.Strand * 4 * _unit * Mathf.Sin(u * Mathf.Pi);
            float half = (2 + 34 * Mathf.Pow(Mathf.Max(0, Mathf.Sin(u * Mathf.Pi)), .8f)) * (s.Strand == 0 ? 1 : .45f) * _unit
                * (.80f + .20f * Mathf.Sin(u * 12 - _age * 9 + s.Strand)) * Mathf.Sqrt(Mathf.Clamp(Travel / .22f, 0, 1));
            s.Vertices[j * 2] = new(p.X + normal.X * half, p.Y + normal.Y * half, 0);
            s.Vertices[j * 2 + 1] = new(p.X - normal.X * half, p.Y - normal.Y * half, 0);
        }
        s.Material.SetShaderParameter("age", _age); s.Material.SetShaderParameter("strength", active * (s.Strand == 0 ? .84f : .35f));
        s.Material.SetShaderParameter("erosion", Drain);
        s.Arrays[(int)Mesh.ArrayType.Vertex] = s.Vertices; s.Mesh.ClearSurfaces(); s.Mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, s.Arrays);
    }
    private void DrawLight()
    {
        float a = Alpha * (1 - Drain);
        for (int b = 0; b < 2; b++)
        {
            bool blue = b == 1; if ((blue ? _incoming.Length : _outgoing.Length) == 0) continue; var color = blue ? Blue : Red;
            Glow(_light, Flow(1, blue), Vector2.One * 38 * _unit, color, a * .20f * Travel);
            for (int j = 1; j < 36; j++) _light.DrawLine(Flow(j / 36f, blue), Flow((j + 1) / 36f, blue), color with { A = a * .020f }, 15 * _unit, true);
            var last = Flow(.03f, blue);
            for (int i = 1; i <= 48; i++)
            {
                float u = .03f + i / 48f * .94f;
                var p = Flow(u, blue) + new Vector2(Mathf.Sin(u * 9 + _age * 2), Mathf.Cos(u * 8 + _age * 2))
                    * 7 * _unit * Mathf.Sin(u * Mathf.Pi);
                float fade = Mathf.Sin(u * Mathf.Pi) * (.35f + .25f * Mathf.Sin(u * 19 - _age * 4));
                _light.DrawLine(last, p, Gold with { A = a * fade }, .75f * _unit, true); last = p;
            }
        }
        Glow(_light, _focus, Vector2.One * (30 + 55 * CrossPulse) * _unit, Gold, CrossPulse * .42f);
    }
    private void Emit(Vector2 p, Vector2 velocity, bool glint)
    {
        if (_dust.Count < 180) _dust.Add(new Dust(p, velocity, _random.RandfRange(.12f, .28f), _random.RandfRange(glint ? 2 : .5f, glint ? 3.7f : 1.4f) * _unit, glint));
    }
    private void DrawDust()
    {
        foreach (var d in _dust)
        {
            float fade = Mathf.Sin(Mathf.Pow(d.Age / d.Life, .7f) * Mathf.Pi) * Alpha;
            if (d.Glint) Glint(_front, d.Position, d.Radius * 3, fade * .65f);
            else { Glow(_front, d.Position, Vector2.One * d.Radius * 4, Gold, fade * .17f); _front.DrawCircle(d.Position, d.Radius * .6f, Ivory with { A = fade * .8f }); }
        }
        Glint(_front, _focus, (10 + CrossPulse * 23) * _unit, CrossPulse * .85f);
        if (!Confirmed || Arrival <= .1f) return;
        if (SentCount > 0) Glow(_front, _pile, Vector2.One * 65 * _unit, Gold, Alpha * Arrival * .36f);
        foreach (var card in _incoming)
        {
            if (!LocalContext.IsMe(_owner) || card.Pile?.Type != PileType.Hand || NCard.FindOnTable(card) is not { } node || !node.IsVisibleInTree()) continue;
            Glint(_front, node.Body.GetGlobalTransformWithCanvas() * new Vector2(node.Body.Size.X * .5f, 25), 12 * _unit, Alpha * Arrival * .6f);
        }
    }
    private static void Glow(Node2D layer, Vector2 p, Vector2 radius, Color color, float a)
        => layer.DrawTextureRect(_soft!, new Rect2(p - radius, radius * 2), false, color with { A = a });
    private static void Glint(Node2D layer, Vector2 p, float radius, float a)
    {
        if (a < .002f) return; Glow(layer, p, Vector2.One * radius * 1.2f, Gold, a * .26f);
        layer.DrawTextureRect(_streak!, new Rect2(p - new Vector2(radius * 1.8f, radius * .28f), new Vector2(radius * 3.6f, radius * .56f)), false, Ivory with { A = a });
        layer.DrawSetTransform(p, Mathf.Pi / 2); layer.DrawTextureRect(_streak!, new Rect2(-radius, -radius * .12f, radius * 2, radius * .24f), false, Ivory with { A = a * .72f });
        layer.DrawSetTransform(Vector2.Zero); layer.DrawCircle(p, radius * .07f, Ivory with { A = a });
    }
    public override void _ExitTree()
    {
        _exiting = true;
        KarenVfxComposition.RemoveCue(this);
        foreach (var s in _silks) s.Mesh.Dispose();
        _silks.Clear(); _dust.Clear(); _outgoing = []; _incoming = []; _random.Dispose();
        // Resume outside tree removal: native card commands may add/reparent nodes.
        var ready = _exchangeReady;
        Callable.From(() => { ready.TrySetResult(); }).CallDeferred();
    }
}
