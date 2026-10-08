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
    private float _age;
    private static Texture2D? _castFrames;
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
    public override void _Ready()
    {
        if (_fullScreen)
        {
            _castFrames ??= KarenResourceLoader.LoadTexture(
                "res://images/vfx/geer/original-cast-atlas.png", nameof(NKarenGeerVfx));
            TextureFilter = TextureFilterEnum.Linear;
        }
        else
        {
            _gearTexture ??= KarenResourceLoader.LoadTexture("res://images/vfx/geer/steel-gear.png", nameof(NKarenGeerVfx));
            TextureFilter = TextureFilterEnum.LinearWithMipmaps;
        }
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
            if (_castFrames == null) return;
            // Original animation, sampled at 30 fps. Do not depend on video decoder timing.
            int frame = Mathf.Clamp((int)(_age * 30), 0, 35);
            var region = new Rect2((frame % 6) * 640, (frame / 6) * 360, 640, 360);
            DrawTextureRectRegion(_castFrames, new Rect2(0, 0, 1280, 720), region,
                new Color(1, 1, 1, 0.94f));
        }
        else
        {
            Gear(new Vector2(-136, 25), 38, -angle * 100 / 38, 0.82f);
            Gear(new Vector2(128, -40), 34, -angle * 100 / 34 + 0.15f, 0.82f);
            Gear(Vector2.Zero, 100, angle, 0.92f);
        }
    }
}
