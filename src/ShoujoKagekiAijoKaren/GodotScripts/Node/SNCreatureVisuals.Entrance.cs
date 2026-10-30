using Godot;

namespace ShoujoKagekiAijoKaren.Core;

public partial class SNCreatureVisuals
{
    private static readonly StringName EntranceOffset = new("entrance_offset");
    private static readonly StringName EntranceLean = new("entrance_lean");
    private static readonly StringName EntranceCape = new("entrance_cape");
    private AnimationPlayer? _entranceAnimationPlayer;
    private Callable _entranceAnimationCallback;
    private Vector2 _entranceOffset;
    private float _entranceLean, _entranceCape;
    public bool IsEntrancePoseActive { get; private set; }
    public float EntranceCapeStrength => _entranceCape;

    private void InitializeEntranceAnimation()
    {
        _entranceAnimationPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (_entranceAnimationPlayer == null) return;
        // Keep the exact callable: generated C# event trampolines are not
        // interchangeable with a fresh Callable.From during teardown.
        _entranceAnimationCallback = Callable.From<StringName>(OnEntranceNativeAnimation);
        _entranceAnimationPlayer.Connect(AnimationMixer.SignalName.AnimationStarted, _entranceAnimationCallback);
    }

    private void OnEntranceNativeAnimation(StringName animation)
    {
        // This is emitted by the existing AnimationPlayer before its first key.
        // No native track, queue, playback speed or completion is changed here.
        if (animation != "idle") CancelKarenEntrance();
    }

    public void BeginKarenEntrance()
    {
        if (_idleMaterial == null || _idleSprite?.Material != _idleMaterial) return;
        if (_entranceAnimationPlayer is { } player && player.IsPlaying()
            && player.CurrentAnimation != "idle" && player.CurrentAnimation != "") return;
        IsEntrancePoseActive = true;
        _idleMaterial.SetShaderParameter("entrance_pivot", new Vector2(0, (_idleSprite.Texture?.GetHeight() ?? 0) * .46f));
        AdvanceKarenEntrance(0);
    }

    public void AdvanceKarenEntrance(float age)
    {
        if (!IsEntrancePoseActive) return;
        if (_idleSprite == null || _idleSprite.Material != _idleMaterial || !_idleSprite.IsVisibleInTree())
        { CancelKarenEntrance(); return; }
        float step = 1 - EntranceEase(age, .16f, .72f);
        float settle = Mathf.Sin(Mathf.Pi * EntranceEase(age, .30f, 1.02f));
        _entranceOffset = new Vector2(-18 * step + 3 * settle, 3 * settle);
        _entranceLean = -.035f * step + .012f * settle;
        _entranceCape = EntranceEase(age, .20f, .55f) * (1 - EntranceEase(age, .85f, 1.22f));
        _idleMaterial!.SetShaderParameter(EntranceOffset, _entranceOffset);
        _idleMaterial.SetShaderParameter(EntranceLean, _entranceLean);
        _idleMaterial.SetShaderParameter(EntranceCape, _entranceCape);
    }

    public void CancelKarenEntrance()
    {
        IsEntrancePoseActive = false;
        _entranceOffset = Vector2.Zero; _entranceLean = _entranceCape = 0;
        // Reset only our uniforms on our scene-local idle material. A temporary
        // power/hurt material belonging to another effect stays installed.
        if (_idleMaterial == null) return;
        _idleMaterial.SetShaderParameter(EntranceOffset, Vector2.Zero);
        _idleMaterial.SetShaderParameter(EntranceLean, 0f);
        _idleMaterial.SetShaderParameter(EntranceCape, 0f);
    }

    public Vector2 EntranceCanvasPoint(Vector2 spriteLocalPoint)
    {
        if (_idleSprite == null) return GetGlobalTransformWithCanvas() * spriteLocalPoint;
        var pivot = new Vector2(0, (_idleSprite.Texture?.GetHeight() ?? 0) * .46f);
        var point = (spriteLocalPoint - pivot).Rotated(_entranceLean) + pivot + _entranceOffset;
        return _idleSprite.GetGlobalTransformWithCanvas() * point;
    }

    private static float EntranceEase(float age, float start, float end)
    { float u = Mathf.Clamp((age - start) / (end - start), 0, 1); return u * u * (3 - 2 * u); }

    public override void _ExitTree()
    {
        CancelKarenEntrance();
        // Native scene teardown may already have removed this connection while
        // the AnimationPlayer instance is still valid.
        if (GodotObject.IsInstanceValid(_entranceAnimationPlayer)
            && _entranceAnimationPlayer!.IsConnected(AnimationMixer.SignalName.AnimationStarted, _entranceAnimationCallback))
            _entranceAnimationPlayer.Disconnect(AnimationMixer.SignalName.AnimationStarted, _entranceAnimationCallback);
        _entranceAnimationPlayer = null;
        _entranceAnimationCallback = default;
        base._ExitTree();
    }
}
