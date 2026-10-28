using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Finite Aquarium cues. No ambient light or particles; native cards retain layout and input ownership.</summary>
public partial class NKarenAquariumVfx : Node2D
{
    // Shared visual resources are prepared during the run loading screen as well as on demand.
    internal static void PrepareResources()
    {
        _bubble ??= new GradientTexture2D { Width = 24, Height = 24, Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new(.5f, .5f), FillTo = new(1, .5f), Gradient = new Gradient {
                Offsets = [0, .46f, .64f, .79f, 1], Colors = [new(1,1,1,.04f), new(1,1,1,.08f), Colors.White, new(1,1,1,.1f), Colors.Transparent] } };
    }
    internal static Godot.Resource?[] PreparedResources => [_bubble];

    private sealed class CardLight(CardModel card, float born)
    {
        public readonly CardModel Card = card;
        public readonly float Born = born;
    }
    private sealed class Ripple(Vector2 point, float born)
    {
        public readonly Vector2 Point = point;
        public readonly float Born = born;
    }
    private KarenAquariumPower _power = null!;
    private NCombatRoom _room = null!;
    private NCreature _view = null!;
    private readonly Node2D _back = new() { Name = "AquariumBehindActor" };
    private readonly Node2D _hud = new() { Name = "AquariumHandLight", ZIndex = 50 };
    private ColorRect _water = null!, _selectionWater = null!;
    private ShaderMaterial _waterMaterial = null!, _selectionMaterial = null!;
    private readonly List<(ColorRect Node, ShaderMaterial Material)> _jellies = new();
    private readonly List<CardLight> _drawn = new();
    private readonly List<Ripple> _ripples = new();
    private readonly Dictionary<CardModel, Vector2> _handPoints = new();
    private readonly List<(CpuParticles2D Node, float End)> _bursts = new();
    private readonly List<AudioStreamPlayer> _sounds = new();
    private static GradientTexture2D? _bubble;
    private Vector2 _foot, _screen;
    private float _unit = 1, _age, _castAge = 5, _turnAge = 5, _moveAge = 5, _selectionAge = 5;
    private bool _turnOwned;
    public int CastStarts { get; private set; }
    public int TurnStarts { get; private set; }
    public int ActualDrawn { get; private set; }
    public int ActualMoved { get; private set; }
    public bool Selecting { get; private set; }
    public Node2D BackLayer => _back;
    public Node2D HandLayer => _hud;
    public Vector2 FootCanvas => _foot;
    private static bool Enabled => !NonInteractiveMode.IsActive && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant;
    private bool Alive => GodotObject.IsInstanceValid(this) && IsInsideTree() && !IsQueuedForDeletion();
    private float Speed => SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? 1.65f : 1f;
    private static float Pulse(float age, float duration) => age <= 0 || age >= duration ? 0 : Mathf.Sin(Mathf.Pi * age / duration);
    // Retain only the selection observer while a native choice is pending; all visible cues still expire.
    public bool Showing => Alive && (_back.Visible || _hud.Visible || Pulse(_castAge, 1.25f) > 0 || Pulse(_turnAge, 1f) > 0);

    public static NKarenAquariumVfx? Ensure(KarenAquariumPower power, bool cast = false)
    {
        var room = NCombatRoom.Instance;
        var view = room?.GetCreatureNode(power.Owner);
        if (!Enabled || room == null || view == null || power.Owner.Player is not { } player || !LocalContext.IsMe(player)
            || power.Amount <= 0 || !CombatManager.Instance.IsInProgress || power.Owner.GetPower<KarenAquariumPower>() != power) return null;
        var existing = room.CombatVfxContainer.GetChildren().OfType<NKarenAquariumVfx>()
            .FirstOrDefault(n => n._power == power && !n.IsQueuedForDeletion());
        if (existing != null) { if (cast) existing.Cast(); return existing; }
        var effect = new NKarenAquariumVfx { _power = power, _room = room, _view = view, Name = "KarenAquariumVfx" };
        room.CombatVfxContainer.AddChildSafely(effect);
        if (cast) effect.Cast();
        return effect;
    }

