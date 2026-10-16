using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
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
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem.Nodes;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Double-star clasp and a finite constellation, spent by native replay events.</summary>
public partial class NKarenStarVfx : Node2D
{
    // Commands may resume off the scene thread. Only this node touches Godot objects;
    // the managed scope transports results and guarantees release even on failure.
    public sealed class Playback(CardModel card, int shine) : IDisposable
    {
        internal readonly CardModel Card = card;
        internal readonly int Shine = Math.Max(1, shine);
        internal readonly ConcurrentQueue<(int Index, int Count)> Events = new();
        private int _completed, _reward;
        internal bool Completed => Volatile.Read(ref _completed) != 0;
        internal bool Reward => Volatile.Read(ref _reward) != 0;
        public void BeforePlay(CardPlay play)
        {
            if (play.Card == Card && !Completed) Events.Enqueue((play.PlayIndex, play.PlayCount));
        }
        public void ConfirmReward() => Interlocked.Exchange(ref _reward, 1);
        public void Dispose() => Interlocked.Exchange(ref _completed, 1);
    }

    private sealed record Pulse(int Index, int Count, float At);
    private static readonly Color Gold = new("#ffd58a"), Hot = new("#fff5d9"), Rose = new("#fa8c9f"), Blue = new("#8fcff6");
    // Triangulate in stable local coordinates, then scale the draw transform.
    // Tiny world-space stars during the first frame can fall below triangulation epsilon.
    private static readonly Vector2[] StarShape = [new(0, -1.35f), new(.1273f, -.1718f), new(1, 0), new(.1273f, .1718f),
        new(0, 1.35f), new(-.1273f, .1718f), new(-1, 0), new(-.1273f, -.1718f)];
    private static GradientTexture2D? _soft;
    private NCombatRoom _room = null!;
    private Playback _playback = null!;
    private readonly Node2D _back = new() { Name = "KarenStarBackLight" };
    private readonly List<Pulse> _pulses = new();
    private readonly List<AudioStreamPlayer> _sounds = new();
    private Vector2 _screen, _body, _foot, _clasp, _cardPoint, _rewardPoint;
    private float _unit, _age, _releaseAt = -1, _rewardAt = -1, _lastSoundAt = -1;
    private int _count, _played;
    private float _speed;
    public int PlannedCount => _count;
    public int PlayedCount => _played;
    public bool RewardConfirmed => _rewardAt >= 0;
    public bool Holding => _releaseAt < 0;
    public Node2D BackgroundLight => _back;
    private float Fade => Smooth(_age / .24f) * (_releaseAt < 0 ? 1 : 1 - Smooth((_age - _releaseAt) / .7f));
    private static bool Enabled => !NonInteractiveMode.IsActive && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant;
    public static Task WaitForGather() => Enabled ? Cmd.CustomScaledWait(.38f, .62f) : Task.CompletedTask;

