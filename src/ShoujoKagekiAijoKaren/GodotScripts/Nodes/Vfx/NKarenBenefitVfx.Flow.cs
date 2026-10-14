using Godot;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

public partial class NKarenBenefitVfx
{
    private int FlowMode => Design switch {
        BenefitKind.Cake=>0, BenefitKind.Snack=>1, BenefitKind.Dinner=>2,
        BenefitKind.Ticket or BenefitKind.TicketTrigger=>3,
        BenefitKind.Dream or BenefitKind.DreamTrigger=>4,
        BenefitKind.Shine=>5, BenefitKind.Radiance=>6, _=>7 };

    private Rect2 FlowRect()
    {
        Vector2 center,size;
        switch(Design) {
            case BenefitKind.Cake: center=Bottle;size=new(290,280);break;
            case BenefitKind.Snack: center=Body+new Vector2(0,-12)*Unit;size=new(355,345);break;
            case BenefitKind.Dinner: center=Body+new Vector2(0,-5)*Unit;size=new(340,350);break;
            case BenefitKind.Ticket: case BenefitKind.TicketTrigger:
                center=StageCenter;size=new(310,295);break;
            case BenefitKind.Dream: case BenefitKind.DreamTrigger:
                center=StageCenter+Vector2.Down*30*Unit;size=new(430,295);break;
            case BenefitKind.Radiance: center=RadianceSource;size=new(285,260);break;
            case BenefitKind.Shine:
                return PartyBounds().Grow(72*Unit);
            default:center=StoryPoint;size=new(210,210);break;
        }
        return new Rect2(center-size*Unit/2,size*Unit);
    }
    private void DrawFlowPlane(Node2D plane)
    {
        if(Design==BenefitKind.Radiance&&!UsesPlayedCardSource)return;
        if(Design!=BenefitKind.Shine){plane.DrawTextureRect(_soft,FlowRect(),false);return;}
        foreach(var player in Owner.Creature.CombatState!.Players){
            if(Room.GetCreatureNode(player.Creature)==null)continue;
            var foot=PlayerFoot(player);
            var size=new Vector2(300,550)*Unit;
            plane.DrawTextureRect(_soft,new Rect2(foot+new Vector2(-size.X*.5f,-size.Y+12*Unit),size),false);
        }
    }
    private void UpdateFlowMaterial(ShaderMaterial? material)
    {
        if(material==null)return;
        material.SetShaderParameter("age",Age);
        material.SetShaderParameter("result_age",Confirmed?ResultAge:-1f);
        material.SetShaderParameter("mode",FlowMode);
        material.SetShaderParameter("seed",_seed%31);
        material.SetShaderParameter("strength",StoryAlpha);
        material.SetShaderParameter("trigger",Trigger);
        material.SetShaderParameter("failed",this is NKarenBananaVfx {PotionSucceeded:false});
    }
    private void Grain(Node2D layer,Vector2 point,float size,Color color,float alpha,float angle=0)
        =>Sprite(layer,_ash,point,new Vector2(size,size*.75f)*Unit,color,alpha,angle);

    private void WarmSmoke(Vector2 origin,float elapsed,float width,float height,int count,float opacity)
    {
        if(_smoke==null)return;
        var cell=_smoke.GetSize()/2;
        for(int i=0;i<count;i++) {
            float u=(elapsed-Random(i+42)*.4f)/(.65f+Random(i+21)*.5f);
            if(u<=0||u>=1)continue;
            var point=origin+new Vector2((Random(i+8)-.5f)*width,-Ease(u)*height)*Unit;
            var size=Vector2.One*(75+Random(i+6)*65)*(.5f+Ease(u))*Unit;
            int frame=Mathf.Min(3,(int)(u*4));
            Sprite(_back,_smoke,point,size,new("#dfb985"),Mathf.Sin(u*Mathf.Pi)*opacity,i+u*.2f,
                new Rect2(new Vector2(frame%2,frame/2)*cell,cell));
        }
    }
}