    public static void Remove(KarenAquariumPower power)
    {
        if (NCombatRoom.Instance is not {} room) return;
        foreach (var node in room.CombatVfxContainer.GetChildren().OfType<NKarenAquariumVfx>().Where(n => n._power == power)) node.Stop();
    }

    public override void _Ready()
    {
        PrepareResources();
        var waterShader = KarenResourceLoader.Load<Shader>("res://shaders/karen_aquarium_water.gdshader", nameof(NKarenAquariumVfx));
        var jellyShader = KarenResourceLoader.Load<Shader>("res://shaders/karen_aquarium_jelly.gdshader", nameof(NKarenAquariumVfx));
        _room.BackCombatVfxContainer.AddChildSafely(_back);
        _room.Ui.AddChildSafely(_hud);
        _waterMaterial = new ShaderMaterial { Shader = waterShader }; _water = Panel(_waterMaterial); _back.AddChild(_water);
        _selectionMaterial = new ShaderMaterial { Shader = waterShader }; _selectionWater = Panel(_selectionMaterial); _hud.AddChild(_selectionWater);
        for (int i = 0; i < 3; i++)
        {
            var material = new ShaderMaterial { Shader = jellyShader }; material.SetShaderParameter("phase", i * 2.13f);
            var node = Panel(material); _back.AddChild(node); _jellies.Add((node, material));
        }
        _hud.Draw += DrawHand; Place(); _back.Hide(); _hud.Hide();
    }
    private static ColorRect Panel(ShaderMaterial material) => new() { Material = material, MouseFilter = Control.MouseFilterEnum.Ignore };

    private void Cast()
    {
        if (!Alive) return;
        _castAge = 0; CastStarts++; Burst(_back, _foot - new Vector2(0, 18 * _unit), 14);
        Sound("karen_aquarium_open.wav", .30f);
    }
    public void BeginTurn()
    {
        if (!Alive) return;
        _turnOwned = true; _turnAge = 0; TurnStarts++; Burst(_back, _foot - new Vector2(0, 18 * _unit), 8);
    }
    public void ConfirmDraw(IEnumerable<CardModel> cards)
    {
        if (!Alive) return;
        var actual = cards.Where(c => c.Owner == _power.Owner.Player && c.Pile?.Type == PileType.Hand).Distinct().ToArray();
        ActualDrawn += actual.Length;
        foreach (var card in actual) _drawn.Add(new(card, _age));
        if (actual.Length > 0) { _turnAge = 0; Sound("karen_aquarium_drop.wav", .22f); }
    }
    public void BeginSelection()
    {
        if (!Alive) return;
        _handPoints.Clear();
        Selecting = PileType.Hand.GetPile(_power.Owner.Player!).Cards.Count > 0;
        _selectionAge = Selecting ? 0 : 5;
        CaptureHandPoints();
    }
    public void ConfirmMoves(IEnumerable<CardModel> cards)
    {
        if (!Alive) return;
        // The command returns only after native migration completes, including the Void branch.
        var actual = cards.Where(c => c.Owner == _power.Owner.Player).Distinct().ToArray();
        ActualMoved += actual.Length;
        foreach (var card in actual)
            if (_handPoints.TryGetValue(card, out var point)) { _ripples.Add(new(point, _age)); Burst(_hud, point, 7); }
        if (actual.Length > 0) { _moveAge = 0; Sound("karen_aquarium_drop.wav", .18f, .88f); }
    }
    public void EndSelection()
    {
        if (!Alive) return;
        _turnOwned = false; Selecting = false; _handPoints.Clear();
    }
    private void CaptureHandPoints()
    {
        if (!Selecting) return;
        foreach (var card in PileType.Hand.GetPile(_power.Owner.Player!).Cards)
            if (CardPoint(card) is {} point) _handPoints[card] = point;
    }
    private static Vector2? CardPoint(CardModel model)
    {
        if (model.Pile?.Type != PileType.Hand || NCard.FindOnTable(model) is not {} card
            || !GodotObject.IsInstanceValid(card) || card.Model != model || !card.IsInsideTree() || !card.IsVisibleInTree() || card.IsQueuedForDeletion()) return null;
        var portrait = card.GetNodeOrNull<Control>("%Portrait");
        return portrait == null ? null : portrait.GetGlobalTransformWithCanvas() * new Vector2(portrait.Size.X * .5f, portrait.Size.Y * .80f);
    }

