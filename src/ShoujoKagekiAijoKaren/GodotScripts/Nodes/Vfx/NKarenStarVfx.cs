using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>Gather, spend a visible constellation on actual replays, and deliver the confirmed reward.</summary>
public partial class NKarenStarVfx : Node2D
{
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

    private sealed record Pulse(int Index, int Count, float At) { public bool Shown; }
    private static readonly Color Gold = new("#ffd58a"), Hot = new("#fff1d6"), Rose = new("#fa8c9f"), Blue = new("#8fcff6");
    private readonly Vector2[] _border = new Vector2[5];
    private readonly List<Pulse> _pulses = new(4);
    private readonly Func<float, Vector2> _streamPath;
    private Vector2 _streamFrom, _streamTo, _body, _crest, _stageBodyNormalized, _stageCrestNormalized;
    private float _streamLift, _unit, _timeScale;
    private NCombatRoom _room = null!;
    private Playback _playback = null!;
    private float _age, _pulseAt = -1, _completedAt = -1, _releaseAt = -1, _rewardAt = -1;
    private int _count, _played;
    private bool _hasStage;
    public int PlannedCount => _count;
    public int PlayedCount => _played;
    public bool RewardConfirmed => _playback.Reward;
    public int CardFrameDraws { get; private set; }
    public int RewardFrameDraws { get; private set; }
    public int GatherFrames { get; private set; }
    public int ConstellationFrames { get; private set; }
    public int ReplayPulseFrames { get; private set; }
    public int ReplayPulseCount { get; private set; }
    public int RewardStoryFrames { get; private set; }
    public NKarenStarVfx() => _streamPath = t => Flight(_streamFrom, _streamTo, t, _streamLift);
    private static bool Enabled => KarenVfxQuality.Enabled && !NonInteractiveMode.IsActive
        && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant;
    public static Task WaitForGather() => Enabled ? Cmd.CustomScaledWait(.294f, .42f) : Task.CompletedTask;
    public static Playback Play(CardModel card, int shine)
    {
        var playback = new Playback(card, shine);
        if (!Enabled || NCombatRoom.Instance is not {} room) return playback;
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(room) || NCombatRoom.Instance != room || playback.Completed
                || !CombatManager.Instance.IsInProgress || room.GetCreatureNode(card.Owner.Creature) == null) return;
            room.CombatVfxContainer.AddChild(new NKarenStarVfx {
                Name = "KarenStarConstellation", _room = room, _playback = playback, _count = playback.Shine
            });
        }).CallDeferred();
        return playback;
    }
    public override void _Ready()
    {
        TopLevel = true; ZIndex = 30;
        _timeScale = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? .7f : 1;
        KarenLightRibbon.PrepareResources();
        Place(); KarenVfxQuality.Redraw(this);
    }
    private void Place()
    {
        GlobalTransform = GetCanvasTransform().AffineInverse();
        var screen = GetViewportRect().Size; _unit = screen.Y / 720f;
        var card = _playback.Card.Pile?.Type is PileType.Hand or PileType.Play ? NCard.FindOnTable(_playback.Card) : null;
        if (GodotObject.IsInstanceValid(card) && card!.Model == _playback.Card && card.IsInsideTree()
            && card.IsVisibleInTree() && !card.IsQueuedForDeletion())
        {
            if (!_hasStage && _room.GetCreatureNode(_playback.Card.Owner.Creature) is {} actor)
            {
                _stageBodyNormalized = (actor.Hitbox.GetGlobalTransformWithCanvas() * (actor.Hitbox.Size * .5f)) / screen;
                // Leave clear space above the full play-card frame, including the
                // lowest stars and their glow. Resolve the
                // native destination once, so entrance/replay tweens never move the stars.
                var frame = card.GetNode<Control>("%Frame");
                var localTop = card.GetGlobalTransformWithCanvas().AffineInverse()
                    * (frame.GetGlobalTransformWithCanvas() * new Vector2(frame.Size.X * .5f, 0));
                var restingTop = PileType.Play.GetTargetPosition(card) + card.PivotOffset + (localTop - card.PivotOffset) * .8f;
                var top = _room.Ui.PlayContainer.GetGlobalTransformWithCanvas() * restingTop;
                _stageCrestNormalized = new Vector2(Mathf.Clamp(top.X, 175 * _unit, screen.X - 175 * _unit),
                    MathF.Max(85 * _unit, top.Y - 70 * _unit)) / screen;
                _hasStage = true;
            }
        }
        _body = _stageBodyNormalized * screen;
        _crest = _stageCrestNormalized * screen;
    }
    public override void _Process(double delta)
    {
        var owner = _playback.Card.Owner.Creature;
        if (!Enabled || !GodotObject.IsInstanceValid(_room) || NCombatRoom.Instance != _room
            || !_room.IsInsideTree() || !CombatManager.Instance.IsInProgress || owner.IsDead)
        { Hide(); QueueFree(); return; }
        _age += (float)delta / _timeScale; Place();
        while (_playback.Events.TryDequeue(out var play))
        {
            _count = Math.Max(1, play.Count); _played = Math.Max(_played, play.Index + 1); _pulseAt = _age;
            _pulses.Add(new Pulse(play.Index, _count, _age));
        }
        for (int i = _pulses.Count - 1; i >= 0; i--) if (_age - _pulses[i].At > .34f) _pulses.RemoveAt(i);
        if (_playback.Completed && _completedAt < 0) _completedAt = _age;
        if (_playback.Reward && _rewardAt < 0 && KarenPowerVfxAnchor.Find(_room, owner.GetPower<KarenShineCardRewardPower>()) != null) _rewardAt = _age;
        if (_completedAt >= 0 && _releaseAt < 0 && !ShoujoKageki.Core.V1.Shine.IsDepletionAnimating(_playback.Card)) _releaseAt = _age;
        if ((_releaseAt >= 0 && _age - _releaseAt >= .52f && (_rewardAt < 0 || _age - _rewardAt >= .44f))
            || (_completedAt >= 0 && _age - _completedAt > 1.6f)) { Hide(); QueueFree(); return; }
        KarenVfxQuality.Redraw(this);
    }
    private static float Smooth(float t) => Mathf.SmoothStep(0, 1, t);
    private static Vector2 Flight(Vector2 from, Vector2 to, float t, float lift)
        => from.Lerp(to, t) + Vector2.Up * MathF.Sin(t * MathF.PI) * lift;
    private Vector2 Slot(float t) => _crest + new Vector2(-160 + 320 * t, 24 - 35 * MathF.Sin(t * MathF.PI)) * _unit;
    private float PulseAtSlot(int slot, int slots)
    {
        float strength = 0;
        foreach (var pulse in _pulses)
        {
            int target = Math.Min(slots - 1, pulse.Index * slots / Math.Max(1, pulse.Count));
            if (target != slot) continue;
            float value = 1 - Smooth((_age - pulse.At) / .30f);
            strength = MathF.Max(strength, value);
            if (value > 0 && !pulse.Shown) { pulse.Shown = true; ReplayPulseCount++; }
        }
        return strength;
    }
    private void Stream(Vector2 from, Vector2 to, float lift, float head, float length, Color color, float alpha, float width = 3)
    {
        _streamFrom = from; _streamTo = to; _streamLift = lift;
        KarenLightRibbon.Draw(this, _streamPath, head, length, width * _unit, color, alpha);
    }
    public override void _Draw()
    {
        float alpha = Smooth(_age / .10f) * (_releaseAt < 0 ? 1 : 1 - Smooth((_age - _releaseAt) / .52f));
        if (_hasStage && alpha > 0)
        {
            if (_age < .55f)
            {
                float t = Smooth(_age / .36f), fade = 1 - Smooth((_age - .36f) / .19f);
                for (int side = -1; side <= 1; side += 2)
                {
                    var from = _body + new Vector2(side * 28, -4) * _unit;
                    var to = Slot(.5f) + new Vector2(side * 3, 0) * _unit;
                    var tint = side < 0 ? Rose : Blue;
                    Stream(from, to, 26 * _unit, t + Smooth((_age - .36f) / .19f), .42f, tint, alpha * fade * .8f, 3.4f);
                    KarenLightRibbon.Star(this, Flight(from, to, t, 26 * _unit), 11 * _unit * (1 - Smooth((t - .7f) / .3f) * .8f), tint, alpha * fade);
                }
                GatherFrames++;
            }
            // Keep the finite constellation, with one shallow open arc and clear star silhouettes.
            Stream(Slot(0), Slot(1), 35 * _unit, Smooth((_age - .08f) / .30f), 1, Gold, alpha * .28f, 1.8f);
            int slots = Math.Min(12, _count);
            for (int i = 0; i < slots; i++)
            {
                float t = slots == 1 ? .5f : i / (float)(slots - 1);
                float reveal = Smooth((_age - .08f - t * .12f) / .18f);
                float spent = Mathf.Clamp((_played - i * (_count / (float)slots)) / (_count / (float)slots), 0, 1);
                float pulse = PulseAtSlot(i, slots);
                float radius = (9 + 2 * MathF.Sin(t * MathF.PI)) * _unit * Mathf.Lerp(1 - spent * .22f, 1, pulse);
                KarenLightRibbon.Star(this, Slot(t), radius, Gold.Lerp(Hot, pulse),
                    alpha * reveal * Mathf.Lerp(1 - spent * .78f, 1, pulse));
                if (pulse > 0)
                {
                    // A local glint contracts into the spent star; it never leaves its slot.
                    var span = Vector2.Right * (15 * _unit * pulse);
                    DrawLine(Slot(t) - span, Slot(t) + span, new Color(Hot, alpha * pulse * .85f), 1.8f * _unit, true);
                    ReplayPulseFrames++;
                }
            }
            ConstellationFrames++;
            // The gathering meets the constellation itself; no second core is stacked over its middle star.
        }
        var card = _playback.Card.Pile?.Type is PileType.Hand or PileType.Play ? NCard.FindOnTable(_playback.Card) : null;
        if (GodotObject.IsInstanceValid(card) && card!.Model == _playback.Card && card.IsInsideTree() && card.IsVisibleInTree()
            && !card.IsQueuedForDeletion() && card.GetNodeOrNull<Control>("%Frame") is {} frame)
        {
            float pulse = _pulseAt < 0 ? 0 : MathF.Max(0, 1 - (_age - _pulseAt) / .13f);
            Outline(frame, new Color(Gold, pulse * .4f), 1.5f * _unit); CardFrameDraws++;
        }
        if (_rewardAt < 0) return;
        var badge = KarenPowerVfxAnchor.Find(_room, _playback.Card.Owner.Creature.GetPower<KarenShineCardRewardPower>());
        if (badge == null) return;
        float elapsed = _age - _rewardAt, rewardAlpha = 1 - Smooth(elapsed / .44f);
        var target = badge.GetGlobalTransformWithCanvas() * (badge.Size * .5f);
        if (_hasStage && rewardAlpha > 0)
        {
            float t = Smooth(elapsed / .32f);
            Stream(_crest, target, 24 * _unit, t, .45f, Gold, alpha * rewardAlpha, 3.3f);
            KarenLightRibbon.Star(this, Flight(_crest, target, t, 24 * _unit), 10 * _unit, Hot, alpha * rewardAlpha);
            for (int i = 0; i < 20; i += KarenVfxQuality.ParticleStride)
            {
                float q = Mathf.Clamp((elapsed - i * .004f) / .42f, 0, 1);
                var v = new Vector2(MathF.Sin(i * 2.4f) * (18 + i), -20 - i * 2) * q * _unit;
                KarenLightRibbon.Star(this, _crest + v, (1.4f + i % 3) * _unit, Gold, alpha * MathF.Sin(q * MathF.PI) * .5f);
            }
            RewardStoryFrames++;
        }
        Outline(badge, new Color(Gold, rewardAlpha), 1.8f * _unit); RewardFrameDraws++;
    }
    private void Outline(Control target, Color color, float width)
    {
        if (color.A <= 0) return;
        var t = target.GetGlobalTransformWithCanvas(); var size = target.Size;
        _border[0] = t * Vector2.Zero; _border[1] = t * new Vector2(size.X, 0);
        _border[2] = t * size; _border[3] = t * new Vector2(0, size.Y); _border[4] = _border[0];
        DrawPolyline(_border, color, width, true);
    }
    public override void _ExitTree() => _pulses.Clear();
}
