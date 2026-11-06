using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Audio;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Viewport-wide rose/gold particles for playing Passion, independent of its buff confirmation.</summary>
public partial class NKarenPassionCardVfx : Node2D
{
    // Shared visual resources are prepared during the run loading screen as well as on demand.
    internal static void PrepareResources()
    {
        _soft ??= new GradientTexture2D { Width = 64, Height = 64, Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new(.5f, .5f), FillTo = new(1, .5f), Gradient = new Gradient {
                Offsets = [0, .2f, .55f, 1], Colors = [Colors.White, new(1,1,1,.46f), new(1,1,1,.07f), Colors.Transparent] } };
    }
    internal static Godot.Resource?[] PreparedResources => [_soft];

    private sealed record Particle(Vector2 Origin, Vector2 Velocity, float At, float Life, float Radius,
        float Phase, bool Star, bool Back, Color Tint);
    private static readonly Color Rose = new("#ff4f72"), Coral = new("#ff9a7a"), Gold = new("#ffd892"), Hot = new("#fff2d8");
    private static GradientTexture2D? _soft;
    private readonly Node2D _back = new() { Name = "KarenPassionCardBackground" };
    private readonly List<Particle> _particles = new(320);
    private readonly List<AudioStreamPlayer> _sounds = new();
    private NCombatRoom _room = null!;
    private Player _owner = null!;
    private Vector2 _screen;
    private Rect2 _quietRegion;
    private float _age, _unit, _speed, _lastSoundAt = -1;
    private int _plays = 1;
    private const int Limit = 320;

