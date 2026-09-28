using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System.Linq;
using System;

namespace ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

public partial class NKarenStageLightVfx : Node2D
{
    private const float Duration = 0.6f;
    private const float SpawnInterval = 0.06f;
    private const float FocusOverlayAlpha = 0.2f;
    private const float FocusOverlayFadeInDuration = 0.35f;
    private const float FocusOverlayFadeOutDuration = 0.35f;
    private static readonly Texture2D? StageLightTexture = LoadTexture("res://images/packed/vfx/stage_light/stage_light.png");
    private static readonly Texture2D? StageLightFocusTexture = LoadTexture("res://images/packed/vfx/stage_light/stage_light2.png");
    private static readonly float[] BeamDegrees = [70f, 35f, 350f, 310f];

    private readonly Creature _target;
    private readonly bool _persistent;
    private float _duration = Duration;
    private float _spawnTimer;
    private int _nextBeam;
    private bool _stopping;
    private ColorRect? _darkOverlay;
    private Tween? _overlayTween;
    private StageLightTargetArea _lastTargetArea;

    private NKarenStageLightVfx(Creature target, bool persistent)
    {
        _target = target;
        _persistent = persistent;
        ZAsRelative = true;
        ZIndex = 20;
    }

    public static void Play(Creature target)
    {
        if (NCombatRoom.Instance?.CombatVfxContainer == null) return;

        var vfx = new NKarenStageLightVfx(target, persistent: false);
        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        vfx.GlobalPosition = Vector2.Zero;
        KarenAudioManager.PlaySfx(KarenSfx.StageLight, volume: 0.9f);
    }

    public static NKarenStageLightVfx? StartFocus(Creature target)
    {
        if (NCombatRoom.Instance?.CombatVfxContainer == null) return null;

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
        if (!_stopping && !(NCombatRoom.Instance?.CreatureNodes.Any(creature => creature.Entity == _target) ?? false)) Stop();
        float d = (float)delta;
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

                AddChild(new NKarenStageLightBeam(StageLightTexture, GetTargetArea, BeamDegrees[_nextBeam], persistent: false));
                _nextBeam++;
            }
        }

        if ((_duration < -0.05f || _stopping) && GetChildCount() == 0 && !GodotObject.IsInstanceValid(_darkOverlay))
            GodotTreeExtensions.QueueFreeSafely(this);
    }

    private StageLightTargetArea GetTargetArea()
    {
        var creatureNode = NCombatRoom.Instance?.CreatureNodes.FirstOrDefault(creature => creature.Entity == _target);
        if (creatureNode != null)
        {
            // Convert both corners through the complete canvas transform (including scale/flip).
            var hitbox = creatureNode.Hitbox;
            var first = ToLocal(hitbox.GetGlobalTransform() * Vector2.Zero);
            var last = ToLocal(hitbox.GetGlobalTransform() * hitbox.Size);
            _lastTargetArea = new StageLightTargetArea((first + last) * 0.5f, (last - first).Abs());
        }

        // A removed target leaves its fading lights at the last valid position.
        return _lastTargetArea;
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
    private const float Duration = 0.6f;
    private const float TargetWidth = 130f;
    private const float TargetHeight = 760f;
    private const float OffscreenTopMargin = 80f;

    private readonly bool _persistent;
    private readonly Func<StageLightTargetArea> _getTargetArea;
    private float _duration = Duration;
    private bool _stopping;

    public NKarenStageLightBeam(Texture2D? texture, Func<StageLightTargetArea> getTargetArea, float degrees, bool persistent)
    {
        _persistent = persistent;
        _getTargetArea = getTargetArea;
        Texture = texture;
        Centered = false;
        ZAsRelative = true;
        ZIndex = 20;
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };

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
            Scale = new Vector2(TargetWidth / Texture.GetWidth(), height / Texture.GetHeight());
    }

    private static float GetClampedHeight(Vector2 endPosition, Vector2 beamDirection)
    {
        if (beamDirection.Y <= 0f)
            return TargetHeight;

        var heightToOffscreenTop = (endPosition.Y + OffscreenTopMargin) / beamDirection.Y;
        return Mathf.Max(TargetHeight, heightToOffscreenTop);
    }

    public void Stop()
    {
        _stopping = true;
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;
        UpdatePlacement();
        if (!_persistent || _stopping)
            _duration -= d;

        float alpha = Mathf.Clamp(_duration / Duration, 0f, 1f);
        Modulate = new Color(1f, 1f, 1f, alpha);

        if (_duration <= 0f)
            GodotTreeExtensions.QueueFreeSafely(this);
    }
}

internal partial class NKarenStageLightFocusBeam : Sprite2D
{
    private const float TargetWidth = 170f;
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
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        if (texture != null)
            Offset = new Vector2(-texture.GetWidth() * 0.5f, -texture.GetHeight());

        UpdatePosition();
    }

    public void Stop()
    {
        _stopping = true;
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
