using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace ShoujoKagekiAijoKaren.src.Core.ShineSystem.Nodes;

/// <summary>Last-light embers stay in the hand card's own draw order.</summary>
public partial class NKarenShineWarningVfx(NCard card, CardModel model) : Node2D
{
    private float _age;
    public bool BelongsTo(CardModel? candidate) => ReferenceEquals(candidate, model);
    public override void _Ready() => Visible = false;

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(card) || !BelongsTo(card.Model)) { Hide(); QueueFree(); return; }
        Visible = model.Pile?.Type == PileType.Hand && card.DisplayingPile == PileType.Hand
            && ShineVfx.WillDeplete(model);
        if (!Visible) return;
        _age += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(_age * 4.2f);
        // The upper edge stays readable when the lower card is below the viewport.
        for (int j = 0; j < 4; j++)
        {
            var a = new Vector2(-94 + j * 54, -197);
            DrawLine(a, a + new Vector2(28, -2), new Color(1, .84f, .4f, .35f + pulse * .4f), 2.6f, true);
        }
        var lastLight = new Vector2(56, -220 - pulse * 3);
        float lightSize = 5.5f + pulse * 2.5f;
        DrawCircle(lastLight, lightSize * 1.8f, new Color(1, .65f, .2f, .08f + pulse * .09f));
        DrawLine(lastLight - Vector2.Up * lightSize, lastLight + Vector2.Up * lightSize,
            new Color(1, .94f, .63f, .55f + pulse * .35f), 2, true);
        DrawLine(lastLight - Vector2.Right * lightSize * .65f, lastLight + Vector2.Right * lightSize * .65f,
            new Color(1, .94f, .63f, .55f + pulse * .35f), 2, true);
        // Broken gold seams, not a second solid border over the native red warning.
        for (int side = -1; side <= 1; side += 2)
        for (int j = 0; j < 5; j++)
        {
            float y = -170 + j * 76;
            var a = new Vector2(side * 139, y);
            DrawLine(a, a + new Vector2(side * 3, 22), new Color(1, .79f, .32f, .22f + pulse * .4f), 2.4f, true);
        }
        for (int i = 0; i < 18; i++)
        {
            float t = Mathf.PosMod(_age * .63f + i * .618034f, 1);
            float side = i % 2 == 0 ? -1 : 1;
            var p = new Vector2(side * (141 + 14 * t), -188 + (i * 71 % 360) - 28 * t);
            float a = Mathf.Sin(t * Mathf.Pi) * (.34f + pulse * .4f);
            float r = 1.2f + (i % 3) * .55f;
            DrawCircle(p, r, new Color(1, .73f, .23f, a));
            if (i % 5 == 0)
            {
                DrawLine(p - Vector2.Up * r * 2, p + Vector2.Up * r * 2, new Color(1, .92f, .6f, a), 1, true);
                DrawLine(p - Vector2.Right * r * 2, p + Vector2.Right * r * 2, new Color(1, .92f, .6f, a), 1, true);
            }
        }
    }
}
