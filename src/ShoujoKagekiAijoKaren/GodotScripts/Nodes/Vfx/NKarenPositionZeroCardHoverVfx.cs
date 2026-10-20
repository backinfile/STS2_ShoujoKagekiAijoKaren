using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using ShoujoKagekiAijoKaren.src.Core.ShineSystem;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>A small lit stage T anchored to the actual card frame; never writes to native highlight/material state.</summary>
public partial class NKarenPositionZeroCardHoverVfx(NKarenPositionZeroVfx owner, NCard card, CardModel model) : Node2D
{
    private readonly ShineCardBorder _border = new(card);
    private float _age;
    public override void _Ready() { Name = "KarenPositionZeroCardHover"; }
    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(owner) || !owner.IsInsideTree() || owner.IsQueuedForDeletion()
            || !GodotObject.IsInstanceValid(card) || card.Model != model || owner.HoverModel != model)
        { Hide(); QueueFree(); return; }
        _age += (float)delta;
        _border.Update(this);
        QueueRedraw();
    }
    public override void _Draw()
    {
        // Stay on the frame: opacity pulses, but the marker has no upward offset or drift.
        Vector2 at = _border.TopCenter;
        float a = Mathf.Min(1, _age * 10) * (.75f + .12f * Mathf.Sin(_age * 3));
        Color gold = new("#ffd58a"), white = new("#fff4d9"), red = new("#fb455a");
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 from = at + new Vector2(side * 50, -60);
            DrawPolygon([from, at + new Vector2(-24, 2), at + new Vector2(24, 2)],
                [new(gold, 0), new(white, a * .15f), new(white, a * .15f)]);
        }
        DrawLine(at + Vector2.Left * 22, at + Vector2.Right * 22, new Color(gold, a * .2f), 12, true);
        DrawLine(at + Vector2.Left * 20, at + Vector2.Right * 20, new Color(red, a), 4, true);
        DrawLine(at, at + Vector2.Down * 10, new Color(red, a), 5, true);
        DrawLine(at + new Vector2(-20, -2), at + new Vector2(20, -2), new Color(white, a), 1.3f, true);
    }
}
