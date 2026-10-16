using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>One light per power stack follows a soft contour directly on the native energy icon rim.</summary>
public partial class NKarenWaitingStageVfx : Node2D
{
    private sealed class Delivery(int gain, float angle)
    {
        public readonly int Gain = gain;
        public readonly float Angle = angle;
        public float Age;
        public bool Arrived, Started;
        public CpuParticles2D Trail = null!;
    }

    private KarenStageIsWaitingPower _power = null!;
    private NCombatRoom _room = null!;
    private NCreature _view = null!;
    private NEnergyCounter? _counter;
    private readonly Node2D _hud = new() { Name = "KarenWaitingStageHud" };
    private readonly ColorRect _ring = new() { Name = "WaitingEnergyOrbit", MouseFilter = Control.MouseFilterEnum.Ignore };
    private ShaderMaterial _ringMaterial = null!;
    private sealed class PatrolLight
    {
        public Sprite2D Head = null!;
        public ShaderMaterial Material = null!;
        public Line2D Tail = null!, Glow = null!;
        public readonly Vector2[] Points = new Vector2[16];
        public float Offset, FromOffset;
        public int CreatorLane;
    }
    private readonly List<PatrolLight> _lights = new();
    private Shader _seedShader = null!;
    private int _formationCount;
    private float _layoutAge = 1;
    public IEnumerable<Sprite2D> PatrolHeads => _lights.Select(light => light.Head);
    public int PowerLayers => Math.Max(0, (int)_power.Amount);
    public float PatrolScale => Mathf.Lerp(1.15f, .72f, _opening);
    public float PatrolBrightness => Mathf.Lerp(1f, .28f, _opening);
    private CpuParticles2D _glints = null!;
    private float _glintRadius = -1;
    private readonly List<Delivery> _deliveries = new();
    private readonly List<(CpuParticles2D Node, float Until)> _bursts = new();
    private readonly List<AudioStreamPlayer> _sounds = new();
    private static GradientTexture2D? _soft;
    private static readonly Color PromiseRose = new("#f08aa3"), WaitingMauve = new("#bda2b8"),
        SpotlightWhite = new("#e2f1ff"), CoreWhite = new("#fff6fa");
    private Color CurrentTint => PromiseRose.Lerp(WaitingMauve, _opening);
    private Vector2 _center;
    private float _age, _castAge = 4, _lastSound = -10, _unit = 1;
    private float _buildAge = 4, _orbitPhase, _lastBuildBurst = -10;
    private bool _formationFinalized = true;
    private bool _pileKnown;
    private float _opening, _openingFrom, _stateAge;
    public bool PileIsEmpty { get; private set; }
    public int PileCount { get; private set; }
    public float Opening => _opening;
    public int StateChanges { get; private set; }
    public int CastStarts { get; private set; }
    public int Confirmations { get; private set; }
    public int DeliveredEnergy { get; private set; }
    public int ActiveDeliveries => _deliveries.Count;
    public Node2D Hud => _hud;
    public NEnergyCounter? EnergyCounter => GodotObject.IsInstanceValid(_counter) ? _counter : null;
    public Vector2 EnergyPointCanvas { get; private set; }
    public ColorRect RingVisual => _ring;
    public float RingRadius => 62f;
    public float BuildProgress => Ease(P(_buildAge, .20f, 1.10f));
    public int CreationStarts { get; private set; }
    public float OrbitPhase => _orbitPhase;

    public float Unit => _unit;
    public float CastLight => Cast;
    public float TriggerLight => Pulse;
    public float StageStrength { get; private set; }
    public bool UiVisible => GodotObject.IsInstanceValid(_hud) && _hud.IsVisibleInTree();
    private const float FeedbackDelay = .16f;
    private static bool Enabled => !NonInteractiveMode.IsActive && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant;
    private float Speed => SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? 1.6f : 1f;
    private float Cast => Window(_castAge, 0, 1.35f, .10f, .35f);
    private float Pulse => _deliveries.Count == 0 ? 0 : _deliveries.Max(d => Window(d.Age, FeedbackDelay, .58f, .07f, .25f));

