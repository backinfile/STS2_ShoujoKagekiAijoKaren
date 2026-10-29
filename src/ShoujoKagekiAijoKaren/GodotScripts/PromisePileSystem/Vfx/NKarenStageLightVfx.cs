using ShoujoKagekiAijoKaren.src.Core.Vfx;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System.Linq;
using System;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenStageLightVfx : Node2D
{
    // Shared visual resources are prepared during the run loading screen as well as on demand.
    internal static void PrepareResources() => _additive ??= new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
    private static CanvasItemMaterial? _additive;
    internal static CanvasItemMaterial Additive { get { PrepareResources(); return _additive!; } }
    internal static Godot.Resource?[] PreparedResources => [StageLightTexture, StageLightFocusTexture, _additive];

    public const float HitTime = .28f;
    public const float FastHitTime = HitTime * .7f;
    internal static float PlaybackSpeed => SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? 1f / .7f : 1f;
    private const float Duration = 0.45f;
    private const float SpawnInterval = 0.06f;
    private const float FocusOverlayAlpha = 0.1f;
    private const float FocusOverlayFadeInDuration = 0.35f;
    private const float FocusOverlayFadeOutDuration = 0.35f;
    private static readonly Texture2D? StageLightTexture = LoadTexture("res://images/packed/vfx/stage_light/stage_light.png");
    private static readonly Texture2D? StageLightFocusTexture = LoadTexture("res://images/packed/vfx/stage_light/stage_light2.png");
    private static readonly float[] BeamDegrees = [70f, 35f, 350f, 310f];

    private readonly Creature _target;
    private readonly NCombatRoom _room;
    private NCreature? _view;
    private readonly NKarenStageLightBeam?[] _beams = new NKarenStageLightBeam?[4];
    private readonly bool _persistent;
    private float _duration = Duration;
    private float _spawnTimer;
    private int _nextBeam;
    private bool _stopping;
    private ColorRect? _darkOverlay;
    private Tween? _overlayTween;
    private StageLightTargetArea _lastTargetArea;
    private float _age;

    private NKarenStageLightVfx(Creature target, bool persistent)
    {
        _target = target;
        _room = NCombatRoom.Instance!;
        _view = _room.GetCreatureNode(target);
        _persistent = persistent;
        ZAsRelative = true;
        ZIndex = 20;
        TopLevel = true;
    }

    public static NKarenStageLightVfx? Create(Creature target)
    {
        if (!KarenVfxQuality.Enabled || NonInteractiveMode.IsActive || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant
            || NCombatRoom.Instance?.CombatVfxContainer == null) return null;
        foreach (var child in NCombatRoom.Instance.CombatVfxContainer.GetChildren())
        {
            if (child is not NKarenStageLightVfx cast || cast._persistent || cast._stopping
                || cast.IsQueuedForDeletion() || !ReferenceEquals(cast._target, target)) continue;
            cast._age = 0; cast._duration = Duration; cast._spawnTimer = 0; cast._nextBeam = 0;
            foreach (var beam in cast._beams)
                if (GodotObject.IsInstanceValid(beam) && !beam!.IsQueuedForDeletion()) beam.PauseForReplay();
            return null; // AttackCommand only attaches a newly created group.
        }
        return new NKarenStageLightVfx(target, persistent: false);
    }

    public override void _Ready()
    {
        GlobalTransform = Transform2D.Identity;
        UpdateTargetArea();
        if (!_persistent) KarenAudioManager.PlaySfx(KarenSfx.StageLight, volume: .85f);
    }

    public static NKarenStageLightVfx? StartFocus(Creature target)
    {
        if (!KarenVfxQuality.Enabled || NonInteractiveMode.IsActive || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant
            || NCombatRoom.Instance?.CombatVfxContainer == null) return null;

        var vfx = new NKarenStageLightVfx(target, persistent: true);
        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        vfx.GlobalPosition = Vector2.Zero;
        vfx.AddFocusOverlay();
        return vfx;
    }

    public void Stop()
    {
        if (_stopping) return;
        _stopping = true;
        foreach (var child in GetChildren())
        {
            if (child is NKarenStageLightBeam beam)
                beam.Stop();
            else if (child is NKarenStageLightFocusBeam focusBeam)
                focusBeam.Stop();
        }

        if (_darkOverlay != null && GodotObject.IsInstanceValid(_darkOverlay))
        {
            _overlayTween?.Kill();
            var overlay = _darkOverlay;
            _overlayTween = overlay.CreateTween();
            _overlayTween.TweenProperty(overlay, "modulate", new Color(1f, 1f, 1f, 0f), FocusOverlayFadeOutDuration);
            _overlayTween.TweenCallback(Callable.From(() =>
            {
                if (GodotObject.IsInstanceValid(overlay)) GodotTreeExtensions.QueueFreeSafely(overlay);
            }));
        }
    }

    public override void _Process(double delta)
    {
        if (!KarenVfxQuality.Enabled || !GodotObject.IsInstanceValid(_room) || !_room.IsInsideTree()
            || NCombatRoom.Instance != _room || !CombatManager.Instance.IsInProgress)
        { Hide(); QueueFree(); return; }
        if (!_stopping && (!GodotObject.IsInstanceValid(_view) || !_view!.IsInsideTree())) Stop();
        UpdateTargetArea();
        float d = (float)delta * (_persistent ? 1f : PlaybackSpeed);
        _age += d;
        if (!_persistent)
            _duration -= d;

        if (!_stopping)
        {
            _spawnTimer -= d;
            while (_spawnTimer <= 0f && _nextBeam < BeamDegrees.Length)
            {
                _spawnTimer += SpawnInterval;
                if (_persistent)
                {
                    AddChild(new NKarenStageLightFocusBeam(StageLightFocusTexture, GetTargetArea));
                    _nextBeam = BeamDegrees.Length;
                    break;
                }

                var beam = _beams[_nextBeam];
                if (!GodotObject.IsInstanceValid(beam) || beam!.IsQueuedForDeletion())
                {
                    beam = new NKarenStageLightBeam(StageLightTexture, GetTargetArea, BeamDegrees[_nextBeam], persistent: false, () => _age);
                    _beams[_nextBeam] = beam; AddChild(beam);
                }
                else beam.Restart();
                _nextBeam++;
            }
        }

        if ((_duration < -0.05f || _stopping) && GetChildCount() == 0 && !GodotObject.IsInstanceValid(_darkOverlay))
            GodotTreeExtensions.QueueFreeSafely(this);
    }

    private StageLightTargetArea GetTargetArea() => _lastTargetArea;
    private void UpdateTargetArea()
    {
        if (!GodotObject.IsInstanceValid(_view) || !_view!.IsInsideTree()) return;
        // All beams consume one captured area per frame, including animated visual scale/flip.
        var bounds = _view.Visuals.GetNodeOrNull<Control>("%Bounds") ?? _view.Hitbox;
        var transform = GetGlobalTransformWithCanvas().AffineInverse() * bounds.GetGlobalTransformWithCanvas();
        var area = new Rect2(transform * Vector2.Zero, Vector2.Zero)
            .Expand(transform * new Vector2(bounds.Size.X, 0)).Expand(transform * bounds.Size)
            .Expand(transform * new Vector2(0, bounds.Size.Y));
        _lastTargetArea = new StageLightTargetArea(area.GetCenter(), area.Size);
    }

    private static Texture2D? LoadTexture(string path)
    {
        return KarenResourceLoader.LoadTexture(path, nameof(NKarenStageLightVfx));
    }

    private void AddFocusOverlay()
    {
        var parent = (Node?)NRun.Instance?.GlobalUi ?? NCombatRoom.Instance?.CombatVfxContainer;
        if (parent == null) return;

        _darkOverlay = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, FocusOverlayAlpha),
            Modulate = new Color(1f, 1f, 1f, 0f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZAsRelative = true,
            ZIndex = 19
        };
        _darkOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        parent.AddChildSafely(_darkOverlay);

        _overlayTween = _darkOverlay.CreateTween();
        _overlayTween.TweenProperty(_darkOverlay, "modulate", Colors.White, FocusOverlayFadeInDuration);
    }

    public override void _ExitTree()
    {
        _overlayTween?.Kill();
        if (_darkOverlay != null && GodotObject.IsInstanceValid(_darkOverlay))
            GodotTreeExtensions.QueueFreeSafely(_darkOverlay);
    }
}

