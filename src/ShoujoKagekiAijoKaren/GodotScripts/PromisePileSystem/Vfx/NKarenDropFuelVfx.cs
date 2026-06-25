using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System.Collections.Generic;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenDropFuelVfx : Node2D
{
    private const float Duration = 1.5f;
    private const int MaxTrailPositions = 30;
    private static readonly Texture2D? GlowSparkTexture = LoadTexture("res://images/vfx/sts/glow_spark.png");
    private static bool Flipper = true;

    private readonly List<Vector2> _prevPositions = new();
    private Vector2 _position;
    private Vector2 _velocity;
    private float _duration = Duration;
    private Vector2 _textureHalfSize = Vector2.Zero;

    public static void Play()
    {
        if (NRun.Instance?.GlobalUi == null) return;

        var vfx = new NKarenDropFuelVfx();
        NRun.Instance.GlobalUi.AddChildSafely(vfx);
        vfx.GlobalPosition = Vector2.Zero;
    }

    public override void _Ready()
    {
        ZIndex = 100;
        ZAsRelative = true;
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };

        var viewportSize = GetViewportRect().Size;
        float viewportScale = GetViewportScale(viewportSize.X);
        _textureHalfSize = GlowSparkTexture?.GetSize() * 0.5f ?? Vector2.Zero;

        float xOffset = Flipper ? -100f : -50f;
        Flipper = !Flipper;
        _position = new Vector2(xOffset * viewportScale - _textureHalfSize.X, viewportSize.Y * 0.5f - _textureHalfSize.Y);
        _velocity = new Vector2(3000f * viewportScale, 0f);

        SfxCmd.Play("event:/sfx/characters/attack_fire", 0.7f);
        SfxCmd.Play("event:/sfx/ui/gain_energy", 0.9f);
        AddChild(new NKarenDropFuelBorderFlash());
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;

        _prevPositions.Add(_position);
        _position += _velocity * d;
        if (_prevPositions.Count > MaxTrailPositions)
            _prevPositions.RemoveAt(0);

        _duration -= d;
        if (_duration < 0f)
            GodotTreeExtensions.QueueFreeSafely(this);

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (GlowSparkTexture == null) return;

        for (int i = 0; i < _prevPositions.Count; i++)
        {
            float growth = i * 0.05f + 1f;
            var scale = new Vector2(
                0.375f * growth * (float)GD.RandRange(1.5, 3.0),
                0.375f * growth * (float)GD.RandRange(0.5, 2.0)
            );
            DrawSpark(_prevPositions[i], scale, new Color(1f, 0.9f, 0.3f, 1f));
        }

        DrawSpark(_position, Vector2.One * 7.5f, Colors.Red);
        DrawSpark(_position, Vector2.One * 3f, new Color(1f, 0.88f, 0.17f, 1f));
    }

    private void DrawSpark(Vector2 topLeftPosition, Vector2 scale, Color color)
    {
        var position = topLeftPosition + _textureHalfSize;
        DrawSetTransform(position, 0f, scale);
        DrawTexture(GlowSparkTexture!, -_textureHalfSize, color);
        DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
    }

    private static float GetViewportScale(float width)
    {
        return Mathf.Max(0.5f, width / 1920f);
    }

    private static Texture2D? LoadTexture(string path)
    {
        return KarenResourceLoader.LoadTexture(path, nameof(NKarenDropFuelVfx));
    }
}

internal partial class NKarenDropFuelBorderFlash : Control
{
    private const float Duration = 0.9f;
    private static readonly Texture2D? BorderGlowTexture = LoadTexture("res://images/vfx/sts/border_glow_2.png");
    private float _duration = Duration;

    public NKarenDropFuelBorderFlash()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ZIndex = 101;
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
    }

    public override void _Process(double delta)
    {
        _duration -= (float)delta;
        if (_duration <= 0f)
            GodotTreeExtensions.QueueFreeSafely(this);

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (BorderGlowTexture == null) return;

        var viewportSize = GetViewportRect().Size;
        float t = Mathf.Clamp(_duration / Duration, 0f, 1f);
        float alpha = 1f - Mathf.Pow(1f - t, 2f);
        var color = new Color(1f, 0.84f, 0f, alpha);
        DrawTextureRect(BorderGlowTexture, new Rect2(Vector2.Zero, viewportSize), tile: false, modulate: color);
    }

    private static Texture2D? LoadTexture(string path)
    {
        return KarenResourceLoader.LoadTexture(path, nameof(NKarenDropFuelBorderFlash));
    }
}
