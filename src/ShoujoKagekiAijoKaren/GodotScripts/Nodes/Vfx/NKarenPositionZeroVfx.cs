using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Theatre tape and warm follow spots; the power owns the stage, actual block owns each return.</summary>
public partial class NKarenPositionZeroVfx : Node2D
{
    private sealed class Return(Creature target, float strength)
    {
        public readonly Creature Target = target;
        public readonly float Strength = strength;
        public Vector2 Source;
        public float Age;
    }

    private KarenPosition0Power _power = null!;
    private NCombatRoom _room = null!;
    private NCreature _creature = null!;
    private readonly List<Return> _returns = new();
    private readonly List<AudioStreamPlayer> _sounds = new();
    private static GradientTexture2D? _soft;
    private static readonly Color Red = new("#fb455a"), Gold = new("#ffd58a"), Ivory = new("#fff4d9");
    private Vector2 _foot, _body;
    private float _age, _claimAge, _unit, _facing, _lastSound = -10;
    public int Confirmations { get; private set; }
    public int ActiveReturns => _returns.Count;
    public float ClaimAge => _claimAge;
    public Creature StageOwner => _power.Owner;
    public Vector2 FootPoint => _foot;
    private static bool Enabled => !NonInteractiveMode.IsActive && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant;
    private float Speed => SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? .65f : 1;
    private float Claim => Window(_claimAge, .22f, 1.65f, .25f, .6f);
    private float Pulse => _returns.Count == 0 ? 0 : _returns.Max(r => Window(r.Age, .18f, .8f, .08f, .45f));

    public static NKarenPositionZeroVfx? Ensure(KarenPosition0Power power, bool claim = false)
    {
        var room = NCombatRoom.Instance;
        var creature = room?.GetCreatureNode(power.Owner);
        if (!Enabled || room == null || creature == null || !CombatManager.Instance.IsInProgress
            || power.Amount <= 0 || power.Owner.GetPower<KarenPosition0Power>() != power) return null;
        var existing = room.CombatVfxContainer.GetChildren().OfType<NKarenPositionZeroVfx>()
            .FirstOrDefault(n => n._power == power && !n.IsQueuedForDeletion());
        if (existing != null)
        {
            if (claim) existing.ClaimStage();
            return existing;
        }
        var node = new NKarenPositionZeroVfx { _power = power, _room = room, _creature = creature,
            _claimAge = claim ? 0 : 2, Name = "KarenPositionZeroVfx" };
        room.CombatVfxContainer.AddChildSafely(node);
        return node;
    }

    public static void Remove(KarenPosition0Power power)
    {
        if (NCombatRoom.Instance is not {} room) return;
        foreach (var node in room.CombatVfxContainer.GetChildren().OfType<NKarenPositionZeroVfx>().Where(n => n._power == power)) node.Stop();
    }

    private void ClaimStage()
    {
        _claimAge = 0;
        Sound("karen_benefit_gather.wav", .18f, .9f);
    }

    public void ConfirmBlock(Creature target, decimal actualGain)
    {
        if (actualGain <= 0 || !IsInsideTree() || IsQueuedForDeletion()) return;
        Confirmations++;
        // Closely spaced hits share one stage. Bound transient geometry for replay/AOE bursts.
        if (_returns.Count >= 6) _returns.RemoveAt(0);
        var pulse = new Return(target, Mathf.Clamp(Mathf.Log((float)actualGain + 1) / 3, .45f, 1));
        pulse.Source = TargetPoint(target);
        _returns.Add(pulse);
        if (_age - _lastSound > .12f)
        {
            Sound("karen_benefit_bloom.wav", .15f, 1.16f);
            _lastSound = _age;
        }
    }

    public override void _Ready()
    {
        _soft ??= new GradientTexture2D { Width = 96, Height = 96, Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new(.5f, .5f), FillTo = new(1, .5f), Gradient = new Gradient {
                Offsets = [0, .25f, .65f, 1], Colors = [Colors.White, new(1, 1, 1, .6f), new(1, 1, 1, .1f), Colors.Transparent] } };
        TextureFilter = TextureFilterEnum.Linear;
        KarenVfxComposition.RegisterCue(this, _power.Owner);
        Place();
        if (_claimAge == 0) Sound("karen_benefit_gather.wav", .18f, .9f);
    }