internal readonly record struct StageLightTargetArea(Vector2 Center, Vector2 Size);

internal partial class NKarenStageLightBeam : Sprite2D
{
    private const float Duration = 0.42f;
    private const float TargetWidth = 94f;
    private const float TargetHeight = 760f;
    private const float OffscreenTopMargin = 80f;

    private readonly bool _persistent;
    private readonly Func<StageLightTargetArea> _getTargetArea;
    private float _duration = Duration;
    private bool _stopping;
    private readonly Func<float>? _castAge;

    public NKarenStageLightBeam(Texture2D? texture, Func<StageLightTargetArea> getTargetArea, float degrees, bool persistent, Func<float>? castAge = null)
    {
        _persistent = persistent;
        _getTargetArea = getTargetArea;
        _castAge = castAge;
        Texture = texture;
        Centered = false;
        ZAsRelative = true;
        ZIndex = 20;
        Material = NKarenStageLightVfx.Additive;

        RotationDegrees = degrees;
        Modulate = Colors.White;

        // Sprite offsets are in texture pixels, before Scale is applied.
        if (texture != null) Offset = new Vector2(-texture.GetWidth() * 0.5f, 0f);
        UpdatePlacement();
    }

    private void UpdatePlacement()
    {
        var targetArea = _getTargetArea();
        var beamDirection = Vector2.Down.Rotated(Rotation);
        var endPosition = targetArea.Center + new Vector2(0f, targetArea.Size.Y * 0.35f);
        var height = GetClampedHeight(endPosition, beamDirection);
        Position = endPosition - beamDirection * height;
        if (Texture != null)
        {
            float age = _castAge?.Invoke() ?? 0;
            float focus = Mathf.SmoothStep(0, NKarenStageLightVfx.HitTime, age);
            float release = Mathf.SmoothStep(NKarenStageLightVfx.HitTime, NKarenStageLightVfx.HitTime + .23f, age);
            float width = Mathf.Lerp(1.1f, .40f, focus) + release * .18f;
            Scale = new Vector2(TargetWidth * width / Texture.GetWidth(), height / Texture.GetHeight());
        }
    }

