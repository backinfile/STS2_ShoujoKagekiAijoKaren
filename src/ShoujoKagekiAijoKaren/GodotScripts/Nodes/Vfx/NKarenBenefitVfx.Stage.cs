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
    private static readonly Color DreamWarm = new("#ffbb98"), DreamCool = new("#a2dfff");
    private Vector2 DreamPoint(float u, int side, bool joining)
    {
        var center=StageCenter;
        return KarenLightRibbon.Curve(center+new Vector2(side*92,50)*Unit,
            center+new Vector2(side*115,-22)*Unit,center+new Vector2(-side*28,-85)*Unit,
            center+new Vector2(joining?0:side*54,joining?0:-24)*Unit,u);
    }
    private void DrawDreamCurrents()
    {
        bool joining=Design==BenefitKind.DreamTrigger;
        float lead=joining?.28f:.42f;
        float u=Ease(P(Age,0,lead));
        float ribbonHead=P(Age,0,joining?.48f:.70f)*1.45f;
        for(int side=-1;side<=1;side+=2){
            var color=side<0?DreamWarm:DreamCool;
            KarenLightRibbon.Draw(_front,t=>DreamPoint(t,side,joining),ribbonHead,.54f,4.6f*Unit,color,StoryAlpha*.85f);
            float starAlpha=StoryAlpha*(joining?1-P(Age,lead,lead+.15f):1);
            Star(_front,DreamPoint(u,side,joining),joining?9:11,side*Age*.35f,color,starAlpha);
        }
        if(joining){
            float alpha=Window(Age,.18f,.72f,.10f,.24f)*StoryAlpha;
            Star(_front,StageCenter,Confirmed?18:11,0,Ivory,alpha*.72f);
            if(Confirmed)Glow(_front,StageCenter,70,Ivory,Window(ResultAge,0,.35f,.02f,.24f)*.32f);
        }
    }
    private void DrawDreamDelivery(Vector2 target)
    {
        float head=P(ResultAge,.02f,.78f)*1.45f;
        float alpha=Window(ResultAge,0,1.12f,.05f,.32f);
        var from=StageCenter;
        var normal=(target-from).Orthogonal().Normalized();
        for(int side=-1;side<=1;side+=2){
            Vector2 Path(float t)=>Arc(from,target,t,55)
                +normal*(Mathf.Sin(t*Mathf.Tau*1.2f+side*Mathf.Pi*.5f)*Mathf.Sin(t*Mathf.Pi)*10*Unit);
            KarenLightRibbon.Draw(_front,Path,head,.42f,3.8f*Unit,side<0?DreamWarm:DreamCool,alpha*.78f);
        }
        Star(_front,target,8,0,Ivory,Window(ResultAge,.53f,1.12f,.05f,.35f)*.68f);
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
