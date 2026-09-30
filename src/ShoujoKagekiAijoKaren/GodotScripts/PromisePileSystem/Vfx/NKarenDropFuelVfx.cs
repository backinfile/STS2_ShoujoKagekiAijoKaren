using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenDropFuelVfx : Node2D
{
    private const float Duration = 1.5f;
    private const float TrailDuration = 0.5f;
    private const int TrailSamples = 30;
    private static readonly Texture2D? GlowSparkTexture = KarenResourceLoader.LoadTexture(
        "res://images/vfx/sts/glow_spark.png", nameof(NKarenDropFuelVfx));
    private static readonly CanvasItemMaterial AdditiveMaterial = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add
    };
    private NCombatRoom _room = null!;
    private float _elapsed;

    public static void Play(Player owner)
    {
        if (!LocalContext.IsMe(owner) || NRun.Instance?.GlobalUi == null
            || NCombatRoom.Instance == null || !CombatManager.Instance.IsInProgress) return;
        NRun.Instance.GlobalUi.AddChildSafely(new NKarenDropFuelVfx { _room = NCombatRoom.Instance });
    }

    public static void FlashEnergy(Player owner)
    {
        if (!LocalContext.IsMe(owner) || NRun.Instance?.GlobalUi == null
            || NCombatRoom.Instance == null || !CombatManager.Instance.IsInProgress) return;
        NRun.Instance.GlobalUi.AddChildSafely(new NKarenDropFuelBorderFlash(NCombatRoom.Instance));
    }

    public override void _Ready()
    {
        ZIndex = 100;
        TopLevel = true;
        Material = AdditiveMaterial;
        GlobalTransform = GetCanvasTransform().AffineInverse();
        SfxCmd.Play("event:/sfx/characters/attack_fire", 0.7f);
        // PlayerCmd.GainEnergy already plays the resource sound at the actual gain.
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_room) || !_room.IsInsideTree()
            || NCombatRoom.Instance != _room || !CombatManager.Instance.IsInProgress)
        {
            Hide();
            GodotTreeExtensions.QueueFreeSafely(this);
            return;
        }
        GlobalTransform = GetCanvasTransform().AffineInverse();
        _elapsed += (float)delta;
        if (_elapsed >= Duration)
        {
            GodotTreeExtensions.QueueFreeSafely(this);
            return;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (GlowSparkTexture == null) return;
        var viewport = GetViewportRect().Size;
        float scale = Mathf.Max(0.5f, viewport.X / 1920f);
        // Sample a fixed interval of travel, independent of render FPS. Keeping the
        // shapes deterministic also avoids random flicker on every redraw.
        for (int i = 0; i < TrailSamples; i++)
        {
            float age = TrailDuration * (1f - (float)i / TrailSamples);
            float sampleTime = _elapsed - age;
            if (sampleTime < 0f) continue;
            float freshness = 1f - age / TrailDuration;
            float growth = 1f + i * 0.05f;
            var size = new Vector2(0.375f * growth * (2.25f + 0.6f * Mathf.Sin(i * 2.4f)),
                0.375f * growth * (1.2f + 0.5f * Mathf.Cos(i * 1.7f))) * scale;
            DrawSpark(PositionAt(sampleTime, viewport, scale), size,
                new Color(1f, 0.9f, 0.3f, freshness));
        }
        var position = PositionAt(_elapsed, viewport, scale);
        DrawSpark(position, Vector2.One * 7.5f * scale, Colors.Red);
        DrawSpark(position, Vector2.One * 3f * scale, new Color(1f, 0.88f, 0.17f, 1f));
    }

    private static Vector2 PositionAt(float time, Vector2 viewport, float scale)
        => new((-100f + 3000f * time) * scale, viewport.Y * 0.5f);

    private void DrawSpark(Vector2 position, Vector2 scale, Color color)
    {
        DrawSetTransform(position, 0f, scale);
        DrawTexture(GlowSparkTexture!, -GlowSparkTexture!.GetSize() * 0.5f, color);
        DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
    }
}

internal partial class NKarenDropFuelBorderFlash : Node2D
{
    private const float Duration = 0.9f;
    private static readonly Texture2D? BorderGlowTexture = KarenResourceLoader.LoadTexture(
        "res://images/vfx/sts/border_glow_2.png", nameof(NKarenDropFuelBorderFlash));
    private static readonly CanvasItemMaterial AdditiveMaterial = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add
    };
    private readonly NCombatRoom _room;
    private float _elapsed;

    public NKarenDropFuelBorderFlash(NCombatRoom room) => _room = room;

    public override void _Ready()
    {
        TopLevel = true;
        ZIndex = 101;
        Material = AdditiveMaterial;
        GlobalTransform = GetCanvasTransform().AffineInverse();
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        if (_elapsed >= Duration || !GodotObject.IsInstanceValid(_room)
            || !_room.IsInsideTree() || NCombatRoom.Instance != _room || !CombatManager.Instance.IsInProgress)
        {
            Hide();
            GodotTreeExtensions.QueueFreeSafely(this);
            return;
        }
        GlobalTransform = GetCanvasTransform().AffineInverse();
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (BorderGlowTexture == null) return;
        float alpha = 1f - Mathf.Pow(Mathf.Clamp(_elapsed / Duration, 0f, 1f), 2f);
        DrawTextureRect(BorderGlowTexture, new Rect2(Vector2.Zero, GetViewportRect().Size),
            tile: false, modulate: new Color(1f, 0.84f, 0f, alpha));
    }
}
