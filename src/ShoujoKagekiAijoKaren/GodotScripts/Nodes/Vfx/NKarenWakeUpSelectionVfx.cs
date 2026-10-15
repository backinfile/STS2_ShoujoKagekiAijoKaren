using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>A play-scoped curtain below the native selection grid, never an input surface.</summary>
public partial class NKarenWakeUpSelectionVfx : Node2D
{
    private sealed class Lease(NKarenWakeUpSelectionVfx node) : IDisposable
    {
        public void Dispose() { if (GodotObject.IsInstanceValid(node)) node.End(); }
    }
    private static readonly Color Red = new("#ed4858"), Gold = new("#ffc66a"), Amber = new("#ffb24d"), Ivory = new("#fff2c1");
    private NCombatRoom _room = null!;
    private Player _owner = null!;
    private NSimpleCardSelectScreen? _screen;
    private NCardGrid? _grid;
    private NPeekButton? _peek;
    private ColorRect? _field;
    private ShaderMaterial? _material;
    private readonly HashSet<CardModel> _selected = new();
    private readonly List<Vector2> _targets = new(2);
    private readonly List<CardModel> _gatherModels = new(2);
    private Callable _pressed;
    private float _age, _closingAt = -1;
    private bool _disposed;
    public bool Showing => GodotObject.IsInstanceValid(_field) && _field!.IsVisibleInTree();
    public int SelectedCount => _selected.Count;

    public static IDisposable? Begin(Player player)
    {
        if (!LocalContext.IsMe(player) || NCombatRoom.Instance is not { } room || CombatManager.Instance.IsOverOrEnding) return null;
        var node = new NKarenWakeUpSelectionVfx { _room = room, _owner = player, Name = "KarenWakeUpSelectionVfx", ZIndex = 40 };
        room.Ui.AddChild(node);
        return new Lease(node);
    }

    // The result callback is shared by all of Wake Up's actual source choices.
    internal static void CompleteCurrent(NCombatRoom room, IEnumerable<CardModel> cards)
    {
        var selected = cards.ToList();
        foreach (var effect in room.Ui.GetChildren().OfType<NKarenWakeUpSelectionVfx>())
            if (selected.Any(c => c.Owner == effect._owner)) effect.Complete(selected);
    }

