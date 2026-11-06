using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Silent confirmation of an actual Passion buff gain, using its existing icon and status location.</summary>
public partial class NKarenPassionVfx : Node2D
{
    // Shared visual resources are prepared during the run loading screen as well as on demand.
    internal static void PrepareResources()
    {
        _sharedIcon ??= ResourceLoader.Load<Texture2D>("res://images/powers/karen_passion_power.png");
        _soft ??= new GradientTexture2D { Width = 96, Height = 96, Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new(.5f, .5f), FillTo = new(1, .5f), Gradient = new Gradient {
                Offsets = [0, .2f, .6f, 1], Colors = [Colors.White, new(1, 1, 1, .45f), new(1, 1, 1, .06f), Colors.Transparent] } };
    }
    internal static Godot.Resource?[] PreparedResources => [_soft, _sharedIcon];
    private static Texture2D? _sharedIcon;

    private sealed record Pulse(float At, int Gain);
    private static readonly Color Gold = new("#ffd785"), Hot = new("#fff5d8"), Red = new("#f36b75");
    private Texture2D? _icon;
    private static GradientTexture2D? _soft;
    private readonly Node2D _light = new();
    private readonly List<Pulse> _pulses = new(3);
    private NCombatRoom _room = null!;
    private KarenPassionPower _power = null!;
    private Creature _owner = null!;
    private NPower? _badge;
    private Vector2 _body, _origin, _destination;
    private float _age, _unit, _speed, _searchAt;
    private int _totalGain;
    private const float TravelAt = .18f, ArriveAt = .70f, Lifetime = 1.05f;