    public static NKarenWaitingStageVfx? Ensure(KarenStageIsWaitingPower power, bool cast = false)
    {
        var room = NCombatRoom.Instance;
        var view = room?.GetCreatureNode(power.Owner);
        if (!Enabled || room == null || view == null || power.Owner.Player is not { } player || !LocalContext.IsMe(player)
            || power.Amount <= 0 || !CombatManager.Instance.IsInProgress || power.Owner.GetPower<KarenStageIsWaitingPower>() != power) return null;
        var existing = room.CombatVfxContainer.GetChildren().OfType<NKarenWaitingStageVfx>()
            .FirstOrDefault(n => n._power == power && !n.IsQueuedForDeletion());
        if (existing != null)
        {
            existing.SyncLights(); existing.RefreshPileState();
            if (cast) existing.BeginCast();
            existing.Place(); return existing;
        }
        var node = new NKarenWaitingStageVfx { _power = power, _room = room, _view = view,
            _castAge = cast ? 0 : 4, _buildAge = cast ? 0 : 4, _formationFinalized = !cast,
            _formationCount = cast ? Math.Max(1, (int)power.Amount) : 0, Name = "KarenWaitingStageVfx" };
        room.CombatVfxContainer.AddChildSafely(node);
        return node;
    }

    public static void Remove(KarenStageIsWaitingPower power)
    {
        if (NCombatRoom.Instance is not { } room) return;
        foreach (var node in room.CombatVfxContainer.GetChildren().OfType<NKarenWaitingStageVfx>().Where(n => n._power == power)) node.Stop();
    }

    public override void _Ready()
    {
        _soft ??= new GradientTexture2D { Width = 32, Height = 32, Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new(.5f, .5f), FillTo = new(1, .5f), Gradient = new Gradient {
                Offsets = [0, .2f, .6f, 1], Colors = [Colors.White, new(1, 1, 1, .8f), new(1, 1, 1, .12f), Colors.Transparent] } };
        AddChild(_hud); _hud.Hide();
        _hud.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        _ringMaterial = new ShaderMaterial { Shader = KarenResourceLoader.Load<Shader>(
            "res://shaders/karen_waiting_energy_orbit.gdshader", nameof(NKarenWaitingStageVfx)) };
        _ring.Material = _ringMaterial; _hud.AddChild(_ring);
        _ringMaterial.SetShaderParameter("core_color", CoreWhite);
        _ringMaterial.SetShaderParameter("outline_points", Outline);
        _ringMaterial.SetShaderParameter("outline_perimeter", OutlinePerimeter);
        _ringMaterial.SetShaderParameter("seed_count", Math.Max(1, _formationCount));
        _seedShader = KarenResourceLoader.Load<Shader>("res://shaders/karen_waiting_light_seed.gdshader", nameof(NKarenWaitingStageVfx));
        SyncLights();
        _hud.Draw += DrawDeliveries;
        _glints = Particles(18, 2.4f, false);
        _glints.EmissionShape = CpuParticles2D.EmissionShapeEnum.Points;
        _glints.InitialVelocityMin = .6f; _glints.InitialVelocityMax = 2.5f;
        _glints.ScaleAmountMin = .12f; _glints.ScaleAmountMax = .22f; _glints.Preprocess = 0; _glints.Emitting = false;
        _glints.Modulate = new(1, 1, 1, 0); _hud.AddChild(_glints);
        RefreshPileState(snap: true); Place();
        // ShaderMaterial getters otherwise return an unset Variant before the
        // first process tick; initialize the actual restored pose immediately.
        _ringMaterial.SetShaderParameter("build", BuildProgress);
        _ringMaterial.SetShaderParameter("opening", _opening);
        _ringMaterial.SetShaderParameter("phase", _orbitPhase);
        _ringMaterial.SetShaderParameter("light_color", CurrentTint);
        RenderingServer.FramePreDraw += BeforeDraw;
        if (_castAge == 0) BeginCast();
    }

