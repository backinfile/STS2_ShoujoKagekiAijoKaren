using System;
using System.Linq;
using System.Threading;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

/// <summary>A painted bridge revealed by stage lighting, behind the real actors and cards.</summary>
public partial class NKarenBridgeVfx : Node2D
{
    private static readonly Vector2[] Beacons = [new(481, 81), new(834, 142), new(1124, 186)];
    private static readonly Vector2[] Stars = [new(810, 75), new(1014, 82), new(411, 308)];
    private static readonly Vector2[] JointCenters = [new(500, 146), new(854, 194), new(1132, 236)];
    private static readonly Vector2[] JointEmitters = [new(493, 183), new(852, 231), new(1129, 268)];
    private static readonly Color WarmLight = new(1, 0.85f, 0.55f);
    private static readonly Vector2 SceneryOffset = new(-12, -34);
    private const float SceneryScale = 1.02f;
    private static Texture2D? _bridgeTexture;
    private static Shader? _revealShader;
    private static Shader? _lightShader;
    private static Shader? _atmosphereShader;
    private NCombatRoom _room = null!;
    private NCreature _owner = null!;
    private CardModel? _card;
    private bool _hasCards;
    private bool _locked;
    private bool _fromDraw;
    private float _speed;
    private float _lightWeight = 1;
    private float _atmosphereWeight = 1;
    private Playback? _playback;
    private float _releaseAge = -1;
    private Vector2 _source;
    private Sprite2D? _bridge;
    private ShaderMaterial? _bridgeMaterial;
    private ShaderMaterial? _atmosphereMaterial;
    private ShaderMaterial? _mainLightMaterial;
    private ShaderMaterial? _floorLightMaterial;
    private ShaderMaterial? _lampMaterial;
    private readonly ShaderMaterial?[] _jointLightMaterials = new ShaderMaterial?[3];
    private ShaderMaterial? _leftLightMaterial;
    private ShaderMaterial? _rightLightMaterial;
    private Node2D? _glints;
    private AudioStreamPlayer? _slideSound;
    private AudioStreamPlayer? _lockSound;
    public float Age { get; private set; }
    public bool IsSpotlight => _card != null;
    public bool HasCards => _hasCards;
    public bool FromDraw => _fromDraw;
    public bool IsHolding => !IsSpotlight && _hasCards && _releaseAge < 0;
    public float MainLightStrength => _mainLightMaterial == null ? 0 : Mathf.SmoothStep(0.34f, 0.52f, Age) * StageFade;
    private float AtmosphereStrength => _atmosphereMaterial == null ? 0 : Mathf.SmoothStep(0, 0.24f, Age) * StageFade;
    private float ExitProgress => Mathf.Clamp(_releaseAge / 0.45f, 0, 1);
    private float StageFade => 1 - Mathf.SmoothStep(0, 1, ExitProgress);

    /// <summary>Managed lifetime signal; disposal is safe off the Godot scene thread.</summary>
    public sealed class Playback : IDisposable
    {
        private int _completed;
        internal bool IsCompleted => Volatile.Read(ref _completed) != 0;
        public void Dispose() => Interlocked.Exchange(ref _completed, 1);
    }

