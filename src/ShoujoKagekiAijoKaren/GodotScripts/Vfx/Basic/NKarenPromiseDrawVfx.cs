using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

/// <summary>A light short cut; actual tower-to-hand cards provide the draw confirmation.</summary>
public partial class NKarenPromiseDrawVfx:NKarenBasicAttackVfx
{
    protected override float TailTime=>.10f;
    public static NKarenPromiseDrawVfx? Create(Creature source,Creature target)=>Create<NKarenPromiseDrawVfx>(source,target);
    protected override void DrawAttack()
    {
        float side=Contact.X>=Source.X?1:-1;
        var direction=new Vector2(side*.82f,-.57f);
        float alpha=1-Mathf.SmoothStep(.015f,TailTime,Age);
        Blade(Contact-direction*23*Unit,Contact+direction*27*Unit,3.2f,Coral,alpha);
    }
}
