using System.Collections.Generic;
using Godot;
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
    // Resource commands use native waits independently of particle completion.
    public const float NormalResourceTime = 0.65f;
    public const float FastResourceTime = 0.4f;
    private static readonly Vector2 DesignSize = new(1280, 720);
    private static Texture2D? _softTexture;
    private static Texture2D? _starTexture;
    private static Shader? _flowShader;
    private readonly Node2D _stage = new();
    private readonly List<ShaderMaterial> _materials = new();
    private NCombatRoom _room = null!;
    private Player _owner = null!;
    private NCard _card = null!;
    private Sprite2D _cardGlow = null!;
    private Sprite2D _bodyGlow = null!;
    private Vector2 _source;
    private Vector2 _axisX;
    private Vector2 _axisY;
    private float _elapsed;
    private float _burstAt = -1f;
    private float _energyAt = -1f;
    private float _speed;
    private float _seed;
    public bool HasEnergyPulse => _burstAt >= 0f && _energyAt >= 0f
        && _elapsed >= PulseStart && _elapsed < PulseStart + 0.7f;
    private float PulseStart => Mathf.Max(_burstAt + 2.65f, _energyAt);

    public static NKarenDropFuelVfx? Play(CardModel card)
    {
        var room = NCombatRoom.Instance;
        if (NonInteractiveMode.IsActive || !LocalContext.IsMe(card.Owner)
            || NRun.Instance?.GlobalUi == null || room == null || !CombatManager.Instance.IsInProgress)
            return null;
        var cardNode = NCard.FindOnTable(card);
        if (cardNode == null || room.GetCreatureNode(card.Owner.Creature) == null) return null;
        _flowShader ??= KarenResourceLoader.Load<Shader>("res://shaders/fuel_particles.gdshader", nameof(NKarenDropFuelVfx));
        if (_flowShader == null) return null;
        var effect = new NKarenDropFuelVfx
        {
            _room = room,
            _owner = card.Owner,
            _card = cardNode,
            _speed = SaveManager.Instance.PrefsSave.FastMode switch
            {
                FastModeType.Normal => 1f,
                FastModeType.Fast => NormalResourceTime / FastResourceTime,
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
        // PlayerCmd.GainEnergy owns its sound. No duplicate gain or attack/fire sound here.
    }

    public override void _Ready()
    {
        ZIndex = 100;
        TopLevel = true;
        _softTexture ??= MakeTexture(false);
        _starTexture ??= MakeTexture(true);
        AddChild(_stage);
        _cardGlow = AddGlow(new Vector2(140, 195));
        _bodyGlow = AddGlow(new Vector2(160, 230));
        UpdateCoordinates();
        CaptureSource();
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
        if (!GodotObject.IsInstanceValid(_card) || !_card.IsInsideTree()) return;
        var transform = _card.GetGlobalTransformWithCanvas();
        var scale = DesignSize / GetViewportRect().Size;
        _source = transform.Origin * scale;
        // The native card's origin is its center; keep emission inside its actual transformed face.
        _axisX = transform.X * (NCard.defaultSize.X * 0.32f) * scale;
        _axisY = transform.Y * (NCard.defaultSize.Y * 0.32f) * scale;
    }

    private void Emit()
    {
        CaptureSource();
        _burstAt = _elapsed;
        _cardGlow.Position = _source;
        AddParticles(0, 1f, 480, false);
        AddParticles(0.014f, 0.28f, 480, false);
        AddParticles(0.028f, 0.10f, 480, false);
        AddParticles(0, 0.85f, 28, true);
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
        _materials.Add(material);
        var particles = new GpuParticles2D
        {
            Emitting = false,
            Amount = amount,
            Texture = star ? _starTexture : _softTexture,
            Lifetime = 3.4,
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
        if (_burstAt < 0f && (_elapsed >= 0.4f || !GodotObject.IsInstanceValid(_card)
            || _card.PlayPileTween == null || !_card.PlayPileTween.IsRunning())) Emit();
        if (_burstAt < 0f) return;
        float age = _elapsed - _burstAt;
        float flash = Mathf.Clamp(age / 0.06f, 0, 1) * Mathf.Clamp((0.50f - age) / 0.35f, 0, 1);
        _cardGlow.Modulate = new Color(1, 0.69f, 0.29f, flash * 1.5f);
        float pulseAge = _elapsed - PulseStart;
        float pulse = HasEnergyPulse ? Mathf.Sin(Mathf.Clamp(pulseAge / 0.7f, 0, 1) * Mathf.Pi) : 0;
        _bodyGlow.Modulate = new Color(1, 0.73f, 0.39f, pulse * 0.68f);
        if (age >= 3.8f && (_energyAt < 0f || _elapsed >= PulseStart + 0.7f))
            GodotTreeExtensions.QueueFreeSafely(this);
    }
}