    public static Playback Play(Player owner, bool hasCards)
    {
        var playback = new Playback();
        Spawn(owner, null, hasCards, playback);
        return playback;
    }
    public static void PlaySpotlight(CardModel card) => Spawn(card.Owner, card, true, null);
    private static void Spawn(Player player, CardModel? card, bool hasCards, Playback? playback)
    {
        if (NonInteractiveMode.IsActive || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant) return;
        var room = NCombatRoom.Instance;
        if (room == null) return;
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(room) || NCombatRoom.Instance != room
                || CombatManager.Instance.IsOverOrEnding) return;
            var owner = room.GetCreatureNode(player.Creature);
            if (owner == null) return;
            room.BackCombatVfxContainer.AddChild(new NKarenBridgeVfx
            {
                _room = room, _owner = owner, _card = card, _hasCards = hasCards, _playback = playback,
                _fromDraw = PromisePileManager.IsVoidMode(player),
                _speed = SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? 1.6f : 1f
            });
            if (card == null && hasCards && !room.BackCombatVfxContainer.GetChildren()
                .OfType<StageLighting>().Any(n => !n.IsQueuedForDeletion()))
                room.BackCombatVfxContainer.AddChild(new StageLighting { ProcessPriority = 100 });
        }).CallDeferred();
    }

    public override void _Ready()
    {
        TopLevel = false;
        ZIndex = 0;
        TextureFilter = TextureFilterEnum.LinearWithMipmaps;
        Place();
        UpdateSource();
        if (IsSpotlight)
        {
            AddGlints(DrawCardGlints);
            return;
        }
        if (!_hasCards) return;
        _atmosphereShader ??= KarenResourceLoader.Load<Shader>("res://materials/vfx/bridge_atmosphere.gdshader", nameof(NKarenBridgeVfx));
        if (_atmosphereShader != null)
        {
            _atmosphereMaterial = new ShaderMaterial { Shader = _atmosphereShader };
            AddChild(new ColorRect { Position = new Vector2(0, 42), Size = new Vector2(1280, 420),
                Material = _atmosphereMaterial, MouseFilter = Control.MouseFilterEnum.Ignore });
        }
        _bridgeTexture ??= KarenResourceLoader.LoadTexture("res://images/vfx/bridge/stage-v12.png", nameof(NKarenBridgeVfx));
        _revealShader ??= ResourceLoader.Load<Shader>("res://materials/vfx/bridge_reveal.gdshader");
        // Preserve the full-canvas aspect ratio, including the transparent space below the bridge.
        _bridge = AddLayer(_bridgeTexture, out _bridgeMaterial);
        // Slim edge supports keep the shallow overhead span clear of the combat area.
        if (_bridge != null) _bridge.Position = SceneryOffset;
        _lightShader ??= KarenResourceLoader.Load<Shader>("res://materials/vfx/bridge_light.gdshader", nameof(NKarenBridgeVfx));
        _mainLightMaterial = AddLight(new Rect2(462, 200, 380, 341), false);
        _floorLightMaterial = AddLight(new Rect2(485, 495, 310, 44), true);
        _lampMaterial = AddLight(new Rect2(590, 181, 52, 38), true);
        _leftLightMaterial = AddLight(new Rect2(420, 183, 202, 304), false);
        _leftLightMaterial?.SetShaderParameter("light_color", new Color(1, 0.63f, 0.40f));
        _rightLightMaterial = AddLight(new Rect2(763, 231, 230, 270), false);
        _rightLightMaterial?.SetShaderParameter("light_color", new Color(1, 0.46f, 0.52f));
        for (int i = 0; i < JointCenters.Length; i++)
            _jointLightMaterials[i] = AddLight(new Rect2(JointCenters[i] - new Vector2(64, 53), new Vector2(128, 106)), true);
        AddGlints(DrawGlints);
        _slideSound = KarenAudioManager.PlaySfx("karen_bridge_slide.wav", 0.42f, _speed);
    }

    private void AddGlints(Action draw)
    {
        _glints = new Node2D { Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add } };
        _glints.Draw += draw;
        AddChild(_glints);
    }

    private ShaderMaterial? AddLight(Rect2 bounds, bool ground)
    {
        if (_lightShader == null) return null;
        var material = new ShaderMaterial { Shader = _lightShader };
        material.SetShaderParameter("intensity", 0f);
        material.SetShaderParameter("ground", ground);
        AddChild(new ColorRect
        {
            Position = bounds.Position, Size = bounds.Size, Material = material,
            MouseFilter = Control.MouseFilterEnum.Ignore
        });
        return material;
    }

    private Sprite2D? AddLayer(Texture2D? texture, out ShaderMaterial? material)
    {
        material = null;
        if (texture == null || _revealShader == null) return null;
        // Per-cast uniforms keep nested and replayed effects independent.
        material = new ShaderMaterial { Shader = _revealShader };
        material.SetShaderParameter("age", 0f);
        var sprite = new Sprite2D
        {
            Texture = texture, Centered = false,
            Scale = Vector2.One * (SceneryScale * 1280f / texture.GetWidth()), Material = material,
            TextureFilter = TextureFilterEnum.LinearWithMipmaps
        };
        AddChild(sprite);
        return sprite;
    }

    private void Place()
    {
        var scale = GetViewportRect().Size / new Vector2(1280, 720);
        GlobalTransform = GetCanvasTransform().AffineInverse()
            * new Transform2D(new Vector2(scale.X, 0), new Vector2(0, scale.Y), Vector2.Zero);
    }

    private void UpdateSource()
    {
        if (_owner.Entity.Player is { } player && KarenPromiseVfxStarManager.GetPileCanvasPosition(player) is { } source)
            _source = GetGlobalTransformWithCanvas().AffineInverse() * source;
    }
    public override void _Process(double delta)
    {
        float step = (float)delta * _speed;
        Age += step;
        if (!IsSpotlight && _hasCards && _playback?.IsCompleted == true)
            _releaseAge = Mathf.Max(_releaseAge, 0) + step;
        bool finished = IsSpotlight ? Age >= 0.36f : _hasCards ? ExitProgress >= 1 : Age >= 0.22f;
        if (!GodotObject.IsInstanceValid(_owner) || !GodotObject.IsInstanceValid(_room)
            || NCombatRoom.Instance != _room || CombatManager.Instance.IsOverOrEnding || finished)
        {
            Hide();
            GodotTreeExtensions.QueueFreeSafely(this);
            return;
        }
        Place();
        UpdateSource();
        if (!IsSpotlight && _hasCards && !_locked && Age >= 0.48f)
        {
            _locked = true;
            _lockSound = KarenAudioManager.PlaySfx("karen_bridge_lock.wav", 0.60f, _speed);
        }
        _bridgeMaterial?.SetShaderParameter("age", Age);
        _bridgeMaterial?.SetShaderParameter("exit_progress", ExitProgress);
        if (_bridge != null)
            _bridge.Position = SceneryOffset + new Vector2(0, 6 * (1 - Mathf.SmoothStep(0.1f, 0.6f, Age)));
        _glints?.QueueRedraw();
        QueueRedraw();
    }

    // A single late process pass uses the same envelopes for every stage. Per-node
    // normalization in _Process would mix this frame's ages with last frame's,
    // briefly exceeding the budget as a nested stage lights up.
    private partial class StageLighting : Node
    {
        public override void _Process(double delta)
        {
            var stages = GetParent().GetChildren().OfType<NKarenBridgeVfx>()
                .Where(n => !n.IsSpotlight && n._hasCards && n.Visible && !n.IsQueuedForDeletion()).ToArray();
            if (stages.Length == 0) { QueueFree(); return; }
            float lightWeight = 1 / Mathf.Max(1, stages.Sum(n => n.MainLightStrength));
            float atmosphereWeight = 1 / Mathf.Max(1, stages.Sum(n => n.AtmosphereStrength));
            foreach (var stage in stages) stage.UpdateStageLighting(lightWeight, atmosphereWeight);
        }
    }

    private void UpdateStageLighting(float lightWeight, float atmosphereWeight)
    {
        _lightWeight = lightWeight;
        _atmosphereWeight = atmosphereWeight;
        _atmosphereMaterial?.SetShaderParameter("age", Age);
        _atmosphereMaterial?.SetShaderParameter("strength", AtmosphereStrength * _atmosphereWeight);
        float lightStrength = MainLightStrength * _lightWeight;
        float breath = 0.96f + 0.04f * Mathf.Sin(Age * 1.7f);
        UpdateLight(_mainLightMaterial, lightStrength * 0.96f * breath);
        UpdateLight(_floorLightMaterial, lightStrength * 0.46f * breath);
        UpdateLight(_lampMaterial, lightStrength * 1.45f);
        UpdateLight(_leftLightMaterial, lightStrength * 0.26f);
        UpdateLight(_rightLightMaterial, lightStrength * 0.21f);
        for (int i = 0; i < _jointLightMaterials.Length; i++)
        {
            float travel = Mathf.PosMod((Age - 0.50f) * 0.24f - i * 0.31f, 1);
            float glint = Mathf.Exp(-Mathf.Pow((travel - 0.16f) / 0.11f, 2));
            float lockFlash = Mathf.Exp(-Mathf.Pow((Age - 0.50f - i * 0.065f) / 0.13f, 2));
            UpdateLight(_jointLightMaterials[i], lightStrength * (0.12f + glint * 0.30f + lockFlash * 0.52f));
        }
    }

    private void UpdateLight(ShaderMaterial? material, float intensity)
    {
        material?.SetShaderParameter("age", Age);
        material?.SetShaderParameter("intensity", intensity);
    }
    public override void _ExitTree()
    {
        foreach (var sound in new[] { _slideSound, _lockSound })
        {
            if (!GodotObject.IsInstanceValid(sound) || sound!.IsQueuedForDeletion()) continue;
            sound.Stop();
            sound.QueueFree();
        }
    }
    private float Envelope(float start, float attack, float release, float end) =>
        Mathf.SmoothStep(0, 1, Mathf.Clamp((Age - start) / attack, 0, 1))
        * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp((Age - release) / (end - release), 0, 1)));

    private void Beam(Vector2 start, Vector2 end, float width, float alpha)
    {
        var normal = (end - start).Normalized().Orthogonal() * width;
        // Layered cones soften the edges without a dense particle texture.
        for (int i = 0; i < 4; i++)
        {
            var spread = normal * (1 - i * 0.17f);
            DrawPolygon(new[] { start - spread * 0.04f, start + spread * 0.04f, end + spread, end - spread },
                new[] { new Color(1, 0.91f, 0.70f, alpha * 0.25f), new Color(1, 0.91f, 0.70f, alpha * 0.25f),
                    new Color(1, 0.96f, 0.85f, 0), new Color(1, 0.96f, 0.85f, 0) });
        }
    }
    private void SourceLight(float alpha)
    {
        for (int i = 4; i >= 1; i--)
            DrawCircle(_source, i * 5, new Color(1, 0.84f, 0.63f, alpha * 0.05f));
        DrawLine(_source - Vector2.Right * 9, _source + Vector2.Right * 9,
            new Color(1, 0.93f, 0.80f, alpha), 1.5f, true);
        DrawLine(_source - Vector2.Down * 13, _source + Vector2.Down * 13,
            new Color(1, 0.93f, 0.80f, alpha), 1.5f, true);
    }
    private void DrawGlints()
    {
        if (_glints == null) return;
        float strength = MainLightStrength * _lightWeight;
        // Deterministic, bounded populations; wrapping happens only while particles are invisible.
        DrawMotes(new Vector2(616, 205), new Vector2(63, 304), 126, 60, 0.092f, strength, WarmLight);
        DrawMotes(new Vector2(493, 190), new Vector2(-20, 252), 64, 16, 0.13f, strength * 0.65f, WarmLight);
        DrawMotes(new Vector2(852, 236), new Vector2(-18, 218), 58, 16, 0.11f, strength * 0.58f, new Color(1, 0.64f, 0.64f));
        DrawJointSparks(strength);
        for (int i = 0; i < Beacons.Length; i++)
        {
            float lit = strength * (0.76f + 0.24f * Mathf.Sin(Age * 2.0f + i * 0.9f));
            var point = Beacons[i];
            _glints.DrawLine(point + new Vector2(0, 8), point, new Color(0.55f, 0.22f, 0.22f, strength), 2, true);
            for (int ring = 4; ring >= 1; ring--)
                _glints.DrawCircle(point, ring * 3.5f, new Color(1, 0.06f, 0.18f, lit * 0.035f), antialiased: true);
            _glints.DrawCircle(point, 2, new Color(1, 0.55f, 0.58f, lit), antialiased: true);
            _glints.DrawLine(point - new Vector2(11, 0), point + new Vector2(11, 0), new Color(1, 0.23f, 0.36f, lit * 0.30f), 1, true);
        }
        for (int i = 0; i < Stars.Length; i++)
        {
            var point = Stars[i] + new Vector2(0, Mathf.Sin(Age * 0.6f + i) * 2);
            float pulse = 0.7f + 0.3f * Mathf.Sin(Age * 1.6f + i * 2.2f);
            float size = (i == 2 ? 6 : 9) * pulse;
            _glints.DrawCircle(point, size * 1.5f, new Color(1, 0.06f, 0.3f, strength * 0.035f), antialiased: true);
            _glints.DrawColoredPolygon(new[] { point + new Vector2(0, -size), point + new Vector2(size * 0.2f, -size * 0.18f),
                point + new Vector2(size * 0.7f, 0), point + new Vector2(size * 0.2f, size * 0.18f),
                point + new Vector2(0, size), point + new Vector2(-size * 0.2f, size * 0.18f),
                point + new Vector2(-size * 0.7f, 0), point + new Vector2(-size * 0.2f, -size * 0.18f) },
                new Color(1, 0.09f, 0.33f, strength * pulse * 0.74f));
        }
    }

    private void DrawMotes(Vector2 emitter, Vector2 drift, float spread, int count, float speed, float strength, Color tint)
    {
        if (_glints == null) return;
        for (int i = 0; i < count; i++)
        {
            float phase = i * 2.39996f;
            float travel = Mathf.PosMod(i * 0.754877666f + Age * speed * (0.85f + i % 5 * 0.06f), 1);
            float sway = Mathf.Sin(phase + Age * 0.42f) * (8 + travel * spread);
            var point = emitter + drift * travel + new Vector2(sway, Mathf.Sin(phase * 1.7f + Age) * 5);
            float visible = Mathf.Pow(Mathf.Sin(travel * Mathf.Pi), 1.3f);
            float flicker = 0.68f + 0.32f * Mathf.Sin(Age * 2.1f + phase);
            float alpha = strength * visible * flicker * 0.66f;
            float radius = 0.8f + (i % 4) * 0.30f;
            _glints.DrawCircle(point, radius * 3.1f, new Color(tint, alpha * 0.08f), antialiased: true);
            _glints.DrawCircle(point, radius, new Color(tint, alpha), antialiased: true);
            if (i % 9 == 0) DrawSparkle(point, 3.6f, alpha * 0.72f, tint);
        }
    }

    private void DrawJointSparks(float strength)
    {
        if (_glints == null) return;
        for (int joint = 0; joint < JointEmitters.Length; joint++)
        {
            float elapsed = Age - 0.48f - joint * 0.065f;
            if (elapsed <= 0 || elapsed >= 0.80f) continue;
            float fade = (1 - elapsed / 0.80f) * strength;
            for (int i = 0; i < 12; i++)
            {
                float phase = i * 2.39996f + joint;
                var velocity = new Vector2(Mathf.Sin(phase) * (28 + i % 4 * 8), 18 + i % 5 * 13);
                var point = JointEmitters[joint] + velocity * elapsed + new Vector2(0, 40 * elapsed * elapsed);
                var tail = point - (velocity + new Vector2(0, 80 * elapsed)).Normalized() * (3 + fade * 5);
                _glints.DrawLine(tail, point, new Color(1, 0.72f, 0.33f, fade * 0.72f), 1.2f, true);
                _glints.DrawCircle(point, 1.2f, new Color(1, 0.93f, 0.75f, fade), antialiased: true);
            }
            DrawSparkle(JointEmitters[joint], 12, fade * fade, WarmLight);
        }
    }

    private void DrawSparkle(Vector2 point, float size, float alpha, Color tint)
    {
        if (_glints == null) return;
        _glints.DrawLine(point - Vector2.Right * size, point + Vector2.Right * size, new Color(tint, alpha * 0.65f), 1, true);
        _glints.DrawLine(point - Vector2.Down * size * 1.4f, point + Vector2.Down * size * 1.4f, new Color(tint, alpha), 1, true);
    }

    private void DrawCardGlints()
    {
        var cardNode = NCard.FindOnTable(_card!);
        if (_glints == null || !GodotObject.IsInstanceValid(cardNode)) return;
        // Follow the visible frame as the native card moves and scales.
        var frame = cardNode!.GetNodeOrNull<Control>("%Frame");
        if (!GodotObject.IsInstanceValid(frame)) return;
        var bounds = frame!.GetGlobalRect();
        var center = ToLocal(bounds.GetCenter());
        var half = (ToLocal(bounds.End) - ToLocal(bounds.Position)).Abs() * 0.5f;
        if (half.X < 1 || half.Y < 1) return;
        float flash = Envelope(0, 0.045f, 0.12f, 0.36f);
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.Tau / 8 - Mathf.Pi / 2;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            direction /= Mathf.Max(Mathf.Abs(direction.X), Mathf.Abs(direction.Y));
            var point = center + direction * (half + new Vector2(10 + Age * 25, 8 + Age * 18));
            _glints.DrawCircle(point, 5, new Color(WarmLight, flash * 0.07f), antialiased: true);
            DrawSparkle(point, 4 + i % 3, flash * 0.82f, WarmLight);
        }
    }

    public override void _Draw()
    {
        if (IsSpotlight)
        {
            var cardNode = NCard.FindOnTable(_card!);
            // No substitute card or light on a made-up card location.
            if (!GodotObject.IsInstanceValid(cardNode)) return;
            var center = ToLocal(cardNode!.GlobalPosition);
            float light = Envelope(0, 0.05f, 0.15f, 0.36f);
            Beam(new Vector2(center.X - 30, 135), center + new Vector2(0, 140), 105, light * 0.32f);
            return;
        }
        if (!_hasCards)
        {
            SourceLight(Envelope(0, 0.045f, 0.10f, 0.22f) * 0.8f);
            return;
        }
        SourceLight(Envelope(0, 0.05f, 0.10f, 0.28f));
    }
}
