using Godot;

namespace ShoujoKagekiAijoKaren.Core;

public partial class SNCreatureVisuals
{
    private uint _attackGeneration;
    private int _attackOriginalZ;
    private bool _attackOriginalRelative, _attackBorrowedZ, _attackAwaitingNativeStart;
    private static readonly StringName AttackOffsetParameter = new("attack_offset");
    private static readonly StringName AttackLeanParameter = new("attack_lean");
    private static readonly StringName AttackPivotParameter = new("attack_pivot");
    private Vector2 _attackOffset, _attackCanvasOffset;
    private float _attackLean;
    public bool IsAttackPoseActive { get; private set; }
    internal bool OwnsKarenAttack(uint ticket) => IsAttackPoseActive && ticket == _attackGeneration;
    internal Texture2D? KarenAttackTexture => _idleSprite?.Texture;
    internal uint BeginKarenAttack(bool expectsNativeAttack)
    {
        CancelKarenAttack(); CancelKarenEntrance(); CancelKarenGuard();
        ++_attackGeneration;
        _attackAwaitingNativeStart = expectsNativeAttack;
        IsAttackPoseActive = _idleMaterial != null && _idleSprite?.Material == _idleMaterial;
        if (IsAttackPoseActive && _idleSprite != null)
        {
            _attackOriginalZ = _idleSprite.ZIndex; _attackOriginalRelative = _idleSprite.ZAsRelative;
            _attackBorrowedZ = true; _idleSprite.ZAsRelative = false; _idleSprite.ZIndex = 24;
        }
        SetKarenAttackPose(_attackGeneration, Vector2.Zero, 0);
        return _attackGeneration;
    }
    internal void SetKarenAttackPose(uint ticket, Vector2 canvasOffset, float lean)
    {
        if (!OwnsKarenAttack(ticket)) return;
        if (_idleSprite == null || !_idleSprite.IsVisibleInTree() || _idleSprite.Material != _idleMaterial)
        { CancelKarenAttack(ticket); return; }
        var inverse = _idleSprite.GetGlobalTransformWithCanvas().AffineInverse();
        _attackCanvasOffset = canvasOffset;
        // Native attack tracks retain their timing. Remove their positional component
        // only inside this owned shader pose so each card has one coherent movement.
        var nativeOffset = _idleSprite.GetParent<CanvasItem>().GetGlobalTransformWithCanvas().BasisXform(_idleSprite.Position - _visualsStartPosition);
        _attackOffset = inverse.BasisXform(canvasOffset - nativeOffset);
        _attackLean = lean * (inverse.BasisXform(Vector2.Right).X >= 0 ? 1 : -1);
        ApplyKarenAttack();
    }
    internal void CancelKarenAttack(uint ticket = 0)
    {
        if (ticket != 0 && ticket != _attackGeneration) return;
        if (_attackBorrowedZ && GodotObject.IsInstanceValid(_idleSprite))
        {
            if (_idleSprite!.ZIndex == 24 && !_idleSprite.ZAsRelative)
            { _idleSprite.ZIndex = _attackOriginalZ; _idleSprite.ZAsRelative = _attackOriginalRelative; }
        }
        _attackBorrowedZ = false; _attackAwaitingNativeStart = false;
        IsAttackPoseActive = false;
        _attackOffset = _attackCanvasOffset = Vector2.Zero; _attackLean = 0;
        ApplyKarenAttack();
    }
    private void OnKarenAttackNativeAnimation(StringName animation)
    {
        if (animation == "idle") return;
        if (animation == "attack" && _attackAwaitingNativeStart)
        { _attackAwaitingNativeStart = false; return; }
        CancelKarenAttack();
    }
    private void ApplyKarenAttack()
    {
        if (_idleMaterial == null) return;
        _idleMaterial.SetShaderParameter(AttackOffsetParameter, _attackOffset);
        _idleMaterial.SetShaderParameter(AttackLeanParameter, _attackLean);
        _idleMaterial.SetShaderParameter(AttackPivotParameter, new Vector2(0, (_idleSprite?.Texture?.GetHeight() ?? 0) * .43f));
    }
    internal Transform2D KarenAttackCanvasTransform
    {
        get
        {
            if (_idleSprite == null) return GetGlobalTransformWithCanvas();
            var pivot = new Vector2(0, (_idleSprite.Texture?.GetHeight() ?? 0) * .43f);
            return _idleSprite.GetGlobalTransformWithCanvas() * new Transform2D(_attackLean, pivot + _attackOffset - pivot.Rotated(_attackLean));
        }
    }
    internal Vector2 KarenAttackCanvasPoint(Vector2 uv)
        => KarenAttackCanvasTransform * ((uv - Vector2.One * .5f) * (_idleSprite?.Texture?.GetSize() ?? Vector2.Zero));
}