    private static float GetClampedHeight(Vector2 endPosition, Vector2 beamDirection)
    {
        if (beamDirection.Y <= 0f)
            return TargetHeight;

        var heightToOffscreenTop = (endPosition.Y + OffscreenTopMargin) / beamDirection.Y;
        return Mathf.Max(TargetHeight, heightToOffscreenTop);
    }

    internal void PauseForReplay() { Hide(); SetProcess(false); }
    internal void Restart() { _duration = Duration; _stopping = false; Show(); SetProcess(true); }

    public void Stop()
    {
        _stopping = true;
        SetProcess(true); // A replay may have paused a beam before an interrupted cast.
    }

    public override void _Process(double delta)
    {
        float d = (float)delta * NKarenStageLightVfx.PlaybackSpeed;
        UpdatePlacement();
        if (!_persistent || _stopping)
            _duration -= d;

        float alpha = Mathf.Clamp(_duration / Duration, 0f, 1f);
        float age = _castAge?.Invoke() ?? 0;
        float strike = Mathf.Exp(-Mathf.Pow((age - NKarenStageLightVfx.HitTime) / .07f, 2));
        float entry = Mathf.SmoothStep(0, .055f, Duration - _duration);
        Modulate = new Color(1f, Mathf.Lerp(.78f, 1f, strike), Mathf.Lerp(.85f, 1f, strike),
            entry * alpha * (.45f + strike * .9f));

        if (_duration <= 0f)
            GodotTreeExtensions.QueueFreeSafely(this);
    }
}

internal partial class NKarenStageLightFocusBeam : Sprite2D
{
    private const float TargetWidth = 125f;
    private const float TargetHeight = 610f;
    private const float OffscreenTopMargin = 80f;

    private readonly Func<StageLightTargetArea> _getTargetArea;
    private bool _stopping;
    private float _alpha = 1f;

    public NKarenStageLightFocusBeam(Texture2D? texture, Func<StageLightTargetArea> getTargetArea)
    {
        _getTargetArea = getTargetArea;
        Texture = texture;
        Centered = false;
        ZAsRelative = true;
        ZIndex = 20;
        Material = NKarenStageLightVfx.Additive;
        if (texture != null)
            Offset = new Vector2(-texture.GetWidth() * 0.5f, -texture.GetHeight());

        UpdatePosition();
    }

    public void Stop()
    {
        _stopping = true;
        SetProcess(true); // A replay may have paused a beam before an interrupted cast.
    }

    public override void _Process(double delta)
    {
        if (_stopping)
        {
            _alpha -= (float)delta / 0.15f;
            Modulate = new Color(1f, 1f, 1f, Mathf.Max(0f, _alpha));
            if (_alpha <= 0f)
                GodotTreeExtensions.QueueFreeSafely(this);
        }

        UpdatePosition();
    }

    private void UpdatePosition()
    {
        var area = _getTargetArea();
        Position = area.Center + new Vector2(0f, area.Size.Y * 0.5f);
        RotationDegrees = 0f;
        if (Texture != null)
            Scale = new Vector2(TargetWidth / Texture.GetWidth(),
                Mathf.Max(TargetHeight, Position.Y + OffscreenTopMargin) / Texture.GetHeight());
    }
}
