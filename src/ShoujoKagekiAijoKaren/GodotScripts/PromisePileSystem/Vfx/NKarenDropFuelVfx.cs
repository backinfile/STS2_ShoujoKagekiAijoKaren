using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>Local skill presentation: throw, submerge, splash, then acknowledge actual energy gain.</summary>
public partial class NKarenDropFuelVfx : Node2D
{
    public const float NormalImpactTime = 0.65f;
    public const float FastImpactTime = 0.4f;
    private static readonly Texture2D? CrownTexture = KarenResourceLoader.LoadTexture(
        "res://images/relics/karen_hairpin2_relic.png", nameof(NKarenDropFuelVfx));
    private static readonly Texture2D? GlowTexture = KarenResourceLoader.LoadTexture(
        "res://images/vfx/sts/glow_spark.png", nameof(NKarenDropFuelVfx));
    private NCombatRoom _room = null!;
    private readonly Node2D _stage = new();
    private Sprite2D _crown = null!;
    private Sprite2D _glow = null!;
    private ShaderMaterial _crownMaterial = null!;
    private readonly Node2D _molten = new();
    private readonly Line2D[] _ripples = new Line2D[3];
    private float _elapsed;
    private float _fallDuration;
    private float _impactAt = -1f;
    private float _energyAt = -1f;
    private float _speed = 1f;
    public bool HasImpacted => _impactAt >= 0f;
    public bool HasEnergyPulse => _energyAt >= 0f && (_elapsed - _energyAt) * _speed < 0.6f;

    public static NKarenDropFuelVfx? Play(Player owner)
    {
        if (NonInteractiveMode.IsActive || !LocalContext.IsMe(owner) || NRun.Instance?.GlobalUi == null
            || NCombatRoom.Instance == null || !CombatManager.Instance.IsInProgress) return null;
        var effect = new NKarenDropFuelVfx
        {
            _room = NCombatRoom.Instance,
            _fallDuration = SaveManager.Instance.PrefsSave.FastMode switch
            {
                FastModeType.Normal => NormalImpactTime,
                FastModeType.Fast => FastImpactTime,
                _ => 0f
            }
        };
        effect._speed = effect._fallDuration > 0f ? NormalImpactTime / effect._fallDuration : 2f;
        NRun.Instance.GlobalUi.AddChildSafely(effect);
        return effect;
    }

    // Called by the card after its native scaled wait, never by a visual callback.
    public static void Impact(NKarenDropFuelVfx? effect)
    {
        if (!IsAlive(effect) || effect!.HasImpacted) return;
        effect._impactAt = effect._elapsed;
        var splash = NSplashVfx.Create(Vector2.Zero, new Color(1f, 0.55f, 0.13f));
        if (splash != null)
        {
            splash.Scale = new Vector2(0.72f, 0.62f);
            effect._stage.AddChildSafely(splash);
        }
        // Native fire flipbooks provide the brief reaction and hot specks above the liquid.
        var flare = NFireBurstVfx.Create(Vector2.Zero, 0.48f);
        if (flare != null) effect._stage.AddChildSafely(flare);
        SfxCmd.Play("event:/sfx/characters/attack_fire", 0.7f);
    }

    public static void ConfirmEnergy(NKarenDropFuelVfx? effect)
    {
        if (IsAlive(effect)) effect!._energyAt = effect._elapsed;
        // PlayerCmd.GainEnergy owns the sound; never duplicate it here.
    }

    private static bool IsAlive(NKarenDropFuelVfx? effect)
        => GodotObject.IsInstanceValid(effect) && !effect!.IsQueuedForDeletion()
            && effect.IsInsideTree() && NCombatRoom.Instance == effect._room
            && CombatManager.Instance.IsInProgress;

    public override void _Ready()
    {
        ZIndex = 100;
        TopLevel = true;
        AddChild(_stage);
        // A short graphic splash mark, not a rendered piece of scenery.
        _stage.AddChild(_molten);
        AddMoltenShape(new Vector2(150, 22), new Color(0.94f, 0.24f, 0.045f));
        AddMoltenShape(new Vector2(121, 13), new Color(1f, 0.63f, 0.09f));
        AddMoltenShape(new Vector2(75, 5), new Color(1f, 0.94f, 0.48f));

        _crownMaterial = new ShaderMaterial
        {
            Shader = GD.Load<Shader>("res://shaders/fuel_submerge.gdshader")
        };
        _crown = MakeSprite(CrownTexture, new Vector2(88, 88));
        _crown.Material = _crownMaterial;
        _stage.AddChild(_crown);
        _glow = MakeSprite(GlowTexture, new Vector2(220, 55));
        _glow.Modulate = Colors.Transparent;
        _stage.AddChild(_glow);
        for (int i = 0; i < _ripples.Length; i++)
        {
            var ring = new Line2D { Width = 2f, Antialiased = true, Visible = false };
            _ripples[i] = ring;
            _stage.AddChild(ring);
        }
        UpdatePresentation();
    }

