using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System.Linq;

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
            var tween = _darkOverlay.CreateTween();
            tween.TweenProperty(_darkOverlay, "modulate", new Color(1f, 1f, 1f, 0f), FocusOverlayFadeOutDuration);
            tween.TweenCallback(Callable.From(() => GodotTreeExtensions.QueueFreeSafely(_darkOverlay)));
            _darkOverlay = null;
        }
    }

    public override void _Process(double delta)
    {
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
                    AddChild(new NKarenStageLightFocusBeam(StageLightFocusTexture, GetTargetPosition()));
                    _nextBeam = BeamDegrees.Length;
                    break;
                }

                AddChild(new NKarenStageLightBeam(StageLightTexture, GetTargetPosition(), BeamDegrees[_nextBeam], persistent: false));
                _nextBeam++;
            }
        }

        if ((_duration < -0.05f || _stopping) && GetChildCount() == 0)
            GodotTreeExtensions.QueueFreeSafely(this);
    }

    private Vector2 GetTargetPosition()
    {
        var creatureNode = NCombatRoom.Instance?.CreatureNodes.FirstOrDefault(creature => creature.Entity == _target);
        if (creatureNode != null)
            return ToLocal(creatureNode.Hitbox.GlobalPosition + creatureNode.Hitbox.Size * 0.5f);

        return GetViewportRect().Size * 0.5f;
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

        var tween = _darkOverlay.CreateTween();
        tween.TweenProperty(_darkOverlay, "modulate", Colors.White, FocusOverlayFadeInDuration);
    }

    public override void _ExitTree()
    {
        if (_darkOverlay != null && GodotObject.IsInstanceValid(_darkOverlay))
            GodotTreeExtensions.QueueFreeSafely(_darkOverlay);
    }
}

internal partial class NKarenStageLightBeam : Sprite2D
{
    private const float Duration = 0.6f;
    private const float TargetWidth = 130f;
    private const float TargetHeight = 760f;

    private readonly bool _persistent;
    private float _duration = Duration;
    private bool _stopping;

    public NKarenStageLightBeam(Texture2D? texture, Vector2 targetPosition, float degrees, bool persistent)
    {
        _persistent = persistent;
        Texture = texture;
        Centered = false;
        ZAsRelative = true;
        ZIndex = 20;
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };

        RotationDegrees = degrees;
        Modulate = Colors.White;

        var beamDirection = Vector2.Down.Rotated(Mathf.DegToRad(degrees));
        Position = targetPosition - beamDirection * TargetHeight;
        Offset = new Vector2(-TargetWidth * 0.5f, 0f);

        if (texture != null)
            Scale = new Vector2(TargetWidth / texture.GetWidth(), TargetHeight / texture.GetHeight());
    }

    public void Stop()
    {
        _stopping = true;
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;
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

    private Vector2 _targetPosition;
    private bool _stopping;
    private float _alpha = 1f;

    public NKarenStageLightFocusBeam(Texture2D? texture, Vector2 targetPosition)
    {
        _targetPosition = targetPosition;
        Texture = texture;
        Centered = false;
        ZAsRelative = true;
        ZIndex = 20;
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        if (texture != null)
            Offset = new Vector2(-texture.GetWidth() * 0.5f, -texture.GetHeight());

        if (texture != null)
            Scale = new Vector2(TargetWidth / texture.GetWidth(), TargetHeight / texture.GetHeight());

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
        Position = _targetPosition;
        RotationDegrees = 0f;
    }
}