    public static void Play(KarenPassionPower power, int gain)
    {
        if (!KarenVfxQuality.Enabled || gain <= 0 || NonInteractiveMode.IsActive || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant
            || NCombatRoom.Instance is not { } room) return;
        var owner = power.Owner;
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(room) || NCombatRoom.Instance != room || !CombatManager.Instance.IsInProgress
                || owner.IsDead || owner.GetPower<KarenPassionPower>() != power || room.GetCreatureNode(owner) == null) return;
            var current = room.CombatVfxContainer.GetChildren().OfType<NKarenPassionVfx>()
                .FirstOrDefault(v => v._power == power && !v.IsQueuedForDeletion());
            if (current != null) { current.AddPulse(gain); return; }
            var effect = new NKarenPassionVfx { Name = "KarenPassionBuffGain", _room = room, _power = power, _owner = owner,
                _speed = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? 1.6f : 1 };
            effect._pulses.Add(new Pulse(0, gain)); effect._totalGain = gain;
            room.CombatVfxContainer.AddChild(effect);
        }).CallDeferred();
    }

    private void AddPulse(int gain)
    {
        _totalGain += gain;
        if (_pulses.Count == 3) _pulses.RemoveAt(0);
        _pulses.Add(new Pulse(_age, gain));
    }

    public override void _Ready()
    {
        // The shared visual catalog pins this texture across native room-cache eviction.
        PrepareResources();
        _icon = _sharedIcon;
        AddChild(_light);
        _light.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        _light.Draw += DrawLight;
        TextureFilter = TextureFilterEnum.Linear;
        KarenVfxComposition.RegisterCue(this, _owner);
        Place();
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_room) || !_room.IsInsideTree() || NCombatRoom.Instance != _room
            || !CombatManager.Instance.IsInProgress || _owner.IsDead || _owner.GetPower<KarenPassionPower>() != _power
            || _power.Amount <= 0 || _room.GetCreatureNode(_owner) == null)
        { Hide(); QueueFree(); return; }
        _age += (float)delta * _speed;
        _pulses.RemoveAll(p => _age - p.At > Lifetime);
        if (_pulses.Count == 0) { QueueFree(); return; }
        Place();
        KarenVfxComposition.SetCueStrength(this, .35f * Window(_age - _pulses[^1].At, 0, Lifetime, .08f, .3f));
        KarenVfxQuality.Redraw(this); KarenVfxQuality.Redraw(_light);
    }

    private void Place()
    {
        GlobalTransform = GetCanvasTransform().AffineInverse();
        var hit = _room.GetCreatureNode(_owner)!.Hitbox;
        var canvas = hit.GetGlobalTransformWithCanvas();
        _body = canvas * (hit.Size * .5f);
        float height = (canvas * new Vector2(hit.Size.X * .5f, hit.Size.Y)).DistanceTo(canvas * new Vector2(hit.Size.X * .5f, 0));
        var screen = GetViewportRect().Size;
        float screenUnit = Mathf.Min(screen.X / 1280, screen.Y / 720);
        _unit = Mathf.Clamp(height / 190, .60f * screenUnit, 1.2f * screenUnit);
        _origin = _body + new Vector2(72, -72) * _unit;
        if (!KarenPowerVfxAnchor.IsFor(_badge, _power)) _badge = null;
        if (_badge == null && _age >= _searchAt)
        {
            _searchAt = _age + .08f;
            _badge = KarenPowerVfxAnchor.Find(_room, _power);
        }
        if (_badge != null) _destination = _badge.GetGlobalTransformWithCanvas() * (_badge.Size * .5f);
    }

    private Vector2 GainPoint(float elapsed)
    {
        if (_badge == null) return _origin + Vector2.Up * Smooth(elapsed / .6f) * 12 * _unit;
        float t = Smooth((elapsed - TravelAt) / (ArriveAt - TravelAt));
        return _origin.Lerp(_destination, t) + Vector2.Right * (Mathf.Sin(t * Mathf.Pi) * 26 * _unit);
    }

    public override void _Draw()
    {
        if (!GodotObject.IsInstanceValid(_icon)) return;
        foreach (var pulse in _pulses)
        {
            float elapsed = _age - pulse.At;
            float travel = _badge == null ? 0 : Smooth((elapsed - TravelAt) / (ArriveAt - TravelAt));
            float alpha = Window(elapsed, 0, .83f, .06f, .16f);
            float size = Mathf.Lerp(48, 19, travel) * _unit * (.72f + .28f * Smooth(elapsed / .1f));
            var point = GainPoint(elapsed);
            DrawTextureRect(_icon, new Rect2(point - Vector2.One * size * .5f, Vector2.One * size), false, new Color(1, 1, 1, alpha));
            if (_badge != null && elapsed >= ArriveAt)
            {
                float textAlpha = Window(elapsed, ArriveAt, Lifetime, .045f, .16f);
                var textPoint = _destination + new Vector2(18, -12 - (elapsed - ArriveAt) * 16) * _unit;
                DrawString(ThemeDB.FallbackFont, textPoint, $"+{pulse.Gain}", HorizontalAlignment.Left, -1,
                    Mathf.Max(12, Mathf.RoundToInt(20 * _unit)), new Color(Hot, textAlpha));
            }
        }
    }

    private void DrawLight()
    {
        foreach (var pulse in _pulses)
        {
            float elapsed = _age - pulse.At;
            float bloom = Window(elapsed, 0, .40f, .045f, .23f);
            Glow(_origin, 49 * _unit, Gold, bloom * .38f);
            for (int i = 0; i < 12; i++)
            {
                float t = (elapsed - i * .012f) / .48f;
                if (t <= 0 || t >= 1) continue;
                float angle = i * 2.39996f;
                var p = _origin + new Vector2(Mathf.Cos(angle) * (12 + 29 * t), Mathf.Sin(angle) * 17 - t * 45) * _unit;
                Glint(p, (i % 4 == 0 ? 4.8f : 2.2f) * _unit, i % 3 == 0 ? Red : Gold, Mathf.Sin(t * Mathf.Pi) * .8f);
            }
            if (elapsed > TravelAt && elapsed < .80f && _badge != null)
            {
                for (int i = 1; i <= 8; i++)
                {
                    float previous = elapsed - i * .014f;
                    if (previous < TravelAt || previous > ArriveAt) continue;
                    _light.DrawLine(GainPoint(previous), GainPoint(previous + .014f), new Color(i % 2 == 0 ? Red : Gold, (1 - i / 9f) * .65f),
                        (3.8f - i * .3f) * _unit, true);
                }
            }
            if (_badge != null && elapsed >= ArriveAt)
            {
                float alpha = Window(elapsed, ArriveAt, Lifetime, .03f, .23f);
                Glow(_destination, 27 * _unit, Gold, alpha * .45f);
                for (int i = 0; i < 4; i++)
                    Glint(_destination + Vector2.FromAngle(i * Mathf.Pi / 2) * (16 + (elapsed - ArriveAt) * 21) * _unit,
                        3.7f * _unit, i % 2 == 0 ? Gold : Red, alpha);
            }
        }
    }

    private static float Smooth(float t) { t = Mathf.Clamp(t, 0, 1); return t * t * (3 - 2 * t); }
    private static float Window(float t, float start, float end, float fadeIn, float fadeOut)
        => Smooth((t - start) / fadeIn) * (1 - Smooth((t - (end - fadeOut)) / fadeOut));
    private void Glow(Vector2 point, float radius, Color color, float alpha)
        => _light.DrawTextureRect(_soft!, new Rect2(point - Vector2.One * radius, Vector2.One * radius * 2), false, new Color(color, alpha));
    private void Glint(Vector2 point, float radius, Color color, float alpha)
    {
        _light.DrawLine(point - Vector2.Up * radius, point + Vector2.Up * radius, new Color(color, alpha), _unit, true);
        _light.DrawLine(point - Vector2.Right * radius * .6f, point + Vector2.Right * radius * .6f, new Color(color, alpha), _unit, true);
    }
    public override void _ExitTree()
    {
        KarenVfxComposition.RemoveCue(this);
        _pulses.Clear(); _badge = null;
        _icon = null;
    }
}
