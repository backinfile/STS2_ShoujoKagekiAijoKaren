using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>A satin star seals an unrevealed reward, then settles into its actual status icon.</summary>
public partial class NKarenPassionVfx : Node2D
{
    private static readonly Color Scarlet = new("#d93249"), Burgundy = new("#771e38"), Gold = new("#edbb68"),
        Hot = new("#fff0c5"), Paper = new("#f5dfb2"), Shade = new("#b99b78");
    private static readonly Vector2[] Star = Enumerable.Range(0, 10).Select(i =>
        Vector2.FromAngle(-Mathf.Pi / 2 + i * Mathf.Pi / 5) * (i % 2 == 0 ? 1 : .43f)).ToArray();
    private static GradientTexture2D? _soft;
    private readonly Node2D _light = new();
    private readonly List<AudioStreamPlayer> _sounds = new();
    private readonly Vector2[] _quad = new Vector2[4];
    private NCombatRoom _room = null!;
    private Player _owner = null!;
    private NPower? _badge;
    private Vector2 _body, _center, _destination;
    private float _age, _unit, _speed, _badgeSearch;
    private bool _stamped, _arrived;
    private const float StampAt = 1.38f, TravelAt = 2.06f, ArriveAt = 2.80f, EndAt = 3.18f;

    public static void Play(Player owner)
    {
        if (NonInteractiveMode.IsActive || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant
            || NCombatRoom.Instance is not { } room) return;
        // Reward commands may resume away from the scene thread. Only the deferred callback touches nodes.
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(room) || NCombatRoom.Instance != room
                || !CombatManager.Instance.IsInProgress || owner.Creature.IsDead
                || owner.Creature.GetPowerAmount<KarenPassionPower>() <= 0 || room.GetCreatureNode(owner.Creature) == null) return;
            // Rapid replays retain their own motion; bound simultaneous decoration and sound per owner.
            if (room.CombatVfxContainer.GetChildren().OfType<NKarenPassionVfx>()
                .Count(v => v._owner == owner && !v.IsQueuedForDeletion()) >= 2) return;
            room.CombatVfxContainer.AddChild(new NKarenPassionVfx {
                Name = "KarenPassionSeal", _room = room, _owner = owner,
                _speed = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? 1.65f : 1
            });
        }).CallDeferred();
    }

    public override void _Ready()
    {
        _soft ??= new GradientTexture2D { Width = 128, Height = 128, Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new(.5f, .5f), FillTo = new(1, .5f), Gradient = new Gradient {
                Offsets = [0, .18f, .55f, 1], Colors = [Colors.White, new(1, 1, 1, .5f), new(1, 1, 1, .08f), Colors.Transparent] } };
        AddChild(_light);
        _light.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        _light.Draw += DrawLight;
        TextureFilter = TextureFilterEnum.Linear;
        KarenVfxComposition.RegisterCue(this, _owner.Creature);
        Place();
        Sound("karen_passion_silk.wav", .55f);
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_room) || !_room.IsInsideTree() || NCombatRoom.Instance != _room
            || !CombatManager.Instance.IsInProgress || _owner.Creature.IsDead
            || _owner.Creature.GetPowerAmount<KarenPassionPower>() <= 0 || _room.GetCreatureNode(_owner.Creature) == null)
        { Hide(); QueueFree(); return; }
        _age += (float)delta * _speed;
        if (_age >= EndAt) { QueueFree(); return; }
        Place();
        if (!_stamped && _age >= StampAt) { _stamped = true; Sound("karen_passion_stamp.wav", .70f); }
        if (!_arrived && _age >= ArriveAt) _arrived = true;
        KarenVfxComposition.SetCueStrength(this, .7f * Window(_age, 0, EndAt, .2f, .4f));
        QueueRedraw(); _light.QueueRedraw();
    }

    private void Place()
    {
        // Viewport-space drawing keeps cloth width and the textless envelope upright under flipped skins.
        GlobalTransform = GetCanvasTransform().AffineInverse();
        var hit = _room.GetCreatureNode(_owner.Creature)!.Hitbox;
        var canvas = hit.GetGlobalTransformWithCanvas();
        _body = canvas * (hit.Size * .5f);
        float height = (canvas * new Vector2(hit.Size.X * .5f, hit.Size.Y)).DistanceTo(canvas * new Vector2(hit.Size.X * .5f, 0));
        var screen = GetViewportRect().Size;
        float screenUnit = Mathf.Min(screen.X / 1280, screen.Y / 720);
        _unit = Mathf.Clamp(height / 190, .60f * screenUnit, 1.25f * screenUnit);
        _center = _body + new Vector2(175, -157) * _unit;
        _center.X = Mathf.Clamp(_center.X, 125 * _unit, screen.X - 150 * _unit);
        _center.Y = Mathf.Max(190 * _unit, _center.Y);
        if (!GodotObject.IsInstanceValid(_badge) || _badge!.IsQueuedForDeletion() || !_badge.IsInsideTree()
            || _badge.Model.Owner != _owner.Creature || _badge.Model is not KarenPassionPower) _badge = null;
        if (_badge == null && _age >= _badgeSearch)
        {
            _badgeSearch = _age + .15f;
            _badge = _room.FindChildren("*", "", true, false).OfType<NPower>()
                .FirstOrDefault(p => p.Model.Owner == _owner.Creature && p.Model is KarenPassionPower && p.IsVisibleInTree());
        }
        if (_badge != null) _destination = _badge.GetGlobalTransformWithCanvas() * (_badge.Size * .5f);
    }

    private Vector2 EnvelopePoint(float age)
    {
        var start = _center + Vector2.Down * (12 + Mathf.Sin(age * 2.4f) * 3) * _unit;
        if (_badge == null || age <= TravelAt) return start;
        float t = Smooth((age - TravelAt) / (ArriveAt - TravelAt));
        return start.Lerp(_destination, t) + Vector2.Up * (Mathf.Sin(t * Mathf.Pi) * 48 * _unit);
    }

    public override void _Draw()
    {
        float clothAlpha = Window(_age, 0, 2.30f, .14f, .40f);
        DrawRisingRibbon(clothAlpha);
        float reveal = Smooth((_age - .34f) / .55f);
        float travel = _badge == null ? 0 : Smooth((_age - TravelAt) / (ArriveAt - TravelAt));
        float alpha = reveal * (1 - Smooth((_age - 2.75f) / .25f));
        var point = EnvelopePoint(_age);
        float scale = _unit * Mathf.Lerp(1, .11f, travel) * (.88f + .12f * reveal);
        float bounce = Mathf.Sin(Mathf.Clamp((_age - StampAt) / .22f, 0, 1) * Mathf.Pi) * .055f;
        DrawSetTransform(point, Mathf.Lerp(-.055f, .12f, travel), Vector2.One * scale * (1 - bounce));
        DrawEnvelope(alpha);
        DrawSetTransform(Vector2.Zero);
    }

    private void DrawEnvelope(float alpha)
    {
        if (alpha <= .001f) return;
        Vector2 tl = new(-100, -58), tr = new(100, -58), br = new(100, 58), bl = new(-100, 58);
        DrawColoredPolygon([tl + new Vector2(3, 6), tr + new Vector2(3, 6), br + new Vector2(3, 6), bl + new Vector2(3, 6)], new Color(Burgundy, alpha * .28f));
        DrawColoredPolygon([tl, tr, br, bl], new Color(Paper, alpha));
        DrawColoredPolygon([tl, new(0, 14), bl], new Color(Shade.Lightened(.23f), alpha));
        DrawColoredPolygon([tr, new(0, 14), br], new Color(Shade.Lightened(.38f), alpha));
        DrawColoredPolygon([bl, new(0, -3), br], new Color(Paper.Lightened(.13f), alpha));
        float closed = Smooth((_age - 1.0f) / .38f);
        DrawColoredPolygon([tl, tr, new(0, Mathf.Lerp(-104, 13, closed))], new Color(Burgundy.Lerp(Paper.Lightened(.2f), closed), alpha));
        DrawPolyline([bl, new(0, -3), br], new Color(Shade, alpha * .6f), 1.2f, true);
        DrawPolyline([tl, new(0, Mathf.Lerp(-104, 13, closed)), tr], new Color(Gold, alpha * .85f), 1.5f, true);
        DrawPolyline([tl, tr, br, bl, tl], new Color(Gold, alpha), 2, true);
        DrawRect(new Rect2(-94, -52, 188, 104), new Color(Shade, alpha * .38f), false, .8f);
        // Small corner ornaments remain inside the paper; no text or false relic identity.
        foreach (float x in new[] { -87f, 87f })
            foreach (float y in new[] { -45f, 45f })
                DrawCircle(new Vector2(x, y), 1.7f, new Color(Gold, alpha));
        float seal = Smooth((_age - StampAt) / .14f);
        if (seal <= .001f) return;
        var sealPoint = new Vector2(0, -2);
        DrawCircle(sealPoint + new Vector2(1.5f, 3), 24, new Color(Burgundy.Darkened(.45f), alpha * seal));
        DrawCircle(sealPoint, 24 * seal, new Color(Burgundy, alpha));
        DrawCircle(sealPoint + new Vector2(-2, -3), 20 * seal, new Color(Scarlet.Darkened(.30f), alpha));
        DrawArc(sealPoint, 20 * seal, 0, Mathf.Tau, 48, new Color(Gold, alpha * .85f), 1.6f, true);
        DrawColoredPolygon(Star.Select(p => p * (15 * seal) + sealPoint).ToArray(), new Color(Gold, alpha));
        DrawLine(sealPoint + new Vector2(-3, -12), sealPoint + new Vector2(-3, 1), new Color(Hot, alpha * .8f), 1, true);
    }

    private Vector2 RibbonPoint(float t)
    {
        var origin = _body + new Vector2(8, -60) * _unit;
        var top = _center + Vector2.Up * 85 * _unit;
        var coil = origin.Lerp(top + new Vector2(70, -67) * _unit, t)
            + new Vector2(Mathf.Sin(t * Mathf.Tau * 1.2f) * 61, -Mathf.Sin(t * Mathf.Pi) * 28) * _unit;
        float at = Mathf.Clamp(t * 9.35f, 0, 9.35f);
        int i = (int)at;
        var folded = top + Star[i].Lerp(Star[(i + 1) % 10], at - i) * 70 * _unit;
        return coil.Lerp(folded, Smooth((_age - .55f) / .63f));
    }

    private void DrawRisingRibbon(float alpha)
    {
        if (alpha <= .001f) return;
        float grow = Smooth(_age / .65f);
        float collapse = Smooth((_age - 1.22f) / .25f);
        var stamp = _center + Vector2.Down * 10 * _unit;
        Vector2 lastL = default, lastR = default;
        const int steps = 90;
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps * grow;
            var p = RibbonPoint(t).Lerp(stamp, collapse);
            var tangent = (RibbonPoint(Mathf.Min(1, t + .006f)) - RibbonPoint(Mathf.Max(0, t - .006f))).Normalized();
            var n = new Vector2(-tangent.Y, tangent.X);
            float twist = Mathf.Sin(t * Mathf.Tau * 1.7f - _age * 2);
            float half = (4 + 7 * Mathf.Sin(t * Mathf.Pi)) * (.45f + .55f * Mathf.Abs(twist)) * _unit * (1 - collapse);
            var left = p - n * half; var right = p + n * half;
            if (i > 0 && collapse < .995f && left.DistanceTo(lastL) > .08f * _unit)
            {
                _quad[0] = lastL; _quad[1] = lastR; _quad[2] = right; _quad[3] = left;
                var face = twist < -.15f ? Burgundy : Scarlet.Lerp(new Color("#f99177"), Mathf.Pow(Mathf.Max(0, twist), 4) * .7f);
                // Twisting strips can cross at a fold; native quad primitives don't triangulate a concave polygon.
                DrawPrimitive(_quad, [new Color(face, alpha * (1 - collapse))], Array.Empty<Vector2>());
                DrawLine(lastL, left, new Color(Gold, alpha * (1 - collapse)), 1.25f * _unit, true);
                DrawLine(lastR, right, new Color(Gold, alpha * .70f * (1 - collapse)), .9f * _unit, true);
                DrawLine((lastL + lastR) * .5f, p, new Color(Hot, alpha * .12f * Mathf.Max(0, twist) * (1 - collapse)), 2 * _unit, true);
            }
            lastL = left; lastR = right;
        }
        // Two broad silk tails curl behind the sealed envelope, then release at the travel cue.
        float loops = Window(_age, .95f, 2.34f, .36f, .3f);
        if (loops <= 0) return;
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 tailL = default, tailR = default;
            Vector2 Tail(float t) => _center + new Vector2(side * (Mathf.Sin(t * Mathf.Pi * 1.8f) * 148 + 10),
                -Mathf.Sin(t * Mathf.Tau) * 48 + t * 80) * _unit;
            for (int i = 0; i <= 60; i++)
            {
                float t = i / 60f;
                var p = Tail(t);
                var n = (Tail(Mathf.Min(1, t + .004f)) - Tail(Mathf.Max(0, t - .004f))).Normalized().Orthogonal();
                float sheen = Mathf.Sin(t * 10 + _age * .6f);
                float half = (7 + Mathf.Sin(t * Mathf.Pi) * 6) * _unit * Mathf.Min(1, (1 - t) * 9) * (.65f + .35f * Mathf.Abs(sheen));
                var left = p - n * half; var right = p + n * half;
                if (i > 0)
                {
                    _quad[0] = tailL; _quad[1] = tailR; _quad[2] = right; _quad[3] = left;
                    var face = Scarlet.Lerp(Burgundy, .45f - .40f * sheen);
                    DrawPrimitive(_quad, [new Color(face, loops)], Array.Empty<Vector2>());
                    DrawLine(tailL, left, new Color(Gold, loops * .85f), _unit, true);
                    DrawLine(tailR, right, new Color(Gold, loops * .65f), .8f * _unit, true);
                    DrawLine(tailL.Lerp(tailR, .35f), left.Lerp(right, .35f), new Color(Hot, loops * .17f * Mathf.Max(0, sheen)), 2.5f * _unit, true);
                }
                tailL = left; tailR = right;
            }
        }
    }

    private void DrawLight()
    {
        float stamp = Window(_age, StampAt, StampAt + .47f, .035f, .34f);
        if (stamp > 0)
        {
            var point = _center + Vector2.Down * 10 * _unit;
            Glow(point, 70 * _unit, Gold, stamp * .55f);
            DrawGlint(point, 47 * _unit, stamp);
        }
        for (int i = 0; i < 20; i++)
        {
            float at = i < 8 ? i * .065f : StampAt + (i - 8) * .009f;
            float t = (_age - at) / (i < 8 ? .85f : .65f);
            if (t <= 0 || t >= 1) continue;
            float angle = i * 2.39996f;
            var origin = i < 8 ? RibbonPoint((i + 1) / 9f) : _center;
            var p = origin + new Vector2(Mathf.Cos(angle) * (17 + t * 65), Mathf.Sin(angle) * (15 + t * 55) - t * 25) * _unit;
            DrawGlint(p, (i % 4 == 0 ? 5 : 2.4f) * _unit, Mathf.Sin(t * Mathf.Pi) * .8f);
        }
        if (_badge != null && _age > TravelAt && _age < ArriveAt + .2f)
        {
            for (int i = 1; i <= 14; i++)
            {
                float past = _age - i * .015f;
                if (past < TravelAt || past > ArriveAt) continue;
                var p = EnvelopePoint(past);
                var next = EnvelopePoint(past + .015f);
                _light.DrawLine(p, next, new Color(Scarlet, (1 - i / 15f) * .45f), (9 - i * .4f) * _unit, true);
                _light.DrawLine(p, next, new Color(Gold, (1 - i / 15f) * .8f), 1.1f * _unit, true);
            }
            float arrive = Window(_age, ArriveAt - .04f, EndAt, .07f, .26f);
            Glow(_destination, 23 * _unit, Gold, arrive * .45f);
            DrawGlint(_destination, 15 * _unit, arrive);
        }
    }

    private static float Smooth(float t) { t = Mathf.Clamp(t, 0, 1); return t * t * (3 - 2 * t); }
    private static float Window(float t, float start, float end, float fadeIn, float fadeOut)
        => Smooth((t - start) / fadeIn) * (1 - Smooth((t - (end - fadeOut)) / fadeOut));
    private void DrawGlint(Vector2 p, float radius, float alpha)
    {
        _light.DrawLine(p - Vector2.Up * radius, p + Vector2.Up * radius, new Color(Hot, alpha), .9f * _unit, true);
        _light.DrawLine(p - Vector2.Right * radius * .45f, p + Vector2.Right * radius * .45f, new Color(Gold, alpha), .8f * _unit, true);
    }
    private void Glow(Vector2 point, float radius, Color color, float alpha)
        => _light.DrawTextureRect(_soft!, new Rect2(point - Vector2.One * radius, Vector2.One * radius * 2), false, new Color(color, alpha));
    private void Sound(string file, float volume)
    {
        var sound = KarenAudioManager.PlaySfxOnGameBus(file, volume, Mathf.Min(_speed, 1.12f));
        if (sound != null) _sounds.Add(sound);
    }
    public override void _ExitTree()
    {
        KarenVfxComposition.RemoveCue(this);
        foreach (var sound in _sounds)
            if (GodotObject.IsInstanceValid(sound)) { sound.Stop(); sound.QueueFree(); }
        _sounds.Clear(); _badge = null;
    }
}