    public static Playback Play(CardModel card, int shine)
    {
        var playback = new Playback(card, shine);
        if (!Enabled || NCombatRoom.Instance is not {} room) return playback;
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(room) || NCombatRoom.Instance != room || playback.Completed
                || !CombatManager.Instance.IsInProgress || room.GetCreatureNode(card.Owner.Creature) == null) return;
            room.CombatVfxContainer.AddChild(new NKarenStarVfx {
                Name = "KarenStarConstellation", _room = room, _playback = playback,
                _count = playback.Shine, _speed = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? 1.6f : 1f
            });
        }).CallDeferred();
        return playback;
    }

    public override void _Ready()
    {
        _soft ??= new GradientTexture2D { Width = 128, Height = 128, Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new(.5f, .5f), FillTo = new(1, .5f), Gradient = new Gradient {
                Offsets = [0, .18f, .5f, 1], Colors = [Colors.White, new(1,1,1,.55f), new(1,1,1,.1f), Colors.Transparent] } };
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        _back.Material = Material;
        TextureFilter = _back.TextureFilter = TextureFilterEnum.Linear;
        _room.BackCombatVfxContainer.AddChild(_back);
        _back.Draw += DrawLights;
        KarenVfxComposition.RegisterCue(this, _playback.Card.Owner.Creature);
        Place();
        Sound("karen_star_gather.wav", .52f);
    }

    public override void _Process(double delta)
    {
        var creature = _playback.Card.Owner.Creature;
        if (!GodotObject.IsInstanceValid(_room) || NCombatRoom.Instance != _room || !_room.IsInsideTree()
            || creature.IsDead || _room.GetCreatureNode(creature) == null
            || (!CombatManager.Instance.IsInProgress && _releaseAt < 0))
        { QueueFree(); return; }
        _age += (float)delta * _speed;
        Place();
        while (_playback.Events.TryDequeue(out var play))
        {
            _count = Math.Max(1, play.Count);
            _played = Math.Max(_played, play.Index + 1);
            _pulses.Add(new(play.Index, _count, _age));
            if (_age - _lastSoundAt > .11f)
            {
                Sound("karen_star_step.wav", .2f, .92f + .26f * (play.Index / (float)_count));
                _lastSoundAt = _age;
            }
        }
        _pulses.RemoveAll(p => _age - p.At > .55f);
        if (_rewardAt < 0 && _playback.Reward)
        {
            _rewardAt = _age;
            var badge = KarenPowerVfxAnchor.Find(_room, creature.GetPower<KarenShineCardRewardPower>());
            // Confirm the real reward marker; no invented reward slot in the HUD.
            _rewardPoint = badge != null ? badge.GetGlobalTransformWithCanvas() * (badge.Size * .5f) : _foot + new Vector2(28, 26) * _unit;
            Sound("karen_star_resolve.wav", .48f);
        }
        if (_releaseAt < 0 && _playback.Completed && !NKarenShineDepleteVfx.IsAnimating(_playback.Card)) _releaseAt = _age;
        if (_releaseAt >= 0 && _age - _releaseAt > .72f) { QueueFree(); return; }
        KarenVfxComposition.SetCueStrength(this, Fade * .8f);
        _back.Modulate = new Color(1, 1, 1, KarenVfxComposition.EventWeight(creature, this));
        QueueRedraw(); _back.QueueRedraw();
    }

    private void Place()
    {
        GlobalTransform = GetCanvasTransform().AffineInverse();
        _back.GlobalTransform = _back.GetCanvasTransform().AffineInverse();
        _screen = GetViewport().GetVisibleRect().Size;
        float screenUnit = Math.Min(_screen.X / 1280, _screen.Y / 720);
        var hit = _room.GetCreatureNode(_playback.Card.Owner.Creature)!.Hitbox;
        _body = hit.GetGlobalTransformWithCanvas() * (hit.Size * .5f);
        _foot = hit.GetGlobalTransformWithCanvas() * new Vector2(hit.Size.X * .5f, hit.Size.Y);
        _unit = Mathf.Clamp((_foot.Y - _body.Y) / 95, .65f, 1.25f) * screenUnit;
        _clasp = new Vector2(Mathf.Clamp(_body.X + 85 * _unit, 100 * screenUnit, _screen.X - 260 * screenUnit),
            Mathf.Max(110 * screenUnit, _body.Y - 175 * _unit));
        var card = NCard.FindOnTable(_playback.Card);
        if (card != null && card.IsInsideTree() && card.Model == _playback.Card)
        {
            var portrait = card.GetNode<TextureRect>("%Portrait");
            _cardPoint = portrait.GetGlobalTransformWithCanvas() * (portrait.Size * .5f);
        }
        else if (_cardPoint == Vector2.Zero) _cardPoint = _body + new Vector2(150, -25) * _unit;
    }

    private static float Smooth(float t) { t = Mathf.Clamp(t, 0, 1); return t * t * (3 - 2 * t); }
    private Vector2 Arc(float t) => _clasp + new Vector2(-140 + 405 * t, 72 - 128 * Mathf.Sin(t * Mathf.Pi)) * _unit;
    private static Vector2 Curve(Vector2 a, Vector2 b, float t, float lift) => a.Lerp(b, t) + Vector2.Up * Mathf.Sin(t * Mathf.Pi) * lift;
    private void Glow(Node2D node, Vector2 point, Vector2 size, Color color, float alpha)
    {
        color.A *= Mathf.Clamp(alpha, 0, 1);
        node.DrawTextureRect(_soft, new Rect2(point - size * _unit / 2, size * _unit), false, color);
    }
    private void Star(Node2D node, Vector2 point, float size, Color color, float alpha)
    {
        if (alpha <= 0 || size < .05f) return;
        color.A *= Mathf.Clamp(alpha, 0, 1);
        Glow(node, point, Vector2.One * size * 6, color, alpha * .2f);
        node.DrawSetTransform(point, 0, Vector2.One * size * _unit);
        node.DrawColoredPolygon(StarShape, color);
        node.DrawSetTransform(Vector2.Zero);
    }
    private void Ribbon(Node2D node, Func<float, Vector2> path, Color color, float alpha, float progress = 1)
    {
        Vector2 prev = path(0);
        for (int i = 1; i <= 40; i++) {
            float t = i / 40f * progress; var point = path(t);
            var c = color; c.A = alpha * Mathf.Sin((i - .5f) / 40 * Mathf.Pi);
            var haze = c; haze.A *= .12f;
            node.DrawLine(prev, point, haze, 6 * _unit, true);
            node.DrawLine(prev, point, c, 1.1f * _unit, true); prev = point;
        }
    }
    private void DrawLights()
    {
        float alpha = Fade;
        // Two narrow, feathered followspots behind the real actor.
        for (int side = -1; side <= 1; side += 2) {
            var top = _clasp + new Vector2(side * 165, -190) * _unit;
            var bottom = _foot + new Vector2(side * 12, 0) * _unit;
            for (int band = 9; band >= 0; band--) {
                float width = (10 + band * 5) * _unit;
                var c = side < 0 ? Gold : Blue; c.A = alpha * .009f;
                _back.DrawPolygon([top + new Vector2(-2 * _unit, 0), top + new Vector2(2 * _unit, 0),
                    bottom + new Vector2(width, 0), bottom - new Vector2(width, 0)], [c, c, new(c, 0), new(c, 0)]);
            }
        }
        Glow(_back, _foot, new Vector2(240, 32), Gold, alpha * .35f);
        Glow(_back, _clasp, new Vector2(200, 260), Gold, alpha * .12f);
        float rise = Smooth(_age / .6f);
        for (int side = -1; side <= 1; side += 2) {
            var from = _body + new Vector2(side * 22, 12) * _unit;
            Ribbon(_back, t => from.Lerp(_clasp + new Vector2(side * 13, 8) * _unit, t)
                + new Vector2(Mathf.Sin(t * Mathf.Tau + _age * .65f) * side * 24, 0) * _unit,
                side < 0 ? Rose : Blue, alpha * .55f, rise);
        }
    }

    public override void _Draw()
    {
        float alpha = Fade, open = Smooth((_age - .14f) / .55f);
        Ribbon(this, Arc, Gold, alpha * .45f, open);
        int slots = Math.Min(12, _count);
        for (int i = 0; i < slots; i++) {
            float t = slots == 1 ? .5f : i / (float)(slots - 1);
            float reveal = Smooth((_age - .18f - t * .3f) / .25f);
            float perSlot = _count / (float)slots;
            float spent = Mathf.Clamp((_played - i * perSlot) / perSlot, 0, 1);
            Star(this, Arc(t), 6.5f + Mathf.Sin(t * Mathf.Pi) * 2, spent >= 1 ? Gold : Hot, alpha * reveal * (1 - spent * .88f));
        }
        foreach (var pulse in _pulses) {
            float t = Mathf.Clamp((_age - pulse.At) / .36f, 0, 1);
            var start = Arc(pulse.Count <= 1 ? .5f : pulse.Index / (float)(pulse.Count - 1));
            for (int i = 14; i >= 0; i--) {
                float q = t - i * .016f; if (q < 0) continue;
                var point = Curve(start, _cardPoint, Smooth(q), 40 * _unit);
                Glow(this, point, Vector2.One * (13 - i * .55f), Gold, alpha * (1 - i / 15f) * (1 - t) * .7f);
            }
            Star(this, Curve(start, _cardPoint, Smooth(t), 40 * _unit), 8, Hot, alpha * (1 - Smooth((t - .78f) / .22f)));
            Glow(this, _cardPoint, new Vector2(110, 150), Gold, alpha * (1 - t) * .13f);
        }
        float flare = _rewardAt < 0 ? 0 : Mathf.Sin(Mathf.Clamp((_age - _rewardAt) / .65f, 0, 1) * Mathf.Pi);
        float gather = Smooth(_age / .5f);
        Star(this, _clasp, (21 + 12 * flare) * gather, Gold, alpha);
        Star(this, _clasp, (14 + 8 * flare) * gather, Hot, alpha * .9f);
        Star(this, _clasp + new Vector2(36, 31) * _unit, (11 + 7 * flare) * gather, Hot, alpha);
        if (_rewardAt >= 0) {
            float t = Smooth((_age - _rewardAt) / .56f);
            var end = _rewardPoint;
            Ribbon(this, q => Curve(_clasp, end, q * t, 50 * _unit), Gold, alpha * (1 - t) * .7f);
            Star(this, Curve(_clasp, end, t, 50 * _unit), 7, Hot, alpha * (1 - t));
            for (int i = 0; i < 20; i++) {
                float q = Mathf.Clamp((_age - _rewardAt - i * .008f) / .8f, 0, 1);
                var v = new Vector2(Mathf.Sin(i * 2.4f) * (18 + i * 1.4f), -30 - i * 3) * q * _unit;
                Star(this, _clasp + v, 1.4f + i % 3, Gold, alpha * Mathf.Sin(q * Mathf.Pi) * .55f);
            }
        }
    }

    private void Sound(string file, float volume, float pitch = 1)
    {
        var sound = KarenAudioManager.PlaySfxOnGameBus(file, volume, pitch * Math.Min(_speed, 1.15f));
        if (sound != null) _sounds.Add(sound);
    }
    public override void _ExitTree()
    {
        KarenVfxComposition.RemoveCue(this);
        if (GodotObject.IsInstanceValid(_back)) { _back.Hide(); _back.QueueFree(); }
        foreach (var sound in _sounds) if (GodotObject.IsInstanceValid(sound)) { sound.Stop(); sound.QueueFree(); }
        _sounds.Clear(); _pulses.Clear();
    }
}
