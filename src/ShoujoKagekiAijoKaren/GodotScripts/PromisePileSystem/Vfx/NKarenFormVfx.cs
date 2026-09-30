using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using ShoujoKagekiAijoKaren.src.Core.Utils;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenFormVfx : Node2D
{
    private static readonly Texture2D? HorizontalLineTexture = LoadTexture("res://images/vfx/sts/horizontal_line.png");

    private NCreature? _creatureNode;
    private float _timer;
    private bool _stopping;
    private Tween? _stopTween;
    private float _drawSurgeHold;
    private int _drawSfxRound = -1;
    public float SpeedMultiplier { get; private set; } = 1f;

    public void PulseDraw()
    {
        if (_stopping) return;
        // Key audio to the combat round, not the spacing between card animations:
        // even a slow draw sequence must produce only one wind sound.
        if (_creatureNode?.Entity.CombatState is { } combat && _drawSfxRound != combat.RoundNumber)
        {
            _drawSfxRound = combat.RoundNumber;
            SfxCmd.Play("event:/sfx/characters/ironclad/ironclad_whirlwind", 0.5f);
        }
        // Refresh, never add: ten cards still produce one gust. Real-time duration
        // keeps native Fast mode readable without slowing down gameplay.
        _drawSurgeHold = 0.55f;
        _timer = Mathf.Min(_timer, 0.04f);
    }

    public bool BelongsTo(NCreature creature) => _creatureNode == creature;

    public void Init(NCreature creatureNode)
    {
        _creatureNode = creatureNode;
        ZAsRelative = true;
        ZIndex = 2;
        TopLevel = true;
    }

    public void Restart()
    {
        CancelFade();
        _stopping = false;
        Modulate = Colors.White;
        Visible = true;
        _timer = 0f;
        _drawSurgeHold = 0f;
        SpeedMultiplier = 1f;
    }

    public void Stop()
    {
        if (_stopping) return;
        _stopping = true;
        _drawSurgeHold = 0f;
        _stopTween = CreateTween();
        _stopTween.TweenProperty(this, "modulate", new Color(1f, 1f, 1f, 0f), 0.25f);
        _stopTween.TweenCallback(Callable.From(() =>
        {
            if (_stopping) GodotTreeExtensions.QueueFreeSafely(this);
        }));
    }

    private void CancelFade()
    {
        if (GodotObject.IsInstanceValid(_stopTween)) _stopTween!.Kill();
        _stopTween = null;
    }

    public override void _ExitTree() => CancelFade();

    public override void _Ready()
    {
        Restart();
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_creatureNode) || !_creatureNode!.IsInsideTree())
        {
            GodotTreeExtensions.QueueFreeSafely(this);
            return;
        }
        // This is a battlefield-wide wind, not an actor-local emitter. Cancel the canvas
        // transform so character scaling/flipping and camera motion cannot bend its path.
        GlobalTransform = GetCanvasTransform().AffineInverse();

        if (_stopping) return;

        float d = (float)delta;
        bool surging = _drawSurgeHold > 0f;
        _drawSurgeHold = Mathf.Max(0f, _drawSurgeHold - d);
        SpeedMultiplier = Mathf.MoveToward(SpeedMultiplier, surging ? 2.8f : 1f,
            d * (surging ? 12f : 3.6f));
        _timer -= d * SpeedMultiplier;
        if (_timer <= 0f)
        {
            _timer += (float)GD.RandRange(0.2, 0.4);
            AddChild(new NKarenWindyParticle(HorizontalLineTexture, GetViewportRect().Size, this));
        }
    }

    private static Texture2D? LoadTexture(string path)
    {
        return KarenResourceLoader.LoadTexture(path, nameof(NKarenFormVfx));
    }
}

internal partial class NKarenWindyParticle : Sprite2D
{
    private static readonly CanvasItemMaterial AdditiveMaterial = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add
    };
    private readonly NKarenFormVfx _wind;
    private readonly Vector2 _velocity;
    private readonly float _rotationVelocity;
    private readonly float _lifetime;
    private readonly float _leftBound;
    private readonly float _topBound;
    private readonly float _bottomBound;
    private float _elapsed;

    public NKarenWindyParticle(Texture2D? texture, Vector2 viewportSize, NKarenFormVfx wind)
    {
        _wind = wind;
        Texture = texture;
        Centered = true;
        float scale = Mathf.Max(0.5f, viewportSize.X / 1920f);
        Position = new Vector2(viewportSize.X + (float)GD.RandRange(80, 260) * scale,
            (float)GD.RandRange(0.22, 0.65) * viewportSize.Y);
        _velocity = new Vector2((float)GD.RandRange(-2500, -1500) * scale,
            (float)GD.RandRange(-100, 100) * scale);
        _rotationVelocity = (float)GD.RandRange(0, 0.5);
        _leftBound = -300f * scale;
        _topBound = viewportSize.Y * 0.16f;
        _bottomBound = viewportSize.Y * 0.73f;
        _lifetime = (Position.X - _leftBound) / -_velocity.X;
        Scale = new Vector2((float)GD.RandRange(0.5, 0.9), (float)GD.RandRange(1, 2)) * scale;
        Modulate = new Color(0.28f, 0.1f, 0.08f, 0f);
        Material = AdditiveMaterial;
    }

    public override void _Process(double delta)
    {
        float d = (float)delta * _wind.SpeedMultiplier;
        _elapsed += d;
        Position += _velocity * d;
        RotationDegrees += _rotationVelocity * d;
        // Fade at the boundaries instead of cutting a bright streak off mid-frame.
        float edgeFade = Mathf.Clamp(Mathf.Min(Position.Y - _topBound, _bottomBound - Position.Y) / 40f, 0f, 1f);
        float alpha = Mathf.Min(Mathf.Clamp(_elapsed / 0.12f, 0f, 1f),
            Mathf.Clamp((_lifetime - _elapsed) / 0.12f, 0f, 1f));
        Modulate = new Color(0.28f, 0.1f, 0.08f, 0.9f * alpha * edgeFade);
        if (_elapsed >= _lifetime || Position.X < _leftBound || edgeFade <= 0f)
            GodotTreeExtensions.QueueFreeSafely(this);
    }
}