    // Read the effective pile, including Void. Infinite Reproduction's intermediate
    // zero suppresses the empty hook, so it must not change the resting pose either.
    public void RefreshPileState(bool snap = false)
    {
        var player = _power.Owner.Player;
        if (player?.PlayerCombatState == null) return;
        PileCount = PromisePileManager.GetCount(player);
        if (PileCount == 0 && PromisePileManager.IsInMode(player, PromisePileMode.InfiniteReinforcement)) return;
        bool empty = PileCount == 0;
        if (!_pileKnown || snap)
        {
            PileIsEmpty = empty; _opening = _openingFrom = empty ? 1 : 0;
            _stateAge = 1; _pileKnown = true; return;
        }
        if (empty == PileIsEmpty) return;
        PileIsEmpty = empty; StateChanges++; _openingFrom = _opening; _stateAge = 0;
    }

    private void BeginCast()
    {
        _castAge = 0; CastStarts++;
        if (_buildAge < 1.10f && CreationStarts == 0) CreationStarts++;
        if (!UiVisible) return;
        Sound(KarenSfx.WaitingCurtainRise, .28f, 1f);
    }

    public void ConfirmEnergy(int actual)
    {
        if (actual <= 0 || !Enabled || !IsInsideTree() || IsQueuedForDeletion()) return;
        Confirmations++; DeliveredEnergy += actual;
        if (_deliveries.Count >= 8) { RetireTrail(_deliveries[0]); _deliveries.RemoveAt(0); }
        var delivery = new Delivery(actual, _orbitPhase + Confirmations * 2.39996f) { Trail = Particles(8, .18f, false) };
        delivery.Trail.Emitting = false; delivery.Trail.Modulate = SpotlightWhite;
        delivery.Trail.Spread = 100; delivery.Trail.InitialVelocityMin = 1; delivery.Trail.InitialVelocityMax = 4;
        delivery.Trail.ScaleAmountMin = .02f; delivery.Trail.ScaleAmountMax = .045f;
        _hud.AddChild(delivery.Trail); delivery.Trail.Visible = false; _deliveries.Add(delivery);
    }

