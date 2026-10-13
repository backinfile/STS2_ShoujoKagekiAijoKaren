using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>Card-origin burst, screen-wide scatter, then a subtle irregular planar curl into its owner.</summary>
public partial class NKarenDropFuelVfx : Node2D
{
    public const string BurstSfxPath = "res://ShoujoKagekiAijoKaren/audio/karen_fuel_ignite.wav";
    // Allow card travel, charge, and burst before drawing. Use native waits, not GPU completion.
    public const float NormalResourceTime = 0.8f;
    public const float FastResourceTime = 0.8f;
    private const float ChargeTime = 0.20f;
    private const float BurstSoundLeadTime = 0.12f;
    private static readonly Vector2 DesignSize = new(1280, 720);
    private static Texture2D? _softTexture;
    private static Texture2D? _starTexture;
    private static Shader? _flowShader;
    private readonly Node2D _stage = new();
    private readonly List<ShaderMaterial> _materials = new();
    private NCombatRoom _room = null!;
    private Player _owner = null!;
    private NCard _card = null!;
    private CardModel _model = null!;
    private Sprite2D _cardGlow = null!;
    private Sprite2D _bodyGlow = null!;
    private Vector2 _source;
    private Vector2 _axisX;
    private Vector2 _axisY;
    private float _elapsed;
    private float _chargeAt = -1f;
    private float _burstAt = -1f;
    private float _energyAt = -1f;
    private float _speed;
    private float _seed;
    private bool _burstSoundPlayed;
    public bool IsReplay { get; private set; }
    public bool HasEnergyPulse => _burstAt >= 0f && _energyAt >= 0f
        && _elapsed >= PulseStart && _elapsed < PulseStart + 0.42f;
    private float PulseStart => _energyAt;

    public static NKarenDropFuelVfx? Play(CardModel card, int playIndex = 0)
    {
        var room = NCombatRoom.Instance;
        if (NonInteractiveMode.IsActive || !LocalContext.IsMe(card.Owner)
            || NRun.Instance?.GlobalUi == null || room == null || !CombatManager.Instance.IsInProgress)
            return null;
        var cardNode = NCard.FindOnTable(card);
        if (cardNode == null || room.GetCreatureNode(card.Owner.Creature) == null) return null;
        KarenFuelCardVisuals.Restore(cardNode);
        _flowShader ??= KarenResourceLoader.Load<Shader>("res://shaders/fuel_particles.gdshader", nameof(NKarenDropFuelVfx));
        if (_flowShader == null) return null;
        var effect = new NKarenDropFuelVfx
        {
            _room = room,
            _owner = card.Owner,
            _card = cardNode,
            _model = card,
            IsReplay = playIndex > 0,
            _speed = SaveManager.Instance.PrefsSave.FastMode switch
            {
                // Fast mode keeps the same visual rhythm as Normal.
                FastModeType.Normal or FastModeType.Fast => 1.15f,
                _ => 2f
            },
            // Visual-only randomness must never consume the run's deterministic RNG.
            _seed = GD.RandRange(3, 10000)
        };
        NRun.Instance.GlobalUi.AddChildSafely(effect);
        return effect;
    }

    public static void ConfirmEnergy(NKarenDropFuelVfx? effect)
    {
        if (GodotObject.IsInstanceValid(effect) && !effect!.IsQueuedForDeletion())
            effect._energyAt = effect._elapsed;
        // PlayerCmd.GainEnergy owns its sound; the separate burst cue starts during charge.
    }

    public override void _Ready()
    {
        ZIndex = 100;
        TopLevel = true;
        _softTexture ??= MakeTexture(false);
        _starTexture ??= MakeTexture(true);
        AddChild(_stage);
        _cardGlow = AddGlow(new Vector2(140, 195));
        _bodyGlow = AddGlow(new Vector2(125, 185));
        UpdateCoordinates();
        CaptureSource();
        // Instant mode may resolve and pool the card before the next process frame.
        // Transfer its face into particles synchronously while its identity is still valid.
        if (SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant)
        {
            _chargeAt = 0;
            Emit();
        }
    }

    private static Texture2D MakeTexture(bool star)
    {
        using var image = Image.CreateEmpty(48, 48, false, Image.Format.Rgba8);
        for (int y = 0; y < 48; y++)
        for (int x = 0; x < 48; x++)
        {
            var p = (new Vector2(x, y) + Vector2.One * 0.5f) / 24f - Vector2.One;
            float alpha = star
                ? Mathf.Max(Mathf.Exp(-Mathf.Abs(p.X) * 22f) * Mathf.Pow(Mathf.Max(0, 1 - Mathf.Abs(p.Y)), 1.4f),
                    Mathf.Exp(-Mathf.Abs(p.Y) * 22f) * Mathf.Pow(Mathf.Max(0, 1 - Mathf.Abs(p.X)), 1.4f))
                : Mathf.Pow(Mathf.Max(0, 1 - p.Length()), 1.5f);
            image.SetPixel(x, y, new Color(1, 1, 1, alpha));
        }
        return ImageTexture.CreateFromImage(image);
    }

