using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>A single short coral diagonal cut at the real hit.</summary>
public partial class NKarenStrikeVfx:NKarenBasicAttackVfx
{
    protected override float TailTime=>.14f;
    public static NKarenStrikeVfx? Create(Creature source,Creature target)=>Create<NKarenStrikeVfx>(source,target);
    protected override void DrawAttack()
    {
        float side=Contact.X>=Source.X?1:-1;
        var direction=new Vector2(side*.65f,-.76f);
        var start=Contact-direction*32*Unit;
        var end=Contact+direction*40*Unit;
        var tip=start.Lerp(end,Mathf.SmoothStep(0,1,Mathf.Clamp(Age/.025f,0,1)));
        float t=Mathf.Clamp(Age/TailTime,0,1);
        float alpha=1-Mathf.SmoothStep(.02f,TailTime,Age);
        Blade(start.Lerp(tip,t*.32f),tip,5.5f*(1-t*.65f),Coral,alpha);
    }
}
