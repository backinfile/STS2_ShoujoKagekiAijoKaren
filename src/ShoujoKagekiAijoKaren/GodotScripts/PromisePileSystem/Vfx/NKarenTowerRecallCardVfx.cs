using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>Observes native card movement. Never reparents, moves or decorates a pooled NCard.</summary>
public partial class NKarenTowerRecallCardVfx : Node2D
{
    private readonly NCard _card;
    private readonly CardModel _model;
    private readonly List<Vector2> _trail = new();
    private readonly Vector2[] _outline = new Vector2[5];
    private readonly Vector2[] _star = new Vector2[8];
    private static readonly Color Gold = new("#ffd18a");
    private static readonly Color White = new("#fff5db");
    private float _age;
    private float _landedAt = -1;

    public NKarenTowerRecallCardVfx(NCard card, CardModel model)
    {
        _card = card;
        _model = model;
        Name = "KarenTowerRecallCardVfx";
        ZIndex = 30;
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        if (!GodotObject.IsInstanceValid(_card) || !_card.IsInsideTree() || _card.Model != _model
            || _card.IsQueuedForDeletion() || _model.Pile?.Type != PileType.Hand || _age > 2.5f)
        {
            QueueFree();
            return;
        }
        var body = _card.Body;
        var point = body.GetGlobalTransformWithCanvas() * (body.Size * 0.5f);
        _trail.Add(point);
        if (_trail.Count > 16) _trail.RemoveAt(0);
        if (_landedAt < 0 && _age >= 0.09f && _card.GetParent() is NHandCardHolder holder
            && holder.Position.DistanceTo(holder.TargetPosition) < 8)
            _landedAt = _age;
        if (_landedAt >= 0 && _age - _landedAt > 0.36f) { QueueFree(); return; }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!GodotObject.IsInstanceValid(_card) || _card.Model != _model || _trail.Count < 2) return;
        var inverse = GetGlobalTransformWithCanvas().AffineInverse();
        float after = _landedAt < 0 ? 0 : _age - _landedAt;
        float tail = 1 - Mathf.Clamp(after / 0.2f, 0, 1);
        for (int i = 1; i < _trail.Count; i++)
        {
            float alpha = i / (float)_trail.Count * tail;
            var from = inverse * _trail[i - 1];
            var to = inverse * _trail[i];
            DrawLine(from, to, new Color(Gold, alpha * 0.12f), 12, true);
            DrawLine(from, to, new Color(Gold, alpha * 0.58f), 3, true);
            DrawLine(from, to, new Color(White, alpha * 0.74f), 1, true);
        }
        if (_landedAt < 0) return;
        float light = Mathf.Sin(Mathf.Clamp(after / 0.36f, 0, 1) * Mathf.Pi);
        var body = _card.Body;
        var map = inverse * body.GetGlobalTransformWithCanvas();
        var rect = new Rect2(new Vector2(8, 8), body.Size - new Vector2(16, 16));
        _outline[0] = map * rect.Position;
        _outline[1] = map * new Vector2(rect.End.X, rect.Position.Y);
        _outline[2] = map * rect.End;
        _outline[3] = map * new Vector2(rect.Position.X, rect.End.Y);
        _outline[4] = _outline[0];
        DrawPolyline(_outline, new Color(Gold, light * 0.12f), 10, true);
        DrawPolyline(_outline, new Color(White, light * 0.7f), 1.8f, true);
        DrawGlint(_outline[0], 8, light);
        DrawGlint(_outline[2], 5, light * 0.8f);
    }

    private void DrawGlint(Vector2 point, float radius, float alpha)
    {
        DrawCircle(point, radius * 2, new Color(Gold, alpha * 0.1f));
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.Pi / 4;
            _star[i] = point + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (i % 2 == 0 ? radius : radius * 0.22f);
        }
        DrawColoredPolygon(_star, new Color(White, alpha));
    }
}
