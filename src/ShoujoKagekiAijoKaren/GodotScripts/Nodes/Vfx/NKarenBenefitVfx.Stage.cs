using Godot;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

public partial class NKarenBenefitVfx
{
    private static readonly Color TicketPearl=new("#f3f0e7"),TicketBlue=new("#bfd2f5");
    private Vector2 StageCenter=>Body+(Design is BenefitKind.Dream or BenefitKind.DreamTrigger?new Vector2(135,-110):new Vector2(135,-75))*Unit;

    private Rect2 PartyBounds()
    {
        var box=new Rect2(Floor,Vector2.Zero);
        foreach(var player in Owner.Creature.CombatState!.Players) {
            var node=Room.GetCreatureNode(player.Creature);if(node==null)continue;
            var hit=node.Hitbox;
            var bottom=GetGlobalTransformWithCanvas().AffineInverse()*(hit.GetGlobalTransformWithCanvas()*new Vector2(hit.Size.X*.5f,hit.Size.Y));
            box=box.Expand(bottom);
        }
        return new Rect2(new Vector2(box.Position.X,box.GetCenter().Y-10*Unit),new Vector2(Mathf.Max(80*Unit,box.Size.X),20*Unit));
    }
    private Vector2 PlayerFoot(MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        var node=Room.GetCreatureNode(player.Creature);if(node==null)return Floor;
        var hit=node.Hitbox;
        return GetGlobalTransformWithCanvas().AffineInverse()*(hit.GetGlobalTransformWithCanvas()*new Vector2(hit.Size.X*.5f,hit.Size.Y));
    }
    private void DrawStageBack()
    {
        if(Design==BenefitKind.Radiance)return;
        if(Design!=BenefitKind.Shine)return;
        float lit=P(Age,0,.25f);
        foreach(var player in Owner.Creature.CombatState!.Players) {
            var node=Room.GetCreatureNode(player.Creature);if(node==null)continue;
            var foot=PlayerFoot(player);
            Sprite(_back,_soft,foot+Vector2.Up*4*Unit,new Vector2(135,24)*Unit,Gold,lit*StoryAlpha*.65f);
            Sprite(_back,_soft,foot+Vector2.Up*12*Unit,new Vector2(88,32)*Unit,Cream,lit*StoryAlpha*.35f);
            if(_recipients.TryGetValue(player,out float at))
                Sprite(_back,_soft,foot+Vector2.Up*25*Unit,new Vector2(125,80)*Unit,Gold,Window(Age-at,0,1.1f,.055f,.55f)*.3f);
        }
    }
    private void DrawStageFlow()
    {
        if(Design==BenefitKind.Radiance)return;
        if(Design==BenefitKind.Shine){DrawSharedFootlights();return;}
        if(Design is BenefitKind.Ticket or BenefitKind.TicketTrigger){DrawCurtainDust();return;}
        DrawDreamCurrents();
    }
    private void DrawCurtainDust()
    {
        for(int i=0;i<54;i++) {
            float delay=Random(i+9)*.22f;
            float u=(Age-delay)/1.16f;
            if(u<=0||u>=1)continue;
            float angle=-Mathf.Pi/2+(i%3)*Mathf.Tau/3+(Random(i+7)-.5f)*.35f;
            float radius=12+Random(i+19)*32+55*(1-Ease(P(Age-delay,0,.55f)));
            if(Confirmed)radius+=Ease(P(ResultAge,0,.65f))*(25+Random(i+3)*25);
            var point=StageCenter+Polar(angle,radius,.92f);
            float a=Mathf.Sin(u*Mathf.Pi)*StoryAlpha;
            Grain(_front,point,i%8==0?5:2.5f,i%4==0?TicketBlue:TicketPearl,a*.65f,i*.7f);
        }
        if(Confirmed)Glow(_front,StageCenter,88,TicketBlue,Window(ResultAge,0,.45f,.04f,.30f)*.3f);
    }
    private void DrawDreamCurrents()
    {
        for(int side=0;side<2;side++) {
            var color=side==0?new Color("#ffc38d"):new Color("#9be1ff");
            float direction=side==0?-1:1;
            var origin=StageCenter+new Vector2(direction*140,72)*Unit;
            for(int i=0;i<40;i++) {
                float start=Random(i+side*61)*.32f;
                float u=(Age-start)/.8f;
                if(u<=0||u>=1)continue;
                var from=origin+new Vector2((Random(i+13)-.5f)*62,(Random(i+42)-.5f)*50)*Unit;
                var point=Arc(from,StageCenter,Ease(u),15+Random(i+2)*30);
                float a=Mathf.Sin(u*Mathf.Pi)*StoryAlpha;
                Grain(_front,point,3+Random(i+7)*5,color,a*.8f,i+u);
                if(i%13==0)Star(_front,point,5,0,color,a*.6f);
            }
        }
        if(Confirmed) {
            float a=Window(ResultAge,0,.65f,.04f,.4f);
            Star(_front,StageCenter,Design==BenefitKind.DreamTrigger?23:18,0,Ivory,a*.7f);
        }
    }
    private void DrawSharedFootlights()
    {
        foreach(var pair in _recipients) {
            var node=Room.GetCreatureNode(pair.Key.Creature);if(node==null)continue;
            var foot=PlayerFoot(pair.Key);
            float elapsed=Age-pair.Value,u=Ease(P(elapsed,0,.58f));
            float a=Window(elapsed,0,1.1f,.055f,.55f);
            var target=foot+new Vector2(0,-85)*Unit;
            var point=foot.Lerp(target,u);
            Trail(_front,foot,target,u,Gold,a*.6f,5);
            Star(_front,point,9+8*(1-u),0,Cream,a*.8f);
        }
    }
}