    public override void _Process(double delta)
    {
        if (NCombatRoom.Instance != _room || CombatManager.Instance.IsOverOrEnding) { QueueFree(); return; }
        GlobalTransform = GetCanvasTransform().AffineInverse();
        if (_screen == null && !_disposed && NOverlayStack.Instance?.Peek() is NSimpleCardSelectScreen screen) Attach(screen);
        if (_field == null) { if (_disposed) QueueFree(); return; }
        _age += (float)delta;
        if (_closingAt < 0)
        {
            if (!GodotObject.IsInstanceValid(_screen) || _screen!.IsQueuedForDeletion()) { End(); return; }
            _field.Visible = _screen.IsVisibleInTree() && !_peek!.IsPeeking;
            _field.Size = _screen.Size;
        }
        float close = _closingAt < 0 ? 0 : Mathf.Clamp((_age - _closingAt) / .55f, 0, 1);
        if (_closingAt >= 0)
            for (int i = 0; i < _gatherModels.Count; i++)
                if (_gatherModels[i].Pile?.Type == MegaCrit.Sts2.Core.Entities.Cards.PileType.Hand
                    && NCard.FindOnTable(_gatherModels[i]) is { } moving && GodotObject.IsInstanceValid(moving))
                    _targets[i] = Shoulder(moving);
        _material!.SetShaderParameter("age", _age);
        _material.SetShaderParameter("strength", Mathf.SmoothStep(0, 1, Mathf.Clamp(_age / .32f, 0, 1))
            * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp((close - .65f) / .35f, 0, 1))));
        _material.SetShaderParameter("closing", close);
        for (int i = 0; i < _targets.Count; i++)
            _material.SetShaderParameter(i == 0 ? "gather_a" : "gather_b", _targets[i] / GetViewportRect().Size);
        if (_closingAt >= 0 && close >= 1) { QueueFree(); return; }
        QueueRedraw();
    }

    private void Attach(NSimpleCardSelectScreen screen)
    {
        _screen = screen;
        _grid = screen.GetNode<NCardGrid>("%CardGrid");
        _peek = screen.GetNode<NPeekButton>("%PeekButton");
        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://materials/vfx/wake_up_curtain.gdshader") };
        _material.SetShaderParameter("curtain_tex", GD.Load<Texture2D>("res://images/vfx/karen_wake_up_curtain.png"));
        _field = new ColorRect { Name = "KarenWakeUpCurtain", MouseFilter = Control.MouseFilterEnum.Ignore, Material = _material };
        screen.AddChild(_field);
        screen.MoveChild(_field, 0); // Above the shared dimmer, below every native control/card.
        _field.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _pressed = Callable.From<NCardHolder>(OnPressed);
        _grid.Connect(NCardGrid.SignalName.HolderPressed, _pressed);
    }

    private void OnPressed(NCardHolder holder)
    {
        if (_closingAt >= 0 || holder.CardModel is not { } card) return;
        var completed = _screen!.CardsSelected();
        if (completed.IsCompletedSuccessfully) { Complete(completed.Result); return; }
        if (!_selected.Remove(card)) _selected.Add(card);
    }

    private static Vector2 Shoulder(NCard card)
    {
        var frame = card.GetNode<TextureRect>("%Frame");
        return frame.GetGlobalTransformWithCanvas() * new Vector2(frame.Size.X * .83f, 0);
    }

    private void Complete(IEnumerable<CardModel> cards)
    {
        if (_closingAt >= 0 || !GodotObject.IsInstanceValid(_field)) return;
        _targets.Clear();
        if (GodotObject.IsInstanceValid(_grid))
            foreach (var model in cards.Take(2))
                if (_grid!.GetCardNode(model) is { } card && GodotObject.IsInstanceValid(card))
                { _targets.Add(Shoulder(card)); _gatherModels.Add(model); }
        // The native screen is queued for deletion immediately on its second click.
        // Carry only our rectangle out; never reparent or retain native card nodes.
        _field!.Reparent(this, false);
        _field.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _field.Position = Vector2.Zero;
        _field.Size = GetViewportRect().Size;
        _field.Visible = true;
        _closingAt = _age;
        for (int i = 0; i < _targets.Count; i++)
            _material!.SetShaderParameter(i == 0 ? "gather_a" : "gather_b", _targets[i] / GetViewportRect().Size);
        DisconnectGrid();
    }

    public void End()
    {
        if (!GodotObject.IsInstanceValid(this) || _disposed) return;
        _disposed = true;
        if (_closingAt < 0) { DisconnectGrid(); if (GodotObject.IsInstanceValid(_field)) _field!.QueueFree(); QueueFree(); }
    }

    public override void _ExitTree()
    {
        DisconnectGrid();
        if (GodotObject.IsInstanceValid(_field) && _field!.GetParent() != this) _field.QueueFree();
    }

    private void DisconnectGrid()
    {
        if (GodotObject.IsInstanceValid(_grid) && _grid!.IsConnected(NCardGrid.SignalName.HolderPressed, _pressed))
            _grid.Disconnect(NCardGrid.SignalName.HolderPressed, _pressed);
    }

    public override void _Draw()
    {
        if (!Showing) return;
        if (_closingAt < 0 && GodotObject.IsInstanceValid(_grid))
        {
            foreach (var model in _selected)
                if (_grid!.GetCardNode(model) is { } card && GodotObject.IsInstanceValid(card))
                    DrawStar(Shoulder(card), 10, .7f);
            return;
        }
        float t = Mathf.Clamp((_age - _closingAt) / .55f, 0, 1);
        float unit = GetViewportRect().Size.Y / 1080;
        foreach (var target in _targets)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 start = new(side < 0 ? 0 : GetViewportRect().Size.X, GetViewportRect().Size.Y * .68f);
                Vector2 control = target + new Vector2(side * 170, -160) * unit;
                Vector2 last = start * ((1 - t) * (1 - t)) + control * (2 * t * (1 - t)) + target * (t * t);
                for (int j = 1; j <= 24; j++)
                {
                    float q = Mathf.Lerp(t, 1, j / 24f);
                    Vector2 p = start * ((1 - q) * (1 - q)) + control * (2 * (1 - q) * q) + target * (q * q);
                    float a = (1 - t) * Mathf.Sin(j / 24f * Mathf.Pi);
                    DrawLine(last, p, new Color(Red, a * .20f), 16 * unit * (1 - t), true);
                    DrawLine(last, p, new Color(Gold, a * .75f), 2 * unit, true);
                    last = p;
                }
            }
            DrawStar(target, 13 + Mathf.Sin(t * Mathf.Pi) * 14, 1 - t);
        }
    }

    private void DrawStar(Vector2 point, float radius, float alpha)
    {
        radius *= GetViewportRect().Size.Y / 1080;
        for (int i = 4; i > 0; i--) DrawCircle(point, radius * i * .7f, new Color(Amber, alpha * .14f / i));
        var points = new Vector2[8];
        for (int i = 0; i < 8; i++) points[i] = point + Vector2.FromAngle(i * Mathf.Pi / 4) * radius * (i % 2 == 0 ? 1 : .16f);
        DrawColoredPolygon(points, new Color(Ivory, alpha));
    }
}
