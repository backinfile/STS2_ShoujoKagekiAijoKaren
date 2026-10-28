using System.Collections.Generic;
using Godot;
using ShoujoKagekiAijoKaren.src.Core.Vfx;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>Observes native card movement. Never reparents, moves or decorates a pooled NCard.</summary>
public partial class NKarenTowerRecallCardVfx : Node2D
{
    private readonly NCard _card;
    private readonly CardModel _model;
    private readonly NKarenPromiseStarNode _source;
    private readonly record struct TrailPoint(Vector2 Position, float Age, float Distance);
    private readonly List<TrailPoint> _trail = new();
    private readonly Vector2[] _outline = new Vector2[5];
    private readonly Vector2[] _ribbonPoints = new Vector2[48];
    private readonly KarenVfxCanvasBatch _ribbonBatch = new();
    private static readonly Color Gold = new("#ffc774");
    private static readonly Color Blue = new("#a4d2ff");
    private static readonly Color White = new("#fff5db");
    private readonly float _arrivalDuration;
    private readonly float _trailDuration;
    private float _distance;
    private float _age;
    private float _landedAt = -1;

    public NKarenTowerRecallCardVfx(NCard card, CardModel model, NKarenPromiseStarNode source)
    {
        _card = card;
        _model = model;
        _source = source;
        bool fast = SaveManager.Instance?.PrefsSave.FastMode == FastModeType.Fast;
        _arrivalDuration = fast ? 0.24f : 0.46f;
        _trailDuration = fast ? 0.22f : 0.38f;
        _trail.Add(new TrailPoint(source.TransferCanvasPosition, 0, 0));
        Name = "KarenTowerRecallCardVfx";
        ZIndex = 30;
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        if (!GodotObject.IsInstanceValid(_source) || !_source.IsInsideTree() || _source.IsQueuedForDeletion()
            || !GodotObject.IsInstanceValid(_card) || !_card.IsInsideTree() || _card.Model != _model
            || _card.IsQueuedForDeletion() || _model.Pile?.Type != PileType.Hand || _age > 2.5f)
        {
            QueueFree();
            return;
        }
        var body = _card.Body;
        var point = body.GetGlobalTransformWithCanvas() * (body.Size * 0.5f);
        _distance += _trail[^1].Position.DistanceTo(point);
        _trail.Add(new TrailPoint(point, _age, _distance));
        // Time-based history gives the same tail length at different render rates.
        while (_trail.Count > 2 && (_age - _trail[0].Age > _trailDuration || _trail.Count > 48)) _trail.RemoveAt(0);
        if (_landedAt < 0 && _age >= 0.09f && _card.GetParent() is NHandCardHolder holder
            && holder.Position.DistanceTo(holder.TargetPosition) < 8)
            _landedAt = _age;
        if (_landedAt >= 0 && _age - _landedAt > _arrivalDuration) { QueueFree(); return; }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!GodotObject.IsInstanceValid(_card) || _card.Model != _model || _trail.Count < 2) return;
        var inverse = GetGlobalTransformWithCanvas().AffineInverse();
        float after = _landedAt < 0 ? 0 : _age - _landedAt;
        float tail = 1 - Mathf.Clamp(after / (_arrivalDuration * 0.55f), 0, 1);
        _ribbonBatch.Clear();
        float viewportScale = GetViewport().GetVisibleRect().Size.Y / 1080f;
        // Both ribbons follow observations of the native NCard; the card itself
        // remains owned, positioned and pooled entirely by the native hand.
        for (int ribbon = 0; ribbon < 2; ribbon++)
        {
            for (int i = 0; i < _trail.Count; i++)
                _ribbonPoints[i] = RibbonPoint(i, ribbon, inverse, viewportScale);
            for (int i = 1; i < _trail.Count; i++)
            {
                float alpha = Mathf.Sin(i / (float)_trail.Count * Mathf.Pi * 0.7f) * tail;
                var from = _ribbonPoints[i - 1];
                var to = _ribbonPoints[i];
                Color tint = ribbon == 0 ? Gold : Blue;
                _ribbonBatch.Line(from, to, new Color(tint, alpha * 0.085f), 20);
                _ribbonBatch.Line(from, to, new Color(tint, alpha * 0.20f), 9);
                _ribbonBatch.Line(from, to, new Color(tint, alpha * 0.95f), 4.2f);
                _ribbonBatch.Line(from, to, new Color(White, alpha * 0.45f), 0.75f);
            }
        }
        _ribbonBatch.Flush(this);
        for (int i = 3; i < _trail.Count - 2; i += 7)
            NKarenPromiseStarNode.DrawPromiseStar(this, RibbonPoint(i, i % 2, inverse, viewportScale), 4.5f,
                i % 2 == 0 ? Gold : Blue, tail * i / _trail.Count * 0.65f);
        if (_landedAt < 0) return;
        float progress = Mathf.Clamp(after / _arrivalDuration, 0, 1);
        float light = Mathf.Sin(progress * Mathf.Pi);
        var body = _card.Body;
        var map = inverse * body.GetGlobalTransformWithCanvas();
        var rect = new Rect2(new Vector2(8, 8), body.Size - new Vector2(16, 16));
        _outline[0] = map * rect.Position;
        _outline[1] = map * new Vector2(rect.End.X, rect.Position.Y);
        _outline[2] = map * rect.End;
        _outline[3] = map * new Vector2(rect.Position.X, rect.End.Y);
        _outline[4] = _outline[0];
        DrawPolyline(_outline, new Color(Gold, light * 0.07f), 9, true);
        DrawPolyline(_outline, new Color(Gold, light * 0.65f), 1.5f, true);
        NKarenPromiseStarNode.DrawPromiseStar(this, _outline[0], 9, Gold, light);
        NKarenPromiseStarNode.DrawPromiseStar(this, _outline[2], 6, Blue, light * 0.8f);
        var upperEdge = _outline[0].Lerp(_outline[1], progress);
        DrawCircle(upperEdge, 3, new Color(White, light));
    }

    private Vector2 RibbonPoint(int index, int ribbon, Transform2D inverse, float scale)
    {
        var sample = _trail[index];
        var before = _trail[System.Math.Max(0, index - 1)].Position;
        var next = _trail[System.Math.Min(_trail.Count - 1, index + 1)].Position;
        var normal = (next - before).Normalized().Orthogonal();
        float taper = Mathf.Sin(index / (float)(_trail.Count - 1) * Mathf.Pi);
        float twist = Mathf.Sin(sample.Distance * 0.035f - _age * 7 + ribbon * Mathf.Pi);
        // Canvas-space widths survive resolution changes without changing the
        // actual trajectory; the head converges exactly on the observed card.
        return inverse * (sample.Position + normal * twist * 13 * taper * scale);
    }
}
