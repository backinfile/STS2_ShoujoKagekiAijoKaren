using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>One-shot full-screen engagement or a brief gear pulse on its owner.</summary>
public partial class NKarenGeerVfx : Node2D
{
    private static Texture2D? _gearTexture;
    private NCreature _creature = null!;
    private NCombatRoom _room = null!;
    private bool _fullScreen;
    private bool _castOnOwner;
    private float _age;
    public bool FullScreen => _fullScreen;
    private bool IsCast => _fullScreen || _castOnOwner;
    public float Age => _age;
    public bool BelongsTo(NCreature creature) => _creature == creature;

    public void Init(NCreature creature, bool fullScreen, bool castOnOwner = false)
    {
        _creature = creature;
        _room = NCombatRoom.Instance!;
        _fullScreen = fullScreen;
        _castOnOwner = castOnOwner;
        // GlobalTransform below cancels parent transforms without escaping its draw order.
        TopLevel = false;
        ShowBehindParent = !fullScreen;
        ZIndex = 0;
    }
    public override void _Ready()
    {
        _gearTexture ??= KarenResourceLoader.LoadTexture("res://images/vfx/geer/steel-gear.png", nameof(NKarenGeerVfx));
        TextureFilter = TextureFilterEnum.LinearWithMipmaps;
        UpdatePlacement();
    }
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
        float duration = IsCast ? KarenGeerVfxManager.CastDuration : 0.62f;
        if (!GodotObject.IsInstanceValid(_creature) || !GodotObject.IsInstanceValid(_room)
            || NCombatRoom.Instance != _room || !CombatManager.Instance.IsInProgress || _age >= duration)
        {
            Hide();
            GodotTreeExtensions.QueueFreeSafely(this);
            return;
        }
        // Each casting layer has its own envelope, so the owner's gear remains last.
        Modulate = new Color(1, 1, 1, IsCast ? 1 : Envelope(0, 0.08f, 0.42f, duration));
        UpdatePlacement();
        QueueRedraw();
    }
    private float Envelope(float start, float attack, float release, float end)
    {
        return Mathf.SmoothStep(0, 1, Mathf.Clamp((_age - start) / attack, 0, 1))
            * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp((_age - release) / (end - release), 0, 1)));
    }
    private void EdgeGear(Vector2 outside, Vector2 settled, float radius, float delay, float direction, float opacity)
    {
        float elapsed = Mathf.Max(0, _age - delay);
        float entrance = Mathf.SmoothStep(0, 1, Mathf.Clamp(elapsed / 0.22f, 0, 1));
        float angle = direction * 0.75f * (elapsed - 0.10f * (1 - Mathf.Exp(-elapsed / 0.10f)));
        Gear(outside.Lerp(settled, entrance), radius, angle,
            opacity * Envelope(delay, 0.22f, 0.56f + delay * 0.2f, 0.84f + delay * 0.2f));
    }
    private void Gear(Vector2 center, float pitchRadius, float angle, float alpha)
    {
        if (_gearTexture == null) return;
        // Imagegen sprite includes a narrow transparent margin around its teeth.
        float extent = (pitchRadius + 11) * 2 / 0.90f;
        DrawSetTransform(center, angle);
        DrawTextureRect(_gearTexture, new Rect2(-extent / 2, -extent / 2, extent, extent),
            false, new Color(1, 1, 1, alpha));
        DrawSetTransform(Vector2.Zero);
    }
    public override void _Draw()
    {
        // Gentle acceleration followed by steady mechanical motion; neighbors counter-rotate.
        float angle = 1.7f * (_age - 0.10f * (1 - Mathf.Exp(-_age / 0.10f)));
        if (_fullScreen)
        {
            // BackCombatVfxContainer keeps both the cast and its subtle dim behind actors/UI.
            DrawRect(new Rect2(0, 0, 1280, 720),
                new Color(0.025f, 0.035f, 0.055f, 0.10f * Envelope(0, 0.2f, 0.6f, 0.95f)));
            EdgeGear(new Vector2(-125, 145), new Vector2(-80, 170), 255, 0.10f, -1, 0.38f);
            EdgeGear(new Vector2(1420, 265), new Vector2(1360, 285), 335, 0.20f, 1, 0.42f);
            EdgeGear(new Vector2(850, -200), new Vector2(850, -155), 225, 0.28f, -1, 0.28f);

        }
        else if (_castOnOwner)
        {
            float appear = Mathf.SmoothStep(0, 1, Mathf.Clamp(_age / 0.2f, 0, 1));
            Gear(Vector2.Zero, 116 * Mathf.Lerp(0.94f, 1, appear), angle * 0.55f,
                0.78f * Envelope(0, 0.2f, 0.65f, KarenGeerVfxManager.CastDuration));
        }
        else
        {
            Gear(new Vector2(-136, 25), 38, -angle * 100 / 38, 0.82f);
            Gear(new Vector2(128, -40), 34, -angle * 100 / 34 + 0.15f, 0.82f);
            Gear(Vector2.Zero, 100, angle, 0.92f);
        }
    }
}