    public static void Play(Player owner)
    {
        if (!KarenVfxQuality.Enabled || NonInteractiveMode.IsActive || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant
            || NCombatRoom.Instance is not { } room) return;
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(room) || NCombatRoom.Instance != room || !CombatManager.Instance.IsInProgress
                || owner.Creature.IsDead || room.GetCreatureNode(owner.Creature) == null) return;
            // One bounded particle field per owner. Reinforcement does not restart older particles or touch the buff.
            var current = room.CombatVfxContainer.GetChildren().OfType<NKarenPassionCardVfx>()
                .FirstOrDefault(v => v._owner == owner && !v.IsQueuedForDeletion());
            if (current != null) { current._plays++; current.Emit(64); current.Sound(); return; }
            room.CombatVfxContainer.AddChild(new NKarenPassionCardVfx {
                Name = "KarenPassionCardParticles", _room = room, _owner = owner,
                _speed = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? 1.6f : 1
            });
        }).CallDeferred();
    }

    public override void _Ready()
    {
        PrepareResources();
        Material = _back.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        _room.BackCombatVfxContainer.AddChild(_back);
        _back.Draw += () => DrawParticles(_back, true);
        Place(); Emit(252); Sound();
    }

    private void Emit(int count)
    {
        count = Math.Min(KarenVfxQuality.ParticleCount(count), KarenVfxQuality.ParticleCount(Limit) - _particles.Count);
        for (int i = 0; i < count; i++)
        {
            // Presentation RNG is separate from all gameplay/card/reward RNG streams.
            float x = GD.Randf(), y = GD.Randf(), side = x < .5f ? 1 : -1;
            bool back = i % 3 == 0;
            var tint = i % 11 == 0 ? Hot : i % 3 == 0 ? Gold : i % 2 == 0 ? Rose : Coral;
            _particles.Add(new Particle(new Vector2(x, y),
                new Vector2(side * (.025f + GD.Randf() * .075f), -.055f - GD.Randf() * .14f),
                _age + GD.Randf() * .22f, 1.25f + GD.Randf() * .70f,
                back ? 9 + GD.Randf() * 13 : 1.5f + GD.Randf() * 3,
                GD.Randf() * Mathf.Tau, !back && i % 5 == 0, back, tint));
        }
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_room) || NCombatRoom.Instance != _room || !_room.IsInsideTree()
            || !CombatManager.Instance.IsInProgress || _owner.Creature.IsDead || _room.GetCreatureNode(_owner.Creature) == null)
        { Hide(); QueueFree(); return; }
        _age += (float)delta * _speed;
        _particles.RemoveAll(p => _age - p.At >= p.Life);
        if (_particles.Count == 0) { QueueFree(); return; }
        Place(); KarenVfxQuality.Redraw(this); KarenVfxQuality.Redraw(_back);
    }

    private void Place()
    {
        GlobalTransform = GetCanvasTransform().AffineInverse();
        _back.GlobalTransform = _back.GetCanvasTransform().AffineInverse();
        _screen = GetViewportRect().Size;
        _unit = Mathf.Min(_screen.X / 1280, _screen.Y / 720);
        var hit = _room.GetCreatureNode(_owner.Creature)!.Hitbox;
        var canvas = hit.GetGlobalTransformWithCanvas();
        _quietRegion = new Rect2(canvas * Vector2.Zero, Vector2.Zero)
            .Expand(canvas * new Vector2(hit.Size.X, 0)).Expand(canvas * new Vector2(0, hit.Size.Y)).Expand(canvas * hit.Size);
        // Leave the character and status row readable while particles cover the rest of the viewport.
        _quietRegion = _quietRegion.Grow(18 * _unit);
        _quietRegion.Size += Vector2.Down * 58 * _unit;
    }

    private Vector2 PositionAt(Particle p, float elapsed)
    {
        var drift = p.Velocity * elapsed;
        drift.X += Mathf.Sin(elapsed * 3.1f + p.Phase) * .012f;
        return (p.Origin + drift) * _screen;
    }

    private float Visibility(Vector2 point)
    {
        float dx = Mathf.Max(Mathf.Max(_quietRegion.Position.X - point.X, 0), point.X - _quietRegion.End.X);
        float dy = Mathf.Max(Mathf.Max(_quietRegion.Position.Y - point.Y, 0), point.Y - _quietRegion.End.Y);
        float mask = Mathf.Lerp(.10f, 1, Mathf.SmoothStep(0, 1, Mathf.Clamp(new Vector2(dx, dy).Length() / (55 * _unit), 0, 1)));
        if (point.Y < _screen.Y * .085f) mask *= .25f;
        if (point.Y > _screen.Y * .80f) mask *= .50f;
        return mask;
    }

    public override void _Draw() => DrawParticles(this, false);

    private void DrawParticles(Node2D layer, bool back)
    {
        foreach (var p in _particles)
        {
            if (p.Back != back) continue;
            float elapsed = _age - p.At;
            if (elapsed <= 0 || elapsed >= p.Life) continue;
            var point = PositionAt(p, elapsed);
            if (point.X < -35 * _unit || point.X > _screen.X + 35 * _unit || point.Y < -35 * _unit || point.Y > _screen.Y + 35 * _unit) continue;
            float t = elapsed / p.Life;
            float alpha = Mathf.SmoothStep(0, 1, Mathf.Min(1, elapsed / .16f)) * Mathf.Pow(1 - t, .9f) * Visibility(point);
            float radius = p.Radius * _unit;
            if (back)
            {
                layer.DrawTextureRect(_soft!, new Rect2(point - Vector2.One * radius * 2, Vector2.One * radius * 4), false, new Color(p.Tint, alpha * .12f));
                layer.DrawCircle(point, radius * .23f, new Color(p.Tint, alpha * .22f));
                continue;
            }
            var trail = PositionAt(p, Mathf.Max(0, elapsed - .055f));
            layer.DrawLine(trail, point, new Color(p.Tint, alpha * .30f), radius * .65f, true);
            layer.DrawTextureRect(_soft!, new Rect2(point - Vector2.One * radius * 2.8f, Vector2.One * radius * 5.6f), false, new Color(p.Tint, alpha * .35f));
            if (p.Star)
            {
                float spin = p.Phase + elapsed * .35f;
                var axis = Vector2.FromAngle(spin) * radius * 2.8f;
                layer.DrawLine(point - axis, point + axis, new Color(p.Tint, alpha * .86f), 1.2f * _unit, true);
                axis = axis.Orthogonal() * .58f;
                layer.DrawLine(point - axis, point + axis, new Color(Hot, alpha * .85f), _unit, true);
                layer.DrawCircle(point, radius * .5f, new Color(Hot, alpha));
            }
            else
            {
                layer.DrawCircle(point, radius * .60f, new Color(p.Tint, alpha * .75f));
                layer.DrawCircle(point, radius * .23f, new Color(Hot, alpha * .65f));
            }
        }
    }

    private void Sound()
    {
        if (_lastSoundAt >= 0 && (_age - _lastSoundAt) / _speed < .35f) return;
        _lastSoundAt = _age;
        var sound = KarenAudioManager.PlaySfxOnGameBus("karen_passion_card.wav", .42f, Mathf.Min(_speed, 1.1f));
        if (sound != null) _sounds.Add(sound);
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_back)) { _back.Hide(); _back.QueueFree(); }
        foreach (var sound in _sounds)
            if (GodotObject.IsInstanceValid(sound)) { sound.Stop(); sound.QueueFree(); }
        _sounds.Clear(); _particles.Clear();
    }
}
