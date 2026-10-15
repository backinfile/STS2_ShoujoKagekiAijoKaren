using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenPromiseStarNode
{
    private static readonly Color RecallGold = new("#ffd18a");
    private static readonly (float Y, float HalfWidth)[] RecallDecks =
        [(1310, 247), (1140, 179), (850, 93), (708, 90), (391, 38)];
    private readonly List<NKarenTowerRecallCardVfx> _recallCards = new();
    private readonly List<TransferResolution> _transferResolutions = new();
    private CardModel? _recallPreviewCard;
    private PromiseTransferPreview _transferPreview;
    private float _recallPreviewAlpha;
    private float _transferPreviewStarted;
    private float _recallStarted = -100;
    private float _recallTailUntil = -100;
    private float _recallChargeDuration;
    private float _recallSendPulse;
    private bool _recallCanDraw;
    private int _recallDepth;
    private int _recallEpoch;

    public void SetTransferPreview(CardModel card, bool selected)
    {
        if (selected && LocalContext.IsMe(card.Owner))
        {
            if (_recallPreviewCard != card) _transferPreviewStarted = _time;
            _recallPreviewCard = card;
            _transferPreview = PromiseTransferPreviews.ForCard(card);
        }
        else if (_recallPreviewCard == card) _recallPreviewCard = null;
    }

    public IDisposable BeginTowerRecall(bool canDraw)
    {
        EndCombatEntrance();
        if (_recallDepth++ == 0)
        {
            _recallStarted = _time;
            _recallCanDraw = canDraw;
            _recallChargeDuration = SaveManager.Instance?.PrefsSave.FastMode switch
            {
                FastModeType.Instant => 0.01f,
                FastModeType.Fast => 0.14f,
                _ => 0.32f
            };
        }
        return new RecallBatch(this, _recallEpoch);
    }

    public IDisposable BeginTransferResolution(CardModel card)
    {
        var scope = new TransferResolution(this, PromiseTransferPreviews.ForCard(card));
        _transferResolutions.Add(scope);
        return scope;
    }

    // Individual scopes preserve an outer card's light when a nested play ends.
    private sealed class TransferResolution(NKarenPromiseStarNode node, PromiseTransferPreview profile) : IDisposable
    {
        public PromiseTransferPreview Profile { get; } = profile;
        public void Dispose()
        {
            if (GodotObject.IsInstanceValid(node)) node._transferResolutions.Remove(this);
        }
    }

    private sealed class RecallBatch(NKarenPromiseStarNode node, int epoch) : IDisposable
    {
        private bool _disposed;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (!GodotObject.IsInstanceValid(node) || node._recallEpoch != epoch) return;
            node._recallDepth = Math.Max(0, node._recallDepth - 1);
            if (node._recallDepth == 0) node._recallTailUntil = node._time + 0.42f;
        }
    }

    private bool TrackRecallCard(NCard card)
    {
        if (_recallDepth <= 0 || !_recallCanDraw) return false;
        _recallSendPulse = 1;
        // A peer's hidden hand uses native intent transfers, not the local hand slots.
        if (card.Model is not { } model || !LocalContext.IsMe(model.Owner)) return false;
        var room = NCombatRoom.Instance;
        if (room == null) return false;
        var effect = new NKarenTowerRecallCardVfx(card, model);
        room.Ui.AddChild(effect);
        _recallCards.Add(effect);
        return true;
    }

    private void UpdateTowerRecall(float dt)
    {
        if (_recallPreviewCard is { } card && (card.Pile?.Type != PileType.Hand
            || NCombatRoom.Instance?.Ui.Hand.InCardPlay != true)) _recallPreviewCard = null;
        if (_transferResolutions.Count > 0) _transferPreview = _transferResolutions[^1].Profile;
        else if (_recallPreviewCard != null) _transferPreview = PromiseTransferPreviews.ForCard(_recallPreviewCard);
        _recallPreviewAlpha = Mathf.MoveToward(_recallPreviewAlpha,
            _recallPreviewCard != null || _transferResolutions.Count > 0 ? 1 : 0, dt * 8);
        _recallSendPulse = Mathf.MoveToward(_recallSendPulse, 0, dt * 4);
        _recallCards.RemoveAll(effect => !GodotObject.IsInstanceValid(effect) || effect.IsQueuedForDeletion());
    }

    private void ClearTowerRecall()
    {
        _recallEpoch++;
        _transferResolutions.Clear();
        _recallPreviewCard = null;
        _recallPreviewAlpha = _recallSendPulse = 0;
        _recallDepth = 0;
        _recallStarted = _recallTailUntil = -100;
        foreach (var effect in _recallCards)
            if (GodotObject.IsInstanceValid(effect)) effect.QueueFree();
        _recallCards.Clear();
    }

    private static float RecallPulse(float age, float start, float duration)
    {
        float t = (age - start) / duration;
        return t is < 0 or > 1 ? 0 : Mathf.Sin(t * Mathf.Pi);
    }

    private void DrawTowerRecall(float alpha)
    {
        if (IsVoidPresentation) return;
        float age = _time - _recallStarted;
        float batch = _recallDepth > 0 ? 1 : Mathf.Clamp((_recallTailUntil - _time) / 0.42f, 0, 1);
        if (_recallPreviewAlpha > 0) DrawTransferPreview(alpha * _recallPreviewAlpha);
        if (batch <= 0) return;
        if (!_recallCanDraw && _recallDepth == 0) batch *= 1 - Mathf.Clamp(age / 0.36f, 0, 1);
        if (!_recallCanDraw && batch > 0)
        {
            DrawStar(PaintedSource, 10, RecallGold, alpha * RecallPulse(age, 0, 0.34f) * 0.55f);
            batch = 0;
        }
        float charge = Mathf.Clamp(age / Mathf.Max(0.01f, _recallChargeDuration), 0, 1);
        float runningFade = batch * (1 - Mathf.Clamp((age - _recallChargeDuration) / 0.3f, 0, 1));
        for (int side = -1; side <= 1; side += 2)
        {
            var leg = side < 0 ? PaintedLeftLeg : PaintedRightLeg;
            for (int i = 1; i < leg.Length; i++)
                DrawLine(PaintedPoint(leg[i - 1]), PaintedPoint(leg[i]),
                    new Color(RecallGold, alpha * batch * 0.13f), 3, true);
            float progress = charge;
            float strength = alpha * runningFade;
            for (int i = 12; i >= 0; i--)
            {
                float t = progress - i * 0.032f;
                if (t < 0) continue;
                var p = ReplenishPath(side, t);
                var from = ReplenishPath(side, Mathf.Max(0, t - 0.032f));
                float light = strength * (1 - i / 13f);
                DrawLine(from, p, new Color(RecallGold, light * 0.12f), 15, true);
                DrawLine(from, p, new Color(RecallGold, light * 0.5f), 6, true);
                DrawLine(from, p, new Color(White, light), 2, true);
                if (i == 0) DrawStar(p, 10, RecallGold, light);
            }
        }
        // Painted coordinates keep the lamps on the steel after shrink, flip or movement.
        for (int i = 0; i < RecallDecks.Length; i++)
        {
            var deck = RecallDecks[i];
            float step = (i + 1) / (float)RecallDecks.Length;
            float light = alpha * batch * Mathf.Clamp((charge - step * 0.72f) * 5, 0, 1) * 0.8f;
            DrawRecallDeck(deck, light, preview: false, tower: true);
        }
        DrawStar(PaintedSource, 9 + _recallSendPulse * 7, RecallGold,
            alpha * Mathf.Clamp(batch * 0.3f + _recallSendPulse, 0, 1));
    }

    private void DrawVoidRecall(CanvasItem surface, float alpha)
    {
        var center = _voidOrbitIcon!.Size * 0.5f;
        float radius = _voidOrbitIcon.Size.X * 0.5f;
        if (_recallPreviewAlpha > 0 && _transferPreview != PromiseTransferPreview.None)
        {
            float age = _time - _transferPreviewStarted;
            float light = alpha * _recallPreviewAlpha * (0.92f + Mathf.Sin(age * 3) * 0.08f);
            bool taking = _transferPreview is PromiseTransferPreview.Take or PromiseTransferPreview.Tower
                || (_transferPreview == PromiseTransferPreview.Both && (int)(age / 2.5f) % 2 == 1);
            surface.DrawArc(center, radius, 0, Mathf.Tau, 48,
                new Color(RecallGold, light * 0.55f), 2, true);
            for (int comet = 0; comet < 3; comet++)
            {
                float angle = (taking ? -1 : 1) * age * 2 + comet * Mathf.Tau / 3;
                var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                DrawStar(surface, point, 4, RecallGold, light);
            }
            DrawStar(surface, center, 9, RecallGold, light * 0.7f);
        }
        float batch = _recallDepth > 0 ? 1 : Mathf.Clamp((_recallTailUntil - _time) / 0.42f, 0, 1);
        if (batch > 0)
        {
            float age = _time - _recallStarted;
            float charge = _recallCanDraw ? 0.3f + _recallSendPulse
                : RecallPulse(age, 0, 0.34f) * 0.55f;
            DrawStar(surface, center, 11 + _recallSendPulse * 5, RecallGold,
                alpha * batch * Mathf.Clamp(charge, 0, 1));
        }
    }

    private void DrawTransferPreview(float alpha)
    {
        bool tower = _transferPreview == PromiseTransferPreview.Tower;
        float age = _time - _transferPreviewStarted;
        // A steady illuminated rail remains visible throughout the hold. Motion
        // rides on top of it; a comet wrapping around never switches the preview off.
        float light = alpha * (0.92f + Mathf.Sin(age * 3) * 0.08f);
        float phase = Mathf.PosMod(age * 0.6f, 1);
        bool taking = _transferPreview == PromiseTransferPreview.Take
            || (_transferPreview == PromiseTransferPreview.Both && (int)(age / 2.5f) % 2 == 1);
        DrawPaintedTowerStrip(0, 1536, Vector2.Zero, new Color(RecallGold, light * (tower ? 0.22f : 0.08f)));
        for (int side = -1; side <= 1; side += 2)
        {
            var leg = side < 0 ? PaintedLeftLeg : PaintedRightLeg;
            for (int i = 1; i < leg.Length; i++)
                DrawPreviewRail(PaintedPoint(leg[i - 1]), PaintedPoint(leg[i]), light, tower);
            DrawPreviewRail(PaintedPoint(leg[^1]), PaintedSource, light, tower);
            if (tower)
            {
                var upper = PaintedPoint(new Vector2(516 + side * 40, 391));
                DrawPreviewRail(PaintedPoint(leg[^1]), upper, light * 0.85f, true);
                DrawPreviewRail(upper, PaintedPoint(PaintedTipPixel), light * 0.85f, true);
            }
            for (int comet = 0; comet < 3; comet++)
            for (int tail = 8; tail >= 0; tail--)
            {
                float t = Mathf.PosMod(phase + comet / 3f - tail * 0.018f, 1);
                var point = ReplenishPath(side, taking ? 1 - t : t);
                float glow = light * (1 - tail / 9f);
                DrawCircle(point, tower ? 10 : 7, new Color(RecallGold, glow * 0.16f));
                DrawCircle(point, tower ? 4.2f : 3, new Color(RecallGold, glow));
                if (tail == 0) DrawStar(point, tower ? 8 : 5, White, glow);
            }
        }
        foreach (var deck in RecallDecks)
        {
            if (!tower && deck.Y != 708) continue;
            DrawRecallDeck(deck, light * (tower ? 0.85f : 0.7f), preview: true, tower);
        }
        DrawCircle(PaintedSource, tower ? 28 : 17, new Color(RecallGold, light * 0.12f));
        DrawStar(PaintedSource, tower ? 15 : 9, RecallGold, light);
        if (tower)
        {
            DrawSetTransform(PaintedSource, 0, new Vector2(1, 0.26f));
            DrawArc(Vector2.Zero, 55, 0, Mathf.Tau, 64, new Color(RecallGold, light * 0.75f), 3, true);
            DrawSetTransform(Vector2.Zero);
            for (int i = 0; i < 2; i++)
            {
                float angle = age * 2 + i * Mathf.Pi;
                DrawStar(PaintedSource + new Vector2(Mathf.Cos(angle) * 55, Mathf.Sin(angle) * 14),
                    6, White, light);
            }
            DrawStar(PaintedPoint(PaintedTipPixel), 10, RecallGold, light);
        }
    }

    private void DrawPreviewRail(Vector2 from, Vector2 to, float light, bool tower)
    {
        DrawLine(from, to, new Color(RecallGold, light * (tower ? 0.2f : 0.13f)), tower ? 25 : 16, true);
        DrawLine(from, to, new Color(RecallGold, light * (tower ? 0.65f : 0.45f)), tower ? 9 : 6, true);
        DrawLine(from, to, new Color(White, light * (tower ? 0.95f : 0.78f)), tower ? 3.2f : 2, true);
    }

    private void DrawRecallDeck((float Y, float HalfWidth) deck, float light, bool preview, bool tower)
    {
        // Share the permanent lights' perspective corners instead of drawing
        // a horizontal stroke across the platform windows during selection.
        var edge = deck.Y switch { 708 => PaintedLowerDeckEdge, 391 => PaintedUpperDeckEdge, _ => null };
        for (int segment = 1; segment < (edge?.Length ?? 2); segment++)
        {
            var from = PaintedPoint(edge?[segment - 1] ?? new Vector2(516 - deck.HalfWidth, deck.Y));
            var to = PaintedPoint(edge?[segment] ?? new Vector2(516 + deck.HalfWidth, deck.Y));
            if (preview) DrawPreviewRail(from, to, light, tower);
            else
            {
                DrawLine(from, to, new Color(RecallGold, light * 0.1f), 15, true);
                DrawLine(from, to, new Color(RecallGold, light * 0.75f), 2.5f, true);
            }
            if (tower)
                for (int lamp = 0; lamp < 5; lamp++)
                    DrawStar(from.Lerp(to, lamp / 4f), preview ? 3.5f : 2.6f, White, light);
        }
    }
}