    public override void _Process(double delta)
    {
        if (!Enabled || !GodotObject.IsInstanceValid(_room) || !_room.IsInsideTree() || NCombatRoom.Instance != _room
            || !GodotObject.IsInstanceValid(_view) || !_view.IsInsideTree() || _view.IsQueuedForDeletion()
            || !GodotObject.IsInstanceValid(_hud) || _hud.IsQueuedForDeletion()
            || !CombatManager.Instance.IsInProgress || _power.Owner.IsDead || _power.Amount <= 0
            || !LocalContext.IsMe(_power.Owner.Player!) || _power.Owner.GetPower<KarenStageIsWaitingPower>() != _power) { Stop(); return; }
        RefreshPileState();
        float dt = (float)delta * Speed; _age += dt; _castAge += dt; _stateAge += dt; _buildAge += dt;
        SyncLights(); _layoutAge += dt;
        if (!_formationFinalized && _buildAge >= 1.10f)
        {
            _orbitPhase = Mathf.Tau / Math.Max(1, _formationCount);
            for (int i = 0; i < _lights.Count; i++)
                _lights[i].Offset = _lights[i].FromOffset = CreationPhase(_lights[i], i, 1) - _orbitPhase;
            _formationFinalized = true; _layoutAge = 0;
        }
        if (_formationFinalized) _orbitPhase += dt * (.40f + 1.6f * Cast + 1.8f * Pulse);
        _opening = Mathf.Lerp(_openingFrom, PileIsEmpty ? 1 : 0, Ease(P(_stateAge, 0, .60f)));
        Place();
        if (UiVisible && _buildAge < 1.10f && _buildAge - _lastBuildBurst >= .10f)
        {
            _lastBuildBurst = _buildAge;
            foreach (var light in _lights) Burst(light.Head.Position, 2, 3);
        }
        for (int i = _deliveries.Count - 1; i >= 0; i--)
        {
            var delivery = _deliveries[i]; delivery.Age += dt;
            if (!delivery.Started && delivery.Age >= FeedbackDelay)
            {
                delivery.Started = true;
                if (UiVisible && _age - _lastSound > .18f) { Sound(KarenSfx.WaitingCurtainPulse, .22f, 1f); _lastSound = _age; }
            }
            delivery.Trail.Visible = UiVisible && delivery.Age >= .20f;
            delivery.Trail.Position = Path(delivery, P(delivery.Age, .20f, .68f));
            delivery.Trail.Emitting = UiVisible && delivery.Age >= .20f && delivery.Age < .68f;
            delivery.Trail.SpeedScale = Speed;
            if (!delivery.Arrived && delivery.Age >= .68f)
            {
                delivery.Arrived = true;
                if (UiVisible)
                {
                    Burst(OrbitPoint(delivery.Angle, 58), 4, 9);
                    if (_age - _lastSound > .18f) { Sound(KarenSfx.WaitingEnergyArrive, .16f, 1f); _lastSound = _age; }
                }
            }
            if (delivery.Age >= 1.05f) { RetireTrail(delivery); _deliveries.RemoveAt(i); }
        }
        for (int i = _bursts.Count - 1; i >= 0; i--)
        {
            var burst = _bursts[i];
            if (_age >= burst.Until) { burst.Node.QueueFree(); _bursts.RemoveAt(i); }
            else burst.Node.SpeedScale = Speed;
        }
        _sounds.RemoveAll(s => !GodotObject.IsInstanceValid(s));
        float reveal = Ease(P(_age, 0, .36f)), eventLight = Mathf.Max(Cast * .8f, Pulse);
        StageStrength = reveal * (Mathf.Lerp(.40f, .22f, _opening) + .20f * eventLight);
        _ringMaterial.SetShaderParameter("strength", StageStrength);
        _ringMaterial.SetShaderParameter("clock", _age);
        _ringMaterial.SetShaderParameter("build", BuildProgress);
        _ringMaterial.SetShaderParameter("opening", _opening);
        _ringMaterial.SetShaderParameter("phase", _formationFinalized ? _orbitPhase : Mathf.Pi * BuildProgress);
        _ringMaterial.SetShaderParameter("light_color", CurrentTint.Lerp(SpotlightWhite, eventLight * .55f));
        _glints.Emitting = UiVisible && BuildProgress > .98f;
        _glints.Modulate = new Color(CurrentTint, reveal * (Mathf.Lerp(.72f, .45f, _opening) + .20f * eventLight));
        _glints.SpeedScale = Speed;
        if (!UiVisible) foreach (var sound in _sounds) if (GodotObject.IsInstanceValid(sound)) sound.Stop();
        _hud.QueueRedraw();
    }

    private void BeforeDraw()
    {
        if (IsInsideTree() && !IsQueuedForDeletion() && GodotObject.IsInstanceValid(_hud) && !_hud.IsQueuedForDeletion())
        {
            // Amount hooks can resume after _Process; reconcile before drawing the next frame.
            SyncLights(); Place();
        }
    }

