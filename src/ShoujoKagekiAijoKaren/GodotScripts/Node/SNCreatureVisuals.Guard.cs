using Godot;

namespace ShoujoKagekiAijoKaren.Core;

public partial class SNCreatureVisuals
{
    private uint _guardGeneration;
    private bool _guardSettle;
    private Vector2 _guardOffset;
    private float _guardLean, _guardCompression;
    public bool IsGuardPoseActive { get; private set; }
    internal bool CanPresentKarenGuard => _idleMaterial!=null && _idleSprite?.Material==_idleMaterial;
    internal bool OwnsKarenGuard(uint generation) => IsGuardPoseActive && generation==_guardGeneration;

    public uint BeginKarenGuard(float side,bool settle=false)
    {
        CancelKarenEntrance();
        ++_guardGeneration;
        _guardSettle=settle;
        IsGuardPoseActive=_idleSprite?.Material==_idleMaterial && _idleMaterial!=null;
        AdvanceKarenGuard(_guardGeneration,0,side);
        return _guardGeneration;
    }
    public void AdvanceKarenGuard(uint generation,float age,float side)
    {
        if (generation!=_guardGeneration || !IsGuardPoseActive) return;
        if (_idleSprite == null || _idleSprite.Material!=_idleMaterial || !_idleSprite.IsVisibleInTree())
        { CancelKarenGuard(generation); return; }
        float set=EntranceEase(age,0,.07f)*(1-EntranceEase(age,_guardSettle?.12f:.19f,_guardSettle?.26f:.42f));
        float localSide=_idleSprite.GetGlobalTransformWithCanvas().AffineInverse().BasisXform(new Vector2(side,0)).X>=0 ? 1 : -1;
        _guardOffset=new Vector2(_guardSettle?0:-10*localSide,3)*set;
        _guardLean=(_guardSettle?-.012f:-.038f)*localSide*set;
        _guardCompression=(_guardSettle?.018f:.025f)*set;
        ApplyKarenGuard();
    }
    public void CancelKarenGuard(uint generation=0)
    {
        if (generation!=0 && generation!=_guardGeneration) return;
        IsGuardPoseActive=false;
        _guardOffset=Vector2.Zero;_guardLean=_guardCompression=0;
        ApplyKarenGuard();
    }
    internal Vector2 KarenGuardCanvasPoint(Vector2 textureUv)
    {
        if (_idleSprite?.Texture==null) return GetGlobalTransformWithCanvas()*Vector2.Zero;
        var size=_idleSprite.Texture.GetSize();var pivot=new Vector2(0,size.Y*.46f);
        var point=(textureUv-new Vector2(.5f,.5f))*size-pivot;
        point.Y*=1-_guardCompression;
        return _idleSprite.GetGlobalTransformWithCanvas()*(point.Rotated(_guardLean)+pivot+_guardOffset);
    }
    private void ApplyKarenGuard()
    {
        if (_idleMaterial==null) return;
        _idleMaterial.SetShaderParameter("guard_offset",_guardOffset);
        _idleMaterial.SetShaderParameter("guard_lean",_guardLean);
        _idleMaterial.SetShaderParameter("guard_compression",_guardCompression);
        _idleMaterial.SetShaderParameter("guard_pivot",new Vector2(0,(_idleSprite?.Texture?.GetHeight()??0)*.46f));
    }
}