    private Sprite2D AddGlow(Vector2 size)
    {
        var glow = new Sprite2D
        {
            Texture = _softTexture,
            Scale = size / 48f,
            Modulate = Colors.Transparent,
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add }
        };
        _stage.AddChild(glow);
        return glow;
    }

    private void CaptureSource()
    {
        if (!GodotObject.IsInstanceValid(_card) || !_card.IsInsideTree() || _card.Model != _model) return;
        var transform = _card.GetGlobalTransformWithCanvas();
        var scale = DesignSize / GetViewportRect().Size;
        _source = transform.Origin * scale;
        // The native card's origin is its center; keep emission inside its actual transformed face.
        _axisX = transform.X * (NCard.defaultSize.X * 0.48f) * scale;
        _axisY = transform.Y * (NCard.defaultSize.Y * 0.48f) * scale;
    }

    private void PlayBurstSound()
    {
        if (_burstSoundPlayed) return;
        _burstSoundPlayed = true;
        // Play the approved fireball explosion at its original pitch and full
        // duration. Keep the player local to this VFX's lifetime.
        var sound = new AudioStreamPlayer
        {
            Stream = PreloadManager.Cache.GetAsset<AudioStream>(BurstSfxPath),
            Bus = "SFX",
            VolumeLinear = IsReplay ? .40f : .68f
        };
        AddChild(sound);
        sound.Finished += sound.QueueFree;
        sound.Play();
    }

    private void Emit()
    {
        CaptureSource();
        _burstAt = _elapsed;
        // Instant mode has no charge phase; guarantee one cue without a duplicate.
        PlayBurstSound();
        if (GodotObject.IsInstanceValid(_card) && _card.Model == _model)
            KarenFuelCardVisuals.Burst(_card);
        _cardGlow.Position = _source;
        _cardGlow.Scale = new Vector2(140, 195) / 48f;
        // The real CardPlay index owns replay intensity; separate casts do not share mutable state.
        AddParticles(0, IsReplay ? .68f : 1f, IsReplay ? 160 : 480, false);
        AddParticles(0.014f, IsReplay ? .14f : .24f, IsReplay ? 160 : 480, false);
        AddParticles(0.028f, IsReplay ? .05f : .08f, IsReplay ? 96 : 480, false);
        AddParticles(0, IsReplay ? .60f : .85f, IsReplay ? 12 : 28, true);
    }

    private void AddParticles(float delay, float alpha, int amount, bool star)
    {
        var material = new ShaderMaterial { Shader = _flowShader };
        material.SetShaderParameter("source_position", _source);
        material.SetShaderParameter("source_axis_x", _axisX);
        material.SetShaderParameter("source_axis_y", _axisY);
        material.SetShaderParameter("target_position", _bodyGlow.Position);
        material.SetShaderParameter("flow_seed", _seed);
        material.SetShaderParameter("trail_delay", delay);
        material.SetShaderParameter("trail_alpha", alpha);
        material.SetShaderParameter("bright_star", star);
        material.SetShaderParameter("spread_scale", IsReplay ? .54f : 1f);
        _materials.Add(material);
        var particles = new GpuParticles2D
        {
            Emitting = false,
            Amount = amount,
            Texture = star ? _starTexture : _softTexture,
            Lifetime = 2.3,
            OneShot = true,
            Explosiveness = 0.98f,
            Randomness = 0,
            FixedFps = 60,
            LocalCoords = true,
            VisibilityRect = new Rect2(-100, -100, 1480, 920),
            SpeedScale = _speed,
            ProcessMaterial = material,
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add }
        };
        _stage.AddChild(particles);
        particles.Restart();
        particles.Emitting = true;
    }

    private void UpdateCoordinates()
    {
        GlobalTransform = GetCanvasTransform().AffineInverse();
        _stage.Scale = GetViewportRect().Size / DesignSize;
        var creature = _room.GetCreatureNode(_owner.Creature);
        if (creature == null) return;
        var target = creature.GetCanvasTransform() * creature.VfxSpawnPosition / _stage.Scale;
        _bodyGlow.Position = target;
        foreach (var material in _materials) material.SetShaderParameter("target_position", target);
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
        _elapsed += (float)delta * _speed;
        UpdateCoordinates();
        // Manual skills start while their card is still moving from the hand to the play pile.
        // Wait only in the visual node; never block card commands on a tween or GPU completion.
        if (_chargeAt < 0f && (_elapsed >= 0.4f || !GodotObject.IsInstanceValid(_card)
            || _card.PlayPileTween == null || !_card.PlayPileTween.IsRunning())) _chargeAt = _elapsed;
        if (_burstAt < 0f)
        {
            if (_chargeAt < 0f) return;
            CaptureSource();
            _cardGlow.Position = _source;
            float chargeDuration = IsReplay ? .10f : ChargeTime;
            float chargeAge = _elapsed - _chargeAt;
            // Queue the sound before the flash so its audible attack lands with
            // the burst. Short replay charges start it immediately, never twice.
            if (chargeAge >= Mathf.Max(0, chargeDuration - BurstSoundLeadTime * _speed))
                PlayBurstSound();
            float charge = Mathf.Clamp(chargeAge / chargeDuration, 0, 1);
            _cardGlow.Scale = new Vector2(170, 235).Lerp(new Vector2(95, 130), charge) / 48f;
            _cardGlow.Modulate = new Color(1, 0.60f + charge * 0.22f, 0.10f, charge * charge * (IsReplay ? .65f : 1.1f));
            if (charge < 1f) return;
            Emit();
        }
        float age = _elapsed - _burstAt;
        float flashLife = IsReplay ? .20f : .34f;
        float flash = 1 - Mathf.SmoothStep(0, flashLife, age);
        _cardGlow.Modulate = new Color(1, 0.69f, 0.29f, flash * (IsReplay ? .8f : 1.35f));
        float pulseAge = _elapsed - PulseStart;
        float pulse = HasEnergyPulse ? Mathf.Sin(Mathf.Clamp(pulseAge / .42f, 0, 1) * Mathf.Pi) : 0;
        _bodyGlow.Modulate = new Color(1, 0.83f, 0.55f, pulse * .82f);
        if (age >= 2.6f && (_energyAt < 0f || _elapsed >= PulseStart + .42f))
            GodotTreeExtensions.QueueFreeSafely(this);
    }
}