    public override void _Process(double delta)
    {
        if (!Enabled || !GodotObject.IsInstanceValid(_room) || NCombatRoom.Instance != _room
            || !GodotObject.IsInstanceValid(_creature) || !_creature.IsInsideTree()
            || !CombatManager.Instance.IsInProgress || _power.Owner.IsDead
            || _power.Owner.GetPower<KarenPosition0Power>() != _power || _power.Amount <= 0)
        { Stop(); return; }
        float dt = (float)delta / Speed;
        _age += dt; _claimAge += dt;
        for (int i = _returns.Count - 1; i >= 0; i--)
        {
            var pulse = _returns[i]; pulse.Age += dt;
            if (pulse.Age >= .86f) _returns.RemoveAt(i);
            else if (_room.GetCreatureNode(pulse.Target) is {} target && GodotObject.IsInstanceValid(target))
                pulse.Source = TargetPoint(pulse.Target);
        }
        _sounds.RemoveAll(s => !GodotObject.IsInstanceValid(s));
        Place();
        KarenVfxComposition.SetCueStrength(this, Mathf.Max(Claim, Pulse) * .7f);
        QueueRedraw();
    }

    private void Place()
    {
        // Screen space keeps the T upright for mirrored remote players and camera/viewport scaling.
        GlobalTransform = GetCanvasTransform().AffineInverse();
        var hit = _creature.Hitbox;
        var transform = hit.GetGlobalTransformWithCanvas();
        _body = transform * (hit.Size * .5f);
        _foot = transform * new Vector2(hit.Size.X * .5f, hit.Size.Y);
        _unit = Mathf.Clamp(Mathf.Abs(_foot.Y - _body.Y) / 95, .5f, 2.5f);
        _facing = _creature.Scale.X < 0 ? -1 : 1;
    }

    private Vector2 TargetPoint(Creature target)
    {
        var hit = _room.GetCreatureNode(target)?.Hitbox;
        return hit == null ? _body : hit.GetGlobalTransformWithCanvas() * (hit.Size * .5f);
    }

    private void DrawStage()
    {
        Vector2 at = _foot + new Vector2(0, -9) * _unit;
        if (Claim > 0)
        {
            Beam(at, -128, 34, Claim);
            Beam(at, 138, 43, Claim * .8f);
            for (int i = 0; i < 22; i++)
            {
                float phase = Mathf.PosMod(i * .618f + _claimAge * .24f, 1);
                Vector2 dot = at + new Vector2(Mathf.Sin(i * 12.3f) * 92, -30 - phase * 290) * _unit;
                Soft(this, dot, Vector2.One * (4 + i % 3) * _unit, Gold, Claim * Mathf.Sin(phase * Mathf.Pi) * .7f);
            }
        }
    }

    private void DrawFloor()
    {
        float ambient = KarenVfxComposition.AmbientWeight(_power.Owner);
        float reveal = Ease(P(_claimAge, 0, .28f));
        float a = reveal * (.35f * ambient + Claim * .65f + Pulse * .35f);
        Vector2 at = _foot + new Vector2(0, -9) * _unit;
        Soft(this, at, new Vector2(260, 64) * _unit, Gold, reveal * (.09f * ambient + .37f * Claim + .18f * Pulse));
        Soft(this, at, new Vector2(150, 32) * _unit, Ivory, reveal * (.06f * ambient + .24f * Claim));
        // A shallow perspective T made from theatre tape, with a solid red body and warm lit edge.
        float width = 72 * _unit * reveal;
        Tape(at + new Vector2(-width, 0), at + new Vector2(width, 0), 6 * _unit, a);
        float stem = Ease(P(_claimAge, .17f, .42f));
        // The native health bar sits just below the sole. Keep the perspective stem above that HUD.
        Tape(at, at + new Vector2(0, 13 * stem) * _unit, 8 * _unit, a);
    }

    private void Tape(Vector2 from, Vector2 to, float width, float alpha)
    {
        if (from.DistanceSquaredTo(to) < .1f) return;
        DrawLine(from, to, new Color(Red, alpha * .10f), width * 3.5f, true);
        DrawLine(from, to, new Color(Red, alpha * .3f), width * 1.8f, true);
        DrawLine(from, to, new Color(Red, Mathf.Min(1, alpha + .25f)), width, true);
        DrawLine(from + new Vector2(0, -width * .4f), to + new Vector2(0, -width * .4f), new Color(Ivory, alpha * .7f), _unit, true);
    }