    private void Place()
    {
        // Sit directly on the icon outline above its face, while Label stays in front.
        // All lights and tails inherit the native counter transform.
        var counter = _room.Ui.EnergyCounterContainer.GetChildren().OfType<NEnergyCounter>()
            .FirstOrDefault(c => c.IsInsideTree() && !c.IsQueuedForDeletion());
        if (counter == null) { _counter = null; _hud.Hide(); return; }
        _counter = counter;
        if (_hud.GetParent() != counter) { _hud.Reparent(counter, false); counter.MoveChild(_hud, counter.GetNode("Label").GetIndex()); }
        _hud.Transform = Transform2D.Identity;
        _hud.Visible = counter.IsVisibleInTree();
        _center = counter.Size * .5f; _unit = Mathf.Clamp(Mathf.Min(counter.Size.X, counter.Size.Y) / 128f, .4f, 2.5f);
        EnergyPointCanvas = counter.GetGlobalTransformWithCanvas() * _center;
        _ring.Position = _center - new Vector2(120, 120) * _unit;
        _ring.Size = new Vector2(240, 240) * _unit;
        for (int i = 0; i < _lights.Count; i++)
        {
            var light = _lights[i];
            light.Offset = Mathf.LerpAngle(light.FromOffset, i * Mathf.Tau / _lights.Count, Ease(P(_layoutAge, 0, .35f)));
            float phase = _formationFinalized ? _orbitPhase + light.Offset : CreationPhase(light, i, BuildProgress);
            light.Head.Position = _buildAge < .20f ? ArrivalPoint(i, _buildAge) : OrbitPoint(phase, RingRadius);
            light.Head.Scale = Vector2.One * (PatrolScale + .12f * Cast) * _unit;
            light.Head.Modulate = Colors.White;
            light.Head.Visible = true;
            light.Material.SetShaderParameter("light_color", CurrentTint.Lerp(SpotlightWhite, Mathf.Max(Cast * .35f, Pulse * .70f)));
            light.Material.SetShaderParameter("strength", PatrolBrightness + .18f * Cast + .45f * Pulse);
            float trail = Mathf.Min(.42f, Mathf.Tau / Math.Max(1, _lights.Count) * .62f);
            float floor = !_formationFinalized && light.CreatorLane >= 0 ? light.CreatorLane * Mathf.Tau / Math.Max(1, _formationCount) : float.NegativeInfinity;
            for (int j = 0; j < light.Points.Length; j++)
            {
                float at = Mathf.Max(floor, phase - trail * (1 - j / (float)(light.Points.Length - 1)));
                light.Points[j] = OrbitPoint(at, RingRadius);
            }
            light.Tail.Points = light.Glow.Points = light.Points;
            light.Tail.Width = 1.7f * _unit;
            light.Glow.Width = 5f * _unit;
            light.Tail.Modulate = new Color(CurrentTint.Lerp(SpotlightWhite, Pulse * .7f), PatrolBrightness * .72f + Pulse * .25f);
            light.Glow.Modulate = new Color(CurrentTint, PatrolBrightness * .12f + Pulse * .06f);
            light.Tail.Visible = light.Glow.Visible = _buildAge >= .20f;
        }
        _glints.Position = _center; _glints.Scale = Vector2.One * _unit;
        if (Mathf.Abs(_glintRadius - RingRadius) > .05f)
        {
            _glintRadius = RingRadius;
            _glints.EmissionPoints = Enumerable.Range(0, 24).Select(i =>
                HexOffset(i * Mathf.Tau / 24, RingRadius + 4 * Mathf.Sin(i * 2.39996f))).ToArray();
        }
    }

