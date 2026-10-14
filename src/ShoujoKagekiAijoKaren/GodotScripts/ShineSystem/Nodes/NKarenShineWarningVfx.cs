using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace ShoujoKagekiAijoKaren.src.Core.ShineSystem.Nodes;

/// <summary>Last-light embers follow the card from hand through the whole play.</summary>
public partial class NKarenShineWarningVfx(NCard card, CardModel model) : Node2D
{
    private float _age;
    private bool _warnedInHand;
    private readonly Vector2[] _star = new Vector2[8];
    public bool BelongsTo(CardModel? candidate) => ReferenceEquals(candidate, model);
    public override void _Ready() => Visible = false;

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(card) || !BelongsTo(card.Model)) { Hide(); QueueFree(); return; }
        bool inHand = model.Pile?.Type == PileType.Hand;
        bool inPlay = model.Pile?.Type == PileType.Play;
        if (inHand) _warnedInHand = ShineVfx.WillDeplete(model);
        // Shine is decremented at the start of OnPlayWrapper. Keep the hand warning
        // during queue/Play transitions, without mistaking a 2→1 play for depletion.
        Visible = card.DisplayingPile is PileType.Hand or PileType.Play
            && ((inHand && _warnedInHand) || (inPlay && (_warnedInHand || ShineVfx.WillDeplete(model, shineAlreadyDecreased: true))));
        if (!Visible) return;
        _age += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float pulse = .5f + .5f * Mathf.Sin(_age * 3.6f);
        var gold = new Color("#ffcb62");
        var hot = new Color("#fff4ce");
        // A broad, breathing last-light rim remains legible at actual hand scale.
        // Keep it outside the artwork and native cost/shine labels.
        for (int side = -1; side <= 1; side += 2)
        {
            var top = new Vector2(side * 137, -183);
            var bottom = new Vector2(side * 141, 178);
            DrawLine(top, bottom, new Color(gold, .08f + pulse * .10f), 20, true);
            DrawLine(top, bottom, new Color(gold, .25f + pulse * .25f), 7, true);
            for (int j = 0; j < 5; j++)
            {
                float y = -173 + j * 74;
                float ember = .65f + .35f * Mathf.Sin(_age * 3.6f - j * .7f);
                var p = new Vector2(side * 139, y);
                DrawLine(p, p + new Vector2(0, 40), new Color(hot, ember * .9f), 3.5f, true);
            }
        }
        var left = new Vector2(-100, -199);
        var right = new Vector2(120, -199);
        DrawLine(left, right, new Color(gold, .12f + pulse * .15f), 18, true);
        DrawLine(left, right, new Color(gold, .65f + pulse * .25f), 5, true);
        var lastLight = new Vector2(28, -231 - pulse * 4);
        float radius = 13 + pulse * 5;
        for (int layer = 8; layer > 0; layer--)
            DrawCircle(lastLight, radius * 2 * layer / 8f,
                new Color(gold, (.12f + pulse * .12f) * Mathf.Pow(1 - layer / 9f, 2)));
        for (int i = 0; i < _star.Length; i++)
        {
            float angle = i * Mathf.Pi / 4 - Mathf.Pi / 2;
            _star[i] = lastLight + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * (i % 2 == 0 ? 1 : .23f);
        }
        DrawColoredPolygon(_star, new Color(hot, .75f + pulse * .25f));
        // Sparks peel upward from the top and side seams instead of sitting on them.
        for (int i = 0; i < 28; i++)
        {
            float t = Mathf.PosMod(_age * .72f + i * .618034f, 1);
            float side = i % 2 == 0 ? -1 : 1;
            var origin = i % 3 == 0 ? new Vector2(-90 + (i * 43 % 205), -198)
                : new Vector2(side * 139, -180 + (i * 71 % 320));
            var p = origin + new Vector2(side * (7 * t + 15 * t * t), -54 * t);
            float a = Mathf.Sin(t * Mathf.Pi) * .9f;
            float r = 2 + i % 3;
            DrawCircle(p, r * 2.4f, new Color(gold, a * .15f));
            DrawLine(p + new Vector2(-side * 3, 7), p, new Color(gold, a * .65f), 2, true);
            DrawCircle(p, r, new Color(i % 4 == 0 ? hot : gold, a));
        }
    }
}
