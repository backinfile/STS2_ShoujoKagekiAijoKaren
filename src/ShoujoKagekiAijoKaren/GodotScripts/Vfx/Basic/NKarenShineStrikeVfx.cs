using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>A slim white-gold straight cut with one small contact star.</summary>
public partial class NKarenShineStrikeVfx:NKarenBasicAttackVfx
{
    protected override float TailTime=>.16f;
    public static NKarenShineStrikeVfx? Create(Creature source,Creature target)=>Create<NKarenShineStrikeVfx>(source,target);
    protected override void DrawAttack()
    {
        var direction=(Contact-Source).Normalized();
        float t=Mathf.Clamp(Age/TailTime,0,1);
        float alpha=1-Mathf.SmoothStep(.025f,TailTime,Age);
        Blade(Contact-direction*(45-18*t)*Unit,Contact+direction*35*Unit,3.5f*(1-t*.65f),Gold,alpha);
        Geometry.Star(Contact,(6-3*t)*Unit,.18f,new Color(Ivory,alpha*.8f));
    }
}