    private void SyncLights()
    {
        int count = PowerLayers;
        if (_lights.Count == count) return;
        foreach (var light in _lights) light.FromOffset = light.Offset;
        while (_lights.Count > count)
        {
            var light = _lights[^1]; light.Head.Hide(); light.Tail.Hide(); light.Glow.Hide();
            light.Head.QueueFree(); light.Tail.QueueFree(); light.Glow.QueueFree(); _lights.RemoveAt(_lights.Count - 1);
        }
        while (_lights.Count < count)
        {
            int index = _lights.Count;
            var material = new ShaderMaterial { Shader = _seedShader };
            material.SetShaderParameter("core_color", CoreWhite);
            Line2D NewTail(float alpha)
            {
                var tail = new Line2D { Antialiased = true, JointMode = Line2D.LineJointMode.Round,
                    BeginCapMode = Line2D.LineCapMode.Round, EndCapMode = Line2D.LineCapMode.Round,
                    Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add },
                    Gradient = new Gradient { Offsets = [0, .65f, 1], Colors = [Colors.Transparent, new(Colors.White, alpha * .45f), new(Colors.White, alpha)] } };
                _hud.AddChild(tail); return tail;
            }
            var light = new PatrolLight { Material = material, Tail = NewTail(1), Glow = NewTail(1),
                Head = new Sprite2D { Name = "WaitingStackLight" + index, Texture = _soft, Material = material },
                Offset = index * Mathf.Tau / Math.Max(1, count), FromOffset = index * Mathf.Tau / Math.Max(1, count),
                CreatorLane = !_formationFinalized && index < _formationCount ? index : -1 };
            _hud.AddChild(light.Head); _lights.Add(light);
        }
        _layoutAge = 0;
    }
    private float CreationPhase(PatrolLight light, int index, float progress)
    {
        float sector = Mathf.Tau / Math.Max(1, _formationCount);
        return light.CreatorLane >= 0 ? (light.CreatorLane + progress) * sector
            : ((index % Math.Max(1, _formationCount)) + progress * (index + 1f) / Math.Max(1, _lights.Count)) * sector;
    }
    // Tangent-continuous quadratic fillets sampled around the actual icon rim.
    // The very same outline is passed to the shader and used by every light/tail.
    private static readonly Vector2[] Outline = BuildOutline();
    private static readonly float[] OutlineLengths = Enumerable.Range(0, Outline.Length)
        .Select(i => Outline[i].DistanceTo(Outline[(i + 1) % Outline.Length])).ToArray();
    private static readonly float OutlinePerimeter = OutlineLengths.Sum();
    private static Vector2[] BuildOutline()
    {
        Vector2[] vertices = [new(1, 0), new(.5f, .97f), new(-.5f, .97f), new(-1, 0), new(-.5f, -.97f), new(.5f, -.97f)];
        var points = new List<Vector2>();
        for (int i = 0; i < 6; i++)
        {
            var vertex = vertices[i];
            var a = vertex + (vertices[(i + 5) % 6] - vertex).Normalized() * .16f;
            var b = vertex + (vertices[(i + 1) % 6] - vertex).Normalized() * .16f;
            for (int j = 0; j <= 6; j++)
            {
                float t = j / 6f;
                points.Add(a * ((1 - t) * (1 - t)) + vertex * (2 * t * (1 - t)) + b * (t * t));
            }
        }
        return Enumerable.Range(0, points.Count).Select(i => points[(i + 3) % points.Count]).ToArray();
    }
    private static Vector2 HexOffset(float phase, float radius)
    {
        float remaining = Mathf.PosMod(phase, Mathf.Tau) / Mathf.Tau * OutlinePerimeter;
        for (int i = 0; i < Outline.Length; i++)
        {
            float length = OutlineLengths[i];
            if (remaining <= length || i == Outline.Length - 1)
                return Outline[i].Lerp(Outline[(i + 1) % Outline.Length], remaining / length) * radius;
            remaining -= length;
        }
        return Outline[0] * radius;
    }
    private Vector2 OrbitPoint(float phase, float radius) => _center + HexOffset(phase, radius) * _unit;
    private Vector2 ArrivalPoint(int index, float age)
    {
        float phase = index < _lights.Count && _lights[index].CreatorLane >= 0
            ? _lights[index].CreatorLane * Mathf.Tau / Math.Max(1, _formationCount)
            : index * Mathf.Tau / Math.Max(1, _lights.Count);
        Vector2 offset = HexOffset(phase, RingRadius), outward = offset.Normalized();
        Vector2 tangent = new(outward.Y, -outward.X);
        float u = Ease(P(age, 0, .20f));
        var start = _center + (outward * 132 + tangent * 18) * _unit;
        return start.Lerp(_center + offset * _unit, u) + tangent * (10 * Mathf.Sin(u * Mathf.Pi)) * _unit;
    }
    private static float Ease(float u) => u * u * (3 - 2 * u);
    private Vector2 Path(Delivery delivery, float u)
        => OrbitPoint(delivery.Angle + .16f * u, Mathf.Lerp(RingRadius + 2, 48, Ease(u)));

    private void DrawDeliveries()
    {
        if (!UiVisible || _soft == null) return;
        if (_buildAge < .20f)
        {
            for (int i = 0; i < _lights.Count; i++)
            for (int j = 0; j < 8; j++)
            {
                float a = Mathf.Max(0, _buildAge - j * .012f), b = Mathf.Max(0, a - .012f);
                Vector2 from = ArrivalPoint(i, a), to = ArrivalPoint(i, b);
                float fade = (1 - j / 8f) * .65f;
                _hud.DrawLine(from, to, new Color(PromiseRose, fade * .18f), 5 * _unit, true);
                _hud.DrawLine(from, to, new Color(SpotlightWhite, fade), 1.3f * _unit, true);
            }
        }
        foreach (var delivery in _deliveries)
        {
            if (delivery.Age < .20f) continue;
            float head = P(delivery.Age, .20f, .68f), fade = 1 - P(delivery.Age, .68f, .95f);
            for (int j = 0; j < 6; j++)
            {
                Vector2 at = Path(delivery, Mathf.Max(0, head - j * .022f));
                float size = (j == 0 ? Mathf.Min(7, 4 + delivery.Gain) : 3) * _unit;
                _hud.DrawTextureRect(_soft, new Rect2(at - Vector2.One * size / 2, Vector2.One * size), false,
                    new Color(SpotlightWhite, fade * (1 - j / 7f) * .32f));
            }
        }
    }
    private CpuParticles2D Particles(int amount, float life, bool oneShot)
    {
        return new CpuParticles2D { Amount = amount, Lifetime = life, OneShot = oneShot,
            Explosiveness = oneShot ? 1 : 0, Emitting = !oneShot, Texture = _soft,
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add },
            Direction = Vector2.Up, Spread = 22, Gravity = Vector2.Zero, LocalCoords = true,
            InitialVelocityMin = 2, InitialVelocityMax = 8, ScaleAmountMin = .02f, ScaleAmountMax = .06f,
            ColorRamp = new Gradient { Offsets = [0, .15f, .7f, 1],
                Colors = [new(Colors.White, 0), Colors.White, new(Colors.White, .75f), new(Colors.White, 0)] },
            SpeedScale = Speed, Randomness = .6f, LifetimeRandomness = .3f };
    }
    private void Burst(Vector2 at, int count, float velocity)
    {
        if (_bursts.Count >= 12) { _bursts[0].Node.QueueFree(); _bursts.RemoveAt(0); }
        var particles = Particles(count, .45f, true);
        particles.ScaleAmountMin = .09f; particles.ScaleAmountMax = .18f;
        particles.Position = at; particles.Scale = Vector2.One * _unit;
        particles.Modulate = CurrentTint.Lerp(SpotlightWhite, Pulse * .70f);
        particles.InitialVelocityMin = velocity * .4f; particles.InitialVelocityMax = velocity;
        particles.Spread = 160; _hud.AddChild(particles); particles.Emitting = true;
        _bursts.Add((particles, _age + .5f));
    }
    private static void RetireTrail(Delivery delivery) { delivery.Trail.Hide(); delivery.Trail.QueueFree(); }
    private void Sound(string file, float volume, float pitch)
    {
        _sounds.RemoveAll(s => !GodotObject.IsInstanceValid(s));
        if (_sounds.Count >= 4) { _sounds[0].Stop(); _sounds[0].QueueFree(); _sounds.RemoveAt(0); }
        var sound = KarenAudioManager.PlaySfx(file, volume, pitch * Speed);
        if (sound != null) _sounds.Add(sound);
    }
    private static float P(float x, float a, float b) => Mathf.Clamp((x - a) / (b - a), 0, 1);
    private static float Window(float x, float a, float b, float rise, float fall) => P(x, a, a + rise) * (1 - P(x, b - fall, b));
    public void Stop()
    {
        Hide(); if (GodotObject.IsInstanceValid(_hud)) _hud.Hide(); QueueFree();
    }
    public override void _ExitTree()
    {
        RenderingServer.FramePreDraw -= BeforeDraw;
        if (GodotObject.IsInstanceValid(_hud)) { _hud.Hide(); _hud.QueueFree(); }
        foreach (var sound in _sounds) if (GodotObject.IsInstanceValid(sound)) { sound.Stop(); sound.QueueFree(); }
        _sounds.Clear(); _deliveries.Clear(); _bursts.Clear();
    }
}