    public override void _Process(double delta)
    {
        if (!Enabled || !GodotObject.IsInstanceValid(_room) || !_room.IsInsideTree() || NCombatRoom.Instance != _room
            || !GodotObject.IsInstanceValid(_view) || !_view.IsInsideTree() || _view.IsQueuedForDeletion()
            || !CombatManager.Instance.IsInProgress || _power.Owner.IsDead || _power.Amount <= 0
            || !LocalContext.IsMe(_power.Owner.Player!) || _power.Owner.GetPower<KarenAquariumPower>() != _power) { Stop(); return; }
        float dt = (float)delta * Speed; _age += dt; _castAge += dt; _turnAge += dt; _moveAge += dt;
        _selectionAge += dt;
        Place(); CaptureHandPoints();
        float accent = Mathf.Max(Pulse(_castAge, 1.45f), Pulse(_turnAge, 1.1f) * .75f);
        _waterMaterial.SetShaderParameter("clock", _age);
        _waterMaterial.SetShaderParameter("strength", accent * .65f + Pulse(_moveAge, .75f) * .16f);
        _selectionMaterial.SetShaderParameter("clock", _age * .65f);
        _selectionMaterial.SetShaderParameter("strength", Pulse(_selectionAge, .75f) * .14f);
        foreach (var (_, material) in _jellies)
        {
            material.SetShaderParameter("clock", _age);
            material.SetShaderParameter("strength", accent * .96f);
        }
        for (int i = _bursts.Count - 1; i >= 0; i--)
        {
            if (_age > _bursts[i].End) { _bursts[i].Node.QueueFree(); _bursts.RemoveAt(i); }
            else _bursts[i].Node.SpeedScale = Speed;
        }
        _drawn.RemoveAll(c => _age - c.Born > 1.4f || c.Card.Pile?.Type != PileType.Hand);
        _ripples.RemoveAll(r => _age - r.Born > .75f);
        _sounds.RemoveAll(s => !GodotObject.IsInstanceValid(s));
        _back.Visible = accent > 0 || Pulse(_moveAge, .75f) > 0 || _bursts.Any(b => b.Node.GetParent() == _back);
        _hud.Visible = Pulse(_selectionAge, .75f) > 0 || _drawn.Count > 0 || _ripples.Count > 0 || _bursts.Any(b => b.Node.GetParent() == _hud);
        if (!_turnOwned && _castAge >= 1.65f && _turnAge >= 1.4f && _moveAge >= 1.2f
            && _selectionAge >= .75f && _drawn.Count == 0 && _ripples.Count == 0 && _bursts.Count == 0 && _sounds.Count == 0) { Stop(); return; }
        KarenVfxQuality.Redraw(this); KarenVfxQuality.Redraw(_hud);
    }
    private void Place()
    {
        GlobalTransform = GetCanvasTransform().AffineInverse();
        _back.GlobalTransform = _back.GetCanvasTransform().AffineInverse();
        _hud.GlobalTransform = _hud.GetCanvasTransform().AffineInverse();
        _screen = GetViewport().GetVisibleRect().Size;
        var bounds = _view.Visuals.GetNodeOrNull<Control>("%Bounds") ?? _view.Hitbox;
        var transform = bounds.GetGlobalTransformWithCanvas();
        var corners = new[] { Vector2.Zero, new Vector2(bounds.Size.X, 0), bounds.Size, new Vector2(0, bounds.Size.Y) }.Select(p => transform * p).ToArray();
        float left = corners.Min(v => v.X), right = corners.Max(v => v.X), top = corners.Min(v => v.Y), bottom = corners.Max(v => v.Y);
        _foot = new((left + right) * .5f, bottom);
        _unit = Mathf.Clamp((bottom - top) / 160f, .45f, 2.4f);
        _water.Size = new Vector2(480, 118) * _unit; _water.Position = _foot - _water.Size * new Vector2(.5f, .53f);
        _selectionWater.Size = new Vector2(_screen.X * .62f, _screen.Y * .13f);
        _selectionWater.Position = new Vector2(_screen.X * .19f, _screen.Y * .87f);
        for (int i = 0; i < _jellies.Count; i++)
        {
            float phase = _age * .38f + i * 2.13f;
            var size = (i == 0 ? new Vector2(144, 216) : i == 1 ? new Vector2(68, 110) : new Vector2(64, 104)) * _unit;
            var offset = i == 0 ? new Vector2(72, -360) : i == 1 ? new Vector2(-112, -186) : new Vector2(118, -128);
            var node = _jellies[i].Node; node.Size = size;
            node.Position = _foot + (offset + new Vector2(Mathf.Sin(phase) * 10, Mathf.Cos(phase * .83f) * 9)) * _unit - new Vector2(size.X * .5f, 0);
        }
    }
    public override void _Draw()
    {
        DrawRipple(this, _foot - new Vector2(0, 3 * _unit), Pulse(_castAge, 1.25f), Mathf.Clamp(_castAge / 1.25f, 0, 1), _unit * 190);
        DrawRipple(this, _foot - new Vector2(0, 3 * _unit), Pulse(_turnAge, 1.0f) * .60f, Mathf.Clamp(_turnAge, 0, 1), _unit * 160);
    }
    private void DrawHand()
    {
        foreach (var card in _drawn)
            if (CardPoint(card.Card) is {} point)
            {
                float p = Mathf.Clamp((_age - card.Born) / 1.4f, 0, 1);
                DrawRipple(_hud, point, Mathf.Sin(p * Mathf.Pi) * .7f, p, 40 * _screen.Y / 720f);
            }
        foreach (var ripple in _ripples)
        {
            float p = Mathf.Clamp((_age - ripple.Born) / .75f, 0, 1);
            DrawRipple(_hud, ripple.Point, (1 - p) * .8f, p, 65 * _screen.Y / 720f);
        }
    }
    private static void DrawRipple(Node2D canvas, Vector2 center, float light, float progress, float radius)
    {
        if (light < .005f) return;
        var points = new Vector2[81];
        float r = Mathf.Lerp(.34f, 1, progress) * radius;
        for (int i = 0; i < points.Length; i++) { float a = i * Mathf.Tau / (points.Length - 1); points[i] = center + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r * .22f); }
        canvas.DrawPolyline(points, new Color(.17f, .66f, .9f, light * .13f), 7, true);
        canvas.DrawPolyline(points, new Color(.48f, .88f, 1, light * .64f), 1.25f, true);
    }
    private static CpuParticles2D Bubbles(int count, float life) => new() {
        Amount = KarenVfxQuality.ParticleCount(count), Lifetime = life, OneShot = true, Explosiveness = 1, Emitting = false,
        Texture = _bubble, Direction = Vector2.Up, Spread = 18, Gravity = new(0, -4), InitialVelocityMin = 18,
        InitialVelocityMax = 42, ScaleAmountMin = .10f, ScaleAmountMax = .28f,
        EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle, EmissionRectExtents = new(30, 10),
        ColorRamp = new Gradient { Offsets = [0, .16f, .70f, 1], Colors = [Colors.Transparent, new(.45f,.87f,1,.72f), new(.38f,.76f,1,.4f), Colors.Transparent] },
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add }
    };
    private void Burst(Node2D parent, Vector2 point, int count)
    {
        var particles = Bubbles(count, .85f); particles.Position = point; particles.EmissionRectExtents = new Vector2(38, 8) * _unit;
        parent.AddChild(particles); particles.Emitting = true; _bursts.Add((particles, _age + 1.1f));
    }
    private void Sound(string file, float volume, float pitch = 1)
    {
        var sound = KarenAudioManager.PlaySfxOnGameBus(file, volume, pitch);
        if (sound != null) _sounds.Add(sound);
    }
    private void Stop()
    {
        if (!Alive) return;
        Hide(); _back.Hide(); _hud.Hide(); Selecting = false; QueueFree();
    }
    public override void _ExitTree()
    {
        foreach (var layer in new[] { _back, _hud }) if (GodotObject.IsInstanceValid(layer)) { layer.Hide(); layer.QueueFree(); }
        foreach (var sound in _sounds) if (GodotObject.IsInstanceValid(sound)) { sound.Stop(); sound.QueueFree(); }
        _sounds.Clear(); _handPoints.Clear(); _drawn.Clear(); _ripples.Clear(); Selecting = false;
    }
}
