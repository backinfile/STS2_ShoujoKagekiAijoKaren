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
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Room-owned jewelled ornaments and flowing silk. Native commands own all cards.</summary>
public partial class NKarenExchangeFateVfx : Node2D
{
    private const int Segments = 64;
    private static readonly Color Red = new("#fa5169"), Blue = new("#779fff"), Gold = new("#ffc16d"), Ivory = new("#fff4db");
    private static GradientTexture2D? _soft;
    private static Texture2D? _atlas, _streak;
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
    private readonly Node2D _light = new(), _ornaments = new(), _front = new();
    private readonly List<Silk> _silks = new();
    private readonly List<Dust> _dust = new(180);
    private readonly List<AudioStreamPlayer> _sounds = new();
    private readonly RandomNumberGenerator _random = new() { Seed = 29617 };
    private NCombatRoom _room = null!;
    private Player _owner = null!;
    private CardModel[] _outgoing = [], _incoming = [];
    private PileType _destination;
    private Vector2 _hand, _pile, _focus, _left, _right;
    private float _unit, _age, _exchangeAt = -1, _finishAt = -1, _speed, _dustClock;
    private bool _crossed;
    public int SentCount { get; private set; }
    public int ReceivedCount { get; private set; }
    public bool Confirmed { get; private set; }
    public float ExchangeProgress => Travel;
    private bool Alive => GodotObject.IsInstanceValid(this) && IsInsideTree() && !IsQueuedForDeletion();
    private float Travel => _exchangeAt < 0 ? 0 : Mathf.Clamp((_age - _exchangeAt) / 1.02f, 0, 1);
    private float Arrival => _exchangeAt < 0 ? 0 : Mathf.SmoothStep(0, 1, Mathf.Clamp((_age - _exchangeAt - 1.02f) / .30f, 0, 1));
    private float FadeAt => _finishAt < 0 ? 6 : Mathf.Max(_finishAt, _exchangeAt + 1.25f);
    private float Alpha => Mathf.SmoothStep(0, 1, Mathf.Clamp(_age / .22f, 0, 1)) * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp((_age - FadeAt) / .60f, 0, 1)));
    private float CrossPulse => _exchangeAt < 0 || _incoming.Length == 0 || _outgoing.Length == 0 ? 0 : Mathf.Exp(-Mathf.Pow((Travel - .50f) / .105f, 2)) * Alpha;

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
    public static Task WaitForOpening() => Cmd.CustomScaledWait(.25f, .42f);
    public void BeginExchange() { if (Alive && _exchangeAt < 0) _exchangeAt = _age; }
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
        _soft ??= new GradientTexture2D
        {
            Width = 128,
            Height = 128,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new(.5f, .5f),
            FillTo = new(1, .5f),
            Gradient = new Gradient { Offsets = [0, .12f, .40f, 1], Colors = [Colors.White, new(1, 1, 1, .72f), new(1, 1, 1, .16f), Colors.Transparent] }
        };
        _atlas ??= GD.Load<Texture2D>("res://images/vfx/exchange_fate/ornaments.png");
        _streak ??= GD.Load<Texture2D>("res://images/vfx/sts/horizontal_line.png");
        _shader ??= GD.Load<Shader>("res://shaders/karen_fate_silk.gdshader");
        _light.Material = _front.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        _ornaments.ZIndex = 5; _front.ZIndex = 6;
        _ornaments.TextureFilter = TextureFilterEnum.LinearWithMipmaps;
        AddChild(_light); AddChild(_ornaments); AddChild(_front);
        _light.Draw += DrawLight; _ornaments.Draw += DrawOrnaments; _front.Draw += DrawDust;
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
        _focus = new Vector2(Mathf.Min(_hand.X - 75 * _unit, body.X + 270 * _unit), body.Y - 125 * _unit);
        _left = _focus + new Vector2(-190, -115) * _unit; _right = _focus + new Vector2(190, 115) * _unit;
        for (int b = 0; b < 2; b++) for (int s = 0; s < 3; s++) CreateSilk(b == 1, s);
        Sound("karen_fate_lift.wav", .27f, 1);
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
        float dt = (float)delta / _speed; _age += dt;
        if (_age > FadeAt + .65f || _age > 7) { Hide(); QueueFree(); return; }
        GlobalTransform = GetCanvasTransform().AffineInverse();
        if (!_crossed && Travel >= .5f)
        {
            _crossed = true; Sound("karen_fate_cross.wav", .25f, 1);
            if (_outgoing.Length > 0 && _incoming.Length > 0)
                for (int i = 0; i < 32; i++) Emit(_focus, Vector2.FromAngle(i * Mathf.Tau / 32) * _random.RandfRange(35, 150) * _unit, i % 5 == 0);
        }
        for (int i = _dust.Count - 1; i >= 0; i--)
        {
            var d = _dust[i]; d.Age += dt; d.Position += d.Velocity * dt; d.Velocity += Vector2.Up * 13 * _unit * dt;
            if (d.Age >= d.Life) _dust.RemoveAt(i);
        }
        _dustClock += dt;
        if (_dustClock >= .022f && _age < FadeAt)
        {
            _dustClock = 0;
            for (int b = 0; b < 2; b++)
            {
                bool blue = b == 1; if ((blue ? _incoming.Length : _outgoing.Length) == 0) continue;
                var p = Flow(_random.RandfRange(.10f, 1), blue) + Vector2.FromAngle(_random.RandfRange(0, Mathf.Tau)) * _random.RandfRange(0, 19) * _unit;
                Emit(p, new Vector2(_random.RandfRange(-22, 22), _random.RandfRange(-45, -8)) * _unit, _random.Randf() < .11f);
            }
        }
        foreach (var silk in _silks) UpdateSilk(silk);
        KarenVfxComposition.SetCueStrength(this, Alpha);
        QueueRedraw(); _light.QueueRedraw(); _ornaments.QueueRedraw(); _front.QueueRedraw();
    }
    private Vector2 OrnamentPoint(bool blue)
    {
        if (_exchangeAt < 0)
        {
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp(_age / .42f, 0, 1));
            return (blue ? _pile : _hand).Lerp(blue ? _right : _left, t) + Vector2.Up * Mathf.Sin(t * Mathf.Pi) * 65 * _unit;
        }
        float t2 = Mathf.SmoothStep(0, 1, Travel), s = blue ? 1 - t2 : t2;
        var stage = _left.Lerp(_right, s) + Vector2.Up * Mathf.Sin(s * Mathf.Tau) * (blue ? -75 : 75) * _unit;
        // One passes behind the other. Keep the jewels away from the optical meeting
        // point so their silhouettes and the burst are all readable at game scale.
        float passing = Mathf.Exp(-Mathf.Pow((Travel - .50f) / .19f, 2));
        stage += Vector2.Up * (blue ? 85 : -85) * passing * _unit;
        return stage.Lerp(blue ? _hand : _pile, Arrival);
    }
    private Vector2 Flow(float u, bool blue)
    {
        var from = blue ? _pile : _hand; var head = OrnamentPoint(blue);
        u *= Mathf.SmoothStep(0, 1, Mathf.Clamp((_age - .08f) / .40f, 0, 1));
        if (u < .60f)
            return from.BezierInterpolate(from + new Vector2(blue ? 5 : 130, blue ? -250 : -180) * _unit,
                _focus + new Vector2(blue ? -175 : -95, blue ? -100 : 130) * _unit, _focus, u / .60f);
        var tangent = (head - _focus).Normalized();
        return _focus.BezierInterpolate(_focus + new Vector2(blue ? 175 : 95, blue ? 100 : -130).Normalized() * 95 * _unit,
            head - tangent.Rotated(blue ? -.65f : .65f) * 70 * _unit, head, (u - .60f) / .40f);
    }
    private void UpdateSilk(Silk s)
    {
        float active = (s.Blue ? _incoming.Length : _outgoing.Length) > 0 ? Alpha : 0;
        s.Node.Visible = active > .001f; if (!s.Node.Visible) return;
        for (int j = 0; j <= Segments; j++)
        {
            float u = j / (float)Segments; var p = Flow(u, s.Blue);
            var tangent = (Flow(Mathf.Min(1, u + .003f), s.Blue) - Flow(Mathf.Max(0, u - .003f), s.Blue)).Normalized();
            if (tangent.LengthSquared() < .1f) tangent = Vector2.Up;
            var normal = tangent.Orthogonal();
            p += normal * Mathf.Sin(u * 11 - _age * 4.2f + s.Strand * 2.2f) * s.Strand * 12 * _unit * Mathf.Sin(u * Mathf.Pi);
            float half = (5 + 66 * Mathf.Pow(Mathf.Max(0, Mathf.Sin(u * Mathf.Pi)), .65f)) * (s.Strand == 0 ? 1 : .48f) * _unit
                * (.69f + .31f * Mathf.Sin(u * 12 - _age * 3 + s.Strand));
            half *= 1 - .65f * Mathf.Exp(-Mathf.Pow((u - .60f) / .10f, 2));
            s.Vertices[j * 2] = new(p.X + normal.X * half, p.Y + normal.Y * half, 0);
            s.Vertices[j * 2 + 1] = new(p.X - normal.X * half, p.Y - normal.Y * half, 0);
        }
        s.Material.SetShaderParameter("age", _age); s.Material.SetShaderParameter("strength", active * (s.Strand == 0 ? .84f : .35f));
        s.Material.SetShaderParameter("erosion", Mathf.Clamp((_age - FadeAt) / .60f, 0, 1));
        s.Arrays[(int)Mesh.ArrayType.Vertex] = s.Vertices; s.Mesh.ClearSurfaces(); s.Mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, s.Arrays);
    }
    public override void _Draw() => Glow(this, _focus, new Vector2(450, 330) * _unit, new Color(.10f, .025f, .08f), Alpha * .22f);
    private void DrawLight()
    {
        float a = Alpha;
        for (int b = 0; b < 2; b++)
        {
            bool blue = b == 1; if ((blue ? _incoming.Length : _outgoing.Length) == 0) continue; var color = blue ? Blue : Red;
            Glow(_light, OrnamentPoint(blue), Vector2.One * 90 * _unit, color, a * .25f);
            for (int j = 1; j < 36; j++) _light.DrawLine(Flow(j / 36f, blue), Flow((j + 1) / 36f, blue), color with { A = a * .028f }, 45 * _unit, true);
            for (int thread = 0; thread < 2; thread++)
            {
                var last = Flow(.03f, blue);
                for (int i = 1; i <= 48; i++)
                {
                    float u = .03f + i / 48f * .94f;
                    var p = Flow(u, blue) + new Vector2(Mathf.Sin(u * 9 + thread * 2 + _age * 2), Mathf.Cos(u * 8 + _age * 2))
                        * (thread == 0 ? 17 : 29) * _unit * Mathf.Sin(u * Mathf.Pi);
                    float fade = Mathf.Sin(u * Mathf.Pi) * (.35f + .25f * Mathf.Sin(u * 19 - _age * 4 + thread));
                    _light.DrawLine(last, p, Gold with { A = a * fade }, .75f * _unit, true); last = p;
                }
            }
        }
        Glow(_light, _focus, Vector2.One * (80 + 115 * CrossPulse) * _unit, Gold, CrossPulse * .56f);
    }
    private void DrawOrnaments()
    {
        if (_atlas == null) return;
        for (int b = 0; b < 2; b++)
        {
            bool blue = b == 1; if ((blue ? _incoming.Length : _outgoing.Length) == 0) continue; var p = OrnamentPoint(blue);
            float size = (1 - Arrival * .75f) * (.88f + .12f * Mathf.Sin(Travel * Mathf.Pi));
            float turn = (blue ? .12f : -.16f) + Mathf.Sin(Travel * Mathf.Tau) * .14f;
            float yaw = 1 - .16f * Mathf.Sin(Travel * Mathf.Pi);
            _ornaments.DrawSetTransform(p, turn, new Vector2(yaw, 1) * size * _unit);
            var atlasSize = _atlas.GetSize(); float cell = atlasSize.X / 2;
            _ornaments.DrawTextureRectRegion(_atlas, new Rect2(-93, -93, 186, 186), new Rect2(blue ? cell : 0, 0, cell, atlasSize.Y), new Color(1, 1, 1, Alpha * (1 - Arrival * .55f)));
            _ornaments.DrawSetTransform(Vector2.Zero);
        }
    }
    private void Emit(Vector2 p, Vector2 velocity, bool glint)
    {
        if (_dust.Count < 180) _dust.Add(new Dust(p, velocity, _random.RandfRange(.30f, .88f), _random.RandfRange(glint ? 2 : .5f, glint ? 3.7f : 1.4f) * _unit, glint));
    }
    private void DrawDust()
    {
        for (int b = 0; b < 2; b++)
        {
            bool blue = b == 1; if ((blue ? _incoming.Length : _outgoing.Length) == 0) continue;
            Glint(_front, OrnamentPoint(blue) + new Vector2(-28, -24) * _unit, 10 * _unit,
                Alpha * (.2f + .35f * Mathf.Pow(Mathf.Max(0, Mathf.Sin(_age * 6 + (blue ? 2 : 0))), 8)) * (1 - Arrival));
        }
        foreach (var d in _dust)
        {
            float fade = Mathf.Sin(Mathf.Pow(d.Age / d.Life, .7f) * Mathf.Pi) * Alpha;
            if (d.Glint) Glint(_front, d.Position, d.Radius * 3, fade * .65f);
            else { Glow(_front, d.Position, Vector2.One * d.Radius * 4, Gold, fade * .17f); _front.DrawCircle(d.Position, d.Radius * .6f, Ivory with { A = fade * .8f }); }
        }
        Glint(_front, _focus, (15 + CrossPulse * 36) * _unit, CrossPulse * .9f);
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
    private void Sound(string file, float volume, float pitch)
    {
        if (!LocalContext.IsMe(_owner)) return; var sound = KarenAudioManager.PlaySfx(file, volume, pitch / _speed); if (sound != null) _sounds.Add(sound);
    }
    public override void _ExitTree()
    {
        KarenVfxComposition.RemoveCue(this);
        foreach (var sound in _sounds) if (GodotObject.IsInstanceValid(sound)) { sound.Stop(); sound.QueueFree(); }
        foreach (var s in _silks) s.Mesh.Dispose();
        _silks.Clear(); _dust.Clear(); _sounds.Clear(); _outgoing = []; _incoming = []; _random.Dispose();
    }
}