    private void Beam(Vector2 at, float offset, float spread, float alpha)
    {
        Vector2 top = at + new Vector2(offset, -470) * _unit;
        // Feathered strips make soft optical edges without an opaque cone or a shared material.
        for (int i = 0; i < 16; i++)
        {
            float x0 = (i / 16f * 2 - 1), x1 = ((i + 1) / 16f * 2 - 1);
            float opacity = Mathf.Pow(1 - Mathf.Abs((x0 + x1) * .5f), 1.7f) * alpha * .30f;
            Vector2[] points = [top + new Vector2(x0 * 9, 0) * _unit, top + new Vector2(x1 * 9, 0) * _unit,
                at + new Vector2(x1 * (spread + 54), 0) * _unit, at + new Vector2(x0 * (spread + 54), 0) * _unit];
            DrawPolygon(points, [new(Gold, opacity * .7f), new(Gold, opacity * .7f), new(Ivory, opacity), new(Ivory, opacity)]);
        }
    }

    public override void _Draw()
    {
        // Projected light tints the actor softly; tape and guard follow it in the native VFX plane.
        DrawStage();
        DrawFloor();
        foreach (var pulse in _returns)
        {
            float flight = P(pulse.Age, 0, .27f);
            for (int i = 0; i < 15; i++)
            {
                float t = Mathf.Clamp(flight - i * .018f, 0, 1);
                Vector2 point = ReturnPoint(pulse.Source, t);
                Soft(this, point, new Vector2(24, 7) * _unit, i < 4 ? Ivory : Gold,
                    (1 - i / 15f) * (1 - P(pulse.Age, .22f, .4f)) * pulse.Strength * .8f);
            }
            float guard = Window(pulse.Age, .18f, .84f, .09f, .4f) * pulse.Strength;
            if (guard <= .001f) continue;
            Vector2 center = _body + new Vector2(30 * _facing, 0) * _unit;
            float facingAngle = _facing < 0 ? Mathf.Pi : 0;
            for (int i = 0; i < 4; i++)
            {
                float radius = (55 + i * 4) * _unit;
                DrawArc(center, radius, facingAngle - 1.05f, facingAngle + 1.05f, 40,
                    new Color(i == 0 ? Ivory : Gold, guard * (i == 0 ? .9f : .16f)), (i == 0 ? 2.5f : 5) * _unit, true);
            }
            for (int i = 0; i < 8; i++)
            {
                float angle = facingAngle - .9f + i / 7f * 1.8f;
                Vector2 dot = center + Vector2.FromAngle(angle) * (58 + P(pulse.Age, .27f, .8f) * 24) * _unit;
                Soft(this, dot, Vector2.One * 6 * _unit, Ivory, guard * .65f);
            }
        }
    }

    private Vector2 ReturnPoint(Vector2 source, float t)
    {
        Vector2 end = _body + new Vector2(80 * _facing, -4) * _unit;
        return source.Lerp(end, t) + new Vector2(0, -40 * Mathf.Sin(t * Mathf.Pi)) * _unit;
    }
    private static void Soft(Node2D node, Vector2 center, Vector2 size, Color color, float alpha)
    {
        if (alpha > .001f && _soft != null) node.DrawTextureRect(_soft, new Rect2(center - size * .5f, size), false, new Color(color, Mathf.Clamp(alpha, 0, 1)));
    }
    private void Sound(string path, float volume, float pitch)
    {
        var sound = KarenAudioManager.PlaySfx(path, volume, pitch / Speed);
        if (sound != null) _sounds.Add(sound);
    }
    private static float P(float x, float a, float b) => Mathf.Clamp((x - a) / (b - a), 0, 1);
    private static float Ease(float t) => 1 - Mathf.Pow(1 - t, 3);
    private static float Window(float t, float a, float b, float attack, float release) => P(t, a, a + attack) * (1 - P(t, b - release, b));
    public void Stop() { Hide(); QueueFree(); }
    public override void _ExitTree()
    {
        KarenVfxComposition.RemoveCue(this);
        foreach (var sound in _sounds) if (GodotObject.IsInstanceValid(sound)) { sound.Stop(); sound.QueueFree(); }
        _sounds.Clear(); _returns.Clear();
    }
}
