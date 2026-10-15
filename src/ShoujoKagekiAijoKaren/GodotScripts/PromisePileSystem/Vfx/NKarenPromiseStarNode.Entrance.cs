using Godot;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenPromiseStarNode
{
    private float _combatEntranceAge;
    public bool IsCombatEntranceActive { get; private set; }
    public float CombatEntranceReveal => IsCombatEntranceActive ? EntranceEase(_combatEntranceAge, .14f, 1.02f) : 1;
    private float EntranceHeader => IsCombatEntranceActive ? EntranceEase(_combatEntranceAge, .92f, 1.10f) : 1;
    private float EntranceAmbient => IsCombatEntranceActive ? EntranceEase(_combatEntranceAge, .22f, 1.02f) : 1;

    public void BeginCombatEntrance(float age)
    {
        if (IsVoidPresentation || _recallDepth > 0 || _reproductionActive) return;
        IsCombatEntranceActive = true;
        _combatEntranceAge = age;
        QueueRedraw();
    }

    public void AdvanceCombatEntrance(float age)
    {
        if (!IsCombatEntranceActive) return;
        if (IsVoidPresentation || _recallDepth > 0 || _reproductionActive)
        { EndCombatEntrance(); return; }
        _combatEntranceAge = age;
        QueueRedraw();
    }

    public void EndCombatEntrance()
    {
        IsCombatEntranceActive = false;
        _combatEntranceAge = 0;
        QueueRedraw();
    }

    private static float EntranceEase(float age, float start, float end)
    { float t = Mathf.Clamp((age - start) / (end - start), 0, 1); return t * t * (3 - 2 * t); }

    private float EntranceLampVisibility(Vector2 point)
    {
        if (!IsCombatEntranceActive) return 1;
        float front = PaintedPoint(new Vector2(0, 1536 * (1 - CombatEntranceReveal))).Y;
        return Mathf.SmoothStep(0, 1, Mathf.Clamp((point.Y - front + 10) / 20, 0, 1));
    }

    private void DrawCombatEntranceTower(float alpha)
    {
        // Reveal the painted prop in its existing coordinates. Targets, orbit
        // paths, hitboxes and native card transfers never move with this sweep.
        float front = 1536 * (1 - CombatEntranceReveal);
        float fade = EntranceEase(_combatEntranceAge, 0, .16f);
        DrawPaintedTowerStrip(front, 1536, Vector2.Zero, new Color(.90f, .90f, .90f, alpha * fade * .92f));
        if (front > 0 && front < 1536)
        {
            for (float y = front; y < Mathf.Min(1536, front + 100); y += 16)
            {
                float light = Mathf.Exp(-(y - front) / 34) * alpha * fade * .48f;
                DrawPaintedTowerStrip(y, y + 16, Vector2.Zero, new Color(1.7f, 1.35f, .85f, light));
            }
        }
        float flash = EntranceEase(_combatEntranceAge, 1.02f, 1.10f)
            * (1 - EntranceEase(_combatEntranceAge, 1.16f, 1.40f));
        if (flash > 0) DrawTowerLamp(PaintedPoint(PaintedTipPixel), 6, flash * alpha * .65f);
    }
}