    private void AddMoltenShape(Vector2 size, Color color)
    {
        var points = new Vector2[24];
        for (int i = 0; i < points.Length; i++)
        {
            float angle = Mathf.Tau * i / points.Length;
            float edge = 1f + 0.09f * Mathf.Sin(i * 2.7f);
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size * edge;
        }
        _molten.AddChild(new Polygon2D { Polygon = points, Color = color, Antialiased = true });
    }

    private static Sprite2D MakeSprite(Texture2D? texture, Vector2 size)
        => new() { Texture = texture, Scale = texture == null ? Vector2.One : size / texture.GetSize() };

    public override void _Process(double delta)
    {
        if (!IsAlive(this) || !GodotObject.IsInstanceValid(_room) || !_room.IsInsideTree())
        {
            Hide();
            GodotTreeExtensions.QueueFreeSafely(this);
            return;
        }
        _elapsed += (float)delta;
        // Retain the native particle tails; on interruption freeing the parent cancels their waits.
        float end = Mathf.Max((_impactAt >= 0f ? _impactAt : _fallDuration) + 2.1f,
            _energyAt + 0.8f);
        if (_elapsed >= end)
        {
            GodotTreeExtensions.QueueFreeSafely(this);
            return;
        }
        Modulate = new Color(1f, 1f, 1f, Mathf.Min(Mathf.Clamp(_elapsed / 0.12f, 0f, 1f),
            Mathf.Clamp((end - _elapsed) / 0.55f, 0f, 1f)));
        UpdatePresentation();
    }

    private void UpdatePresentation()
    {
        GlobalTransform = GetCanvasTransform().AffineInverse();
        var viewport = GetViewportRect().Size;
        _stage.Position = viewport * new Vector2(0.64f, 0.58f);
        _stage.Scale = Vector2.One * Mathf.Min(viewport.X / 1920f, viewport.Y / 1080f);
        float impact = HasImpacted ? (_elapsed - _impactAt) * _speed : -1f;
        float t = HasImpacted ? 1f : Mathf.Clamp(_elapsed / Mathf.Max(0.01f, _fallDuration), 0f, 1f);
        float sink = HasImpacted ? Mathf.Clamp(impact / 0.3f, 0f, 1f) : 0f;
        // The path retains the native item's separated horizontal/vertical progress idea.
        // At contact rotation reaches zero, so the shader clips exactly along the liquid plane.
        _crown.Position = new Vector2(Mathf.Lerp(80f, 0f, t), Mathf.Lerp(-220f, -44f, t * t) + 90f * sink);
        _crown.Rotation = Mathf.Lerp(-0.7f, 0f, t);
        _crown.Visible = sink < 1f;
        _crownMaterial.SetShaderParameter("surface_cut", HasImpacted ? 1f - sink * 90f / 88f : 1f);
        _crownMaterial.SetShaderParameter("heat", sink);
        float pulse = HasEnergyPulse ? Mathf.Sin((_elapsed - _energyAt) * _speed / 0.6f * Mathf.Pi) : 0f;
        float markAlpha = impact < 0f ? 0.8f : Mathf.Clamp(1f - impact / 0.55f, 0f, 1f);
        _molten.Modulate = new Color(1f, 1f, 1f, markAlpha);
        _molten.Scale = Vector2.One * (1f + Mathf.Max(0f, impact) * 0.3f);
        float impactGlow = impact >= 0f ? Mathf.Max(0f, 1f - impact / 0.5f) : 0f;
        _glow.Modulate = new Color(1f, 0.63f, 0.16f, impactGlow * 0.65f + pulse * 0.35f);
        for (int i = 0; i < _ripples.Length; i++)
        {
            float age = impact - i * 0.1f;
            var ring = _ripples[i];
            ring.Visible = HasImpacted && age >= 0f && age < 0.45f;
            if (!ring.Visible) continue;
            var points = new Vector2[49];
            float radius = 20f + age * 220f;
            for (int j = 0; j < points.Length; j++)
            {
                float angle = Mathf.Tau * j / (points.Length - 1);
                points[j] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.12f);
            }
            ring.Points = points;
            ring.DefaultColor = new Color(1f, 0.94f, 0.55f, (1f - age / 0.45f) * 0.65f);
        }
    }
}
