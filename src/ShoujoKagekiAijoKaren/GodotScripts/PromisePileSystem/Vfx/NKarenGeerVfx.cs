using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>One-shot full-screen engagement or a brief gear pulse on its owner.</summary>
public partial class NKarenGeerVfx : Node2D
{
    private static readonly Color Steel = new("#9da4ad");
    private static readonly Color Rim = new("#dee2df");
    private static readonly Color Inset = new("#171d29");
    private NCreature _creature = null!;
    private NCombatRoom _room = null!;
    private bool _fullScreen;
    private float _age;
    public bool FullScreen => _fullScreen;
    public float Age => _age;
    public bool BelongsTo(NCreature creature) => _creature == creature;

    public void Init(NCreature creature, bool fullScreen)
    {
        _creature = creature;
        _room = NCombatRoom.Instance!;
        _fullScreen = fullScreen;
        TopLevel = fullScreen;
        ShowBehindParent = !fullScreen;
        ZIndex = fullScreen ? 100 : 0;
    }
    public override void _Ready() => UpdatePlacement();
    private void UpdatePlacement()
    {
        if (_fullScreen)
        {
            // Draw in a fixed composition while covering the actual viewport.
            var scale = GetViewportRect().Size / new Vector2(1280, 720);
            GlobalTransform = GetCanvasTransform().AffineInverse()
                * new Transform2D(new Vector2(scale.X, 0), new Vector2(0, scale.Y), Vector2.Zero);
        }
        else
        {
            var visual = _creature.Visuals.GlobalTransform;
            var x = new Vector2(Mathf.Max(visual.X.Length(), 0.001f), 0);
            var y = new Vector2(0, Mathf.Max(visual.Y.Length(), 0.001f));
            GlobalTransform = new Transform2D(x, y, _creature.VfxSpawnPosition + y * 12);
        }
    }
    public override void _Process(double delta)
    {
        _age += (float)delta;
        float duration = _fullScreen ? 1.2f : 0.62f;
        if (!GodotObject.IsInstanceValid(_creature) || !GodotObject.IsInstanceValid(_room)
            || NCombatRoom.Instance != _room || !CombatManager.Instance.IsInProgress || _age >= duration)
        {
            Hide();
            GodotTreeExtensions.QueueFreeSafely(this);
            return;
        }
        float fadeIn = _fullScreen ? 0.15f : 0.08f;
        float fadeOut = _fullScreen ? 0.3f : 0.2f;
        Modulate = new Color(1, 1, 1, Mathf.Clamp(Mathf.Min(_age / fadeIn, (duration - _age) / fadeOut), 0, 1));
        UpdatePlacement();
        QueueRedraw();
    }
    private static Vector2 Direction(float angle) => new(Mathf.Cos(angle), Mathf.Sin(angle));
    private void Gear(Vector2 center, float pitchRadius, int teeth, float angle, float alpha, bool solid)
    {
        float tooth = solid ? 11 : 5;
        float outer = pitchRadius + tooth;
        float inner = pitchRadius - tooth * 2.8f;
        // Individual annular quads keep the hub transparent: the character remains legible.
        for (int i = 0; i < teeth * 4; i++)
        {
            float a = angle + Mathf.Tau * i / (teeth * 4);
            float b = angle + Mathf.Tau * (i + 1) / (teeth * 4);
            float ra = i % 4 is 1 or 2 ? outer : pitchRadius - tooth;
            float rb = (i + 1) % 4 is 1 or 2 ? outer : pitchRadius - tooth;
            DrawColoredPolygon([center + Direction(a) * inner, center + Direction(a) * ra,
                center + Direction(b) * rb, center + Direction(b) * inner], new Color(Steel, alpha));
        }
        if (solid) DrawCircle(center, inner - 1, new Color(Inset, alpha * 0.5f));
        DrawArc(center, inner + 2, 0, Mathf.Tau, 100, new Color(Rim, alpha * 0.75f), solid ? 3 : 2, true);
        DrawArc(center, pitchRadius - tooth, angle + 3.4f, angle + 5.7f, 48,
            new Color(Rim, alpha), solid ? 4 : 2, true);
        float hub = pitchRadius * 0.13f;
        for (int i = 0; i < 6; i++)
        {
            var d = Direction(angle + i * Mathf.Tau / 6);
            DrawLine(center + d * hub, center + d * (inner - 2),
                new Color(Steel, alpha * 0.7f), solid ? pitchRadius * 0.055f : 4, true);
        }
        DrawArc(center, hub, 0, Mathf.Tau, 40, new Color(Rim, alpha), solid ? 5 : 3, true);
        if (solid)
        {
            for (int i = 0; i < 8; i++)
                DrawCircle(center + Direction(angle + i * Mathf.Tau / 8) * (inner + tooth), 3, new Color(Inset, alpha));
        }
    }
    public override void _Draw()
    {
        // Gentle acceleration followed by steady mechanical motion; neighbors counter-rotate.
        float angle = 1.7f * (_age - 0.10f * (1 - Mathf.Exp(-_age / 0.10f)));
        if (_fullScreen)
        {
            DrawRect(new Rect2(0, 0, 1280, 720), new Color(Inset, 0.42f));
            Gear(new Vector2(-125, 720), 250, 28, -angle * 360 / 250, 0.7f, true);
            Gear(new Vector2(1230, -50), 250, 28, angle * 360 / 250, 0.72f, true);
            Gear(new Vector2(750, 745), 245, 28, angle * 360 / 245, 0.85f, true);
            Gear(new Vector2(920, 310), 225, 26, -angle * 360 / 225 + 0.07f, 0.9f, true);
            Gear(new Vector2(335, 310), 360, 40, angle, 0.94f, true);
        }
        else
        {
            Gear(new Vector2(-136, 25), 38, 12, -angle * 100 / 38, 0.82f, false);
            Gear(new Vector2(128, -40), 34, 10, -angle * 100 / 34 + 0.15f, 0.82f, false);
            Gear(Vector2.Zero, 100, 28, angle, 0.92f, false);
        }
    }
}
