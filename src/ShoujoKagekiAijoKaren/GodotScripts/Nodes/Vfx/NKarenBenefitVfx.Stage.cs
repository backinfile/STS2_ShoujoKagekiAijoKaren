using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using ShoujoKagekiAijoKaren.src.Core.PromisePileSystem.Vfx;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

public partial class NKarenBenefitVfx
{
    private static readonly Color TicketPearl=new("#f3f0e7"),TicketBlue=new("#bfd2f5");
    private float DreamUnit=>Unit*1.55f;
    private Vector2 StageCenter=>Design is BenefitKind.Dream or BenefitKind.DreamTrigger
        ? Body+new Vector2(Design==BenefitKind.Dream?70:145,Design==BenefitKind.Dream?-85:-75)*DreamUnit
        : Body+new Vector2(135,-75)*Unit;
    private Vector2 DreamCore=>StageCenter+Vector2.Up*25*DreamUnit;

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
    private static readonly Color DreamWarm = new("#ff9178"), DreamCool = new("#72baff");
    private Vector2 DreamPoint(float u, int side, bool joining)
    {
        var center=StageCenter;
        if(joining){
            float radius=115*(.70f+.30f*Mathf.Sin(u*Mathf.Pi));
            return center+new Vector2(side*Mathf.Sin(u*Mathf.Tau*1.1f)*radius,90-215*u)*DreamUnit;
        }
        return KarenLightRibbon.Curve(center+new Vector2(side*28,85)*DreamUnit,
            center+new Vector2(side*230,105)*DreamUnit,center+new Vector2(side*245,-100)*DreamUnit,
            center+new Vector2(side*76,-135)*DreamUnit,u);
    }
    private void DrawDreamCurrents()
    {
        bool joining=Design==BenefitKind.DreamTrigger;
        float lead=joining?.68f:.50f;
        float u=joining?P(Age,0,lead):Ease(P(Age,0,lead));
        float ribbonHead=joining
            ? P(Age,0,.48f)+P(Age,.95f,1.4f)*1.05f
            : P(Age,0,.92f)*1.90f;
        for(int side=-1;side<=1;side+=2){
            var color=side<0?DreamWarm:DreamCool;
            KarenLightRibbon.Draw(_front,t=>DreamPoint(t,side,joining),ribbonHead,1.05f,6.8f*DreamUnit,color,StoryAlpha);
            KarenLightRibbon.Draw(_front,t=>DreamPoint(t,side,joining)+new Vector2(side*10,-5)*DreamUnit,
                ribbonHead-.08f,.94f,1.8f*DreamUnit,color,StoryAlpha*.75f);
            float starAlpha=StoryAlpha*(joining?1-P(Age,lead,lead+.16f):1);
            var star=joining?DreamPoint(Mathf.Min(u,.64f),side,true).Lerp(DreamCore,Ease(P(Age,.44f,.68f)))
                :DreamPoint(u,side,false);
            KarenLightRibbon.Star(_front,star,(joining?18:23)*DreamUnit,color,starAlpha);
            for(int i=0;i<5;i++){
                float q=.16f+i*.16f;
                float a=Window(Age,q*.30f,joining?.98f:1.15f,.08f,.30f)*StoryAlpha;
                var offset=new Vector2(side*(i%2==0?16:-14),i%2==0?-10:12)*DreamUnit;
                KarenLightRibbon.Star(_front,DreamPoint(q,side,joining)+offset,(i%2==0?4:2.8f)*DreamUnit,color,a*.78f);
            }
        }
        if(joining){
            float alpha=Window(Age,.46f,1.25f,.12f,.30f)*StoryAlpha;
            KarenLightRibbon.Star(_front,DreamCore,29*DreamUnit,Cream,alpha);
            if(Confirmed)Glow(_front,DreamCore,110,Ivory,Window(ResultAge,0,.45f,.02f,.30f)*.28f);
        }
    }
    private void DrawDreamDelivery(Vector2 target,float elapsed)
    {
        float head=P(elapsed,.10f,1.0f)*1.85f;
        float alpha=Window(elapsed,.06f,1.40f,.06f,.36f);
        var from=DreamCore;
        var normal=(target-from).Orthogonal().Normalized();
        for(int side=-1;side<=1;side+=2){
            Vector2 Path(float t)=>Arc(from,target,t,100)
                +normal*(side*Mathf.Sin(t*Mathf.Pi)*20*DreamUnit);
            var tint=side<0?DreamWarm:DreamCool;
            KarenLightRibbon.Draw(_front,Path,head,.90f,5.6f*DreamUnit,tint,alpha);
            for(int i=0;i<4;i++){
                float q=.22f+i*.18f;
                var point=Path(q)+new Vector2(0,(i%2==0?12:-12)*DreamUnit);
                KarenLightRibbon.Star(_front,point,3.5f*DreamUnit,tint,alpha*Window(elapsed,q*.5f,.95f,.06f,.25f));
            }
        }
        KarenLightRibbon.Star(_front,target,15*DreamUnit,Cream,Window(elapsed,.42f,1.35f,.06f,.38f));
    }
    private void ShowDreamCards()
    {
        // Generating directly into a non-hand pile has no native card node.
        // Show the confirmed models with native NCard art; no extra cards or rewards.
        int i=0;
        foreach(var model in _deliveries.Take(10)){
            if(_dreamCards.ContainsKey(model))continue;
            var card=NCard.Create(model);if(card==null)continue;
            _hud.AddChild(card);card.UpdateVisuals(PileType.None,CardPreviewMode.Normal);
            var frame=card.GetNode<TextureRect>("%Frame");
            float height=frame.Size.Y*frame.GetGlobalTransformWithCanvas().Y.Length();
            card.Scale*=100*DreamUnit*GetGlobalTransformWithCanvas().Y.Length()/Mathf.Max(1,height);
            _dreamCards[model]=card;_dreamCardScales[model]=card.Scale;
            _dreamFlights[model]=Age+i++*.12f;card.Hide();
        }
        UpdateDreamCards();
    }
    private void UpdateDreamCards()
    {
        foreach(var pair in _dreamCards.ToArray()){
            var card=pair.Value;
            if(!GodotObject.IsInstanceValid(card)||card.IsQueuedForDeletion()||!card.IsInsideTree()
                ||card.GetParent()!=_hud||card.Model!=pair.Key){ReleaseDreamCard(pair.Key);continue;}
            float elapsed=Age-_dreamFlights[pair.Key];
            if(elapsed<0){card.Hide();continue;}
            if(elapsed>=1.08f||pair.Key.Pile==null||!KarenPromiseVfxStarManager.IsTowerPile(Owner,pair.Key.Pile.Type)){
                ReleaseDreamCard(pair.Key);continue;
            }
            var anchor=pair.Key.Pile.Type==KarenCustomEnum.PromisePile
                ? KarenPromiseVfxStarManager.GetStarPosition(pair.Key,this)
                : KarenPromiseVfxStarManager.GetPilePosition(Owner,this);
            if(anchor is not {} target)continue;
            float u=P(elapsed,.18f,1.02f);u=u*u*(3-2*u);
            var origin=DreamCore+Vector2.Down*62*DreamUnit;
            var point=Arc(origin,ToLocal(target),u,42);
            card.Scale=_dreamCardScales[pair.Key]*(1-Ease(P(elapsed,.40f,1.08f))*.94f);
            card.Modulate=new Color(Colors.White,Window(elapsed,0,1.08f,.055f,.20f));
            var portrait=card.GetNode<TextureRect>("%Portrait");
            var current=portrait.GetGlobalTransformWithCanvas()*(portrait.Size*.5f);
            var destination=GetGlobalTransformWithCanvas()*point;var canvas=card.GetCanvasTransform().AffineInverse();
            card.GlobalPosition+=canvas*destination-canvas*current;
            card.Show();
        }
    }
    private void ClearDreamCards()
    {
        foreach(var model in _dreamCards.Keys.ToArray())ReleaseDreamCard(model);
    }
    private void ReleaseDreamCard(MegaCrit.Sts2.Core.Models.CardModel model)
    {
        // Pool returns keep a valid Godot object. Drop ownership before returning
        // it, so a later frame or room cleanup cannot free a reused card again.
        if(!_dreamCards.Remove(model,out var card))return;
        _dreamCardScales.Remove(model);
        if(GodotObject.IsInstanceValid(card)&&!card.IsQueuedForDeletion()&&card.Model==model&&card.GetParent()==_hud){
            card.Hide();card.QueueFreeSafely();
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
