using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

public partial class NKarenStageCueVfx
{
    private static readonly Color TicketSilk=new("#ff697b"),TicketIvory=new("#ffe6bb");
    private float CueUnit=>_unit*1.55f;
    private Vector2 Act1Center=>_body+new Vector2(12,-25)*CueUnit;
    private Vector2 Act1StarPoint(float u)=>Act1Center+new Vector2(-158*Mathf.Cos(u*Mathf.Pi),-190*Mathf.Sin(u*Mathf.Pi))*CueUnit;
    private float ShortCueAlpha=>Kind switch {
        CueKind.TicketCast=>Win(_age,0,.80f,.045f,.22f),
        CueKind.Act1Cast=>Win(_age,0,.96f,.06f,.28f),
        CueKind.TicketReturn=>_drawAt<0?Win(_age,0,.30f,.03f,.12f):Win(CardAccentAge,0,.78f,.035f,.22f),
        _=>Mathf.Max(Win(_age,0,.88f,.05f,.28f),_drawAt<0?0:Win(CardAccentAge,0,.64f,.035f,.24f))
    };
    private float CardAccentAge=>_age-_cardTimes.Values.DefaultIfEmpty(_drawAt).Max();

    private void DrawAct1Opening()
    {
        float alpha=Win(_age,0,Kind==CueKind.Act1Cast?.96f:.88f,.06f,.28f);
        float head=P(_age,0,.34f);
        Vector2 previous=Act1StarPoint(.03f);
        for(int i=1;i<=64;i++){
            float u=Mathf.Lerp(.03f,.97f,i/64f);
            var point=Act1StarPoint(u);
            float lit=P(head,u-.14f,u+.015f);
            _back.DrawLine(previous,point,new Color(Blue,alpha*lit*.62f),.95f*CueUnit,true);
            previous=point;
        }
        for(int i=0;i<7;i++){
            float u=.06f+i*.88f/6;
            float lit=P(head,u-.14f,u+.015f);
            var tint=i%2==0?Gold:White;
            KarenLightRibbon.Star(_back,Act1StarPoint(u),(i==3?18:11)*CueUnit,tint,alpha*lit);
        }
    }

    private Vector2 DrawPilePoint()
    {
        if(LocalContext.IsMe(_owner)){
            var icon=_room.Ui.DrawPile.GetNode<TextureRect>("Icon");
            return icon.GetGlobalTransformWithCanvas()*(icon.Size*.5f);
        }
        return PowerPoint();
    }

    private void DrawAct1Cards()
    {
        if(Kind==CueKind.Act1Cast)return;
        var from=Act1StarPoint(.78f);var target=DrawPilePoint();
        Vector2 Path(float t)=>KarenLightRibbon.Curve(from,from+new Vector2(125,45)*CueUnit,
            target+new Vector2(130,-155)*CueUnit,target,t);
        float travel=P(_age,.19f,.68f),alpha=Win(_age,.16f,.88f,.045f,.20f);
        KarenLightRibbon.Draw(_hud,Path,travel*1.55f,.58f,4.2f*CueUnit,Blue,alpha);
        if(travel<.72f)KarenLightRibbon.Star(_hud,Path(travel*1.4f),13*CueUnit,White,alpha);
        foreach(CardModel model in _cards){
            if(_cardTimes[model]<0 || model.Pile?.Type!=PileType.Hand)continue;
            var card=VisibleCard(model);if(card==null)continue;
            float t=_age-_cardTimes[model],u=Ease(P(t,0,.40f)),a=Win(t,0,.64f,.035f,.24f);
            var frame=card.GetNode<TextureRect>("%Frame");var transform=frame.GetGlobalTransformWithCanvas();
            var left=transform*(frame.Size*new Vector2(.08f,.025f));
            var right=transform*(frame.Size*new Vector2(.92f,.025f));
            _hud.DrawLine(left,right,new Color(Gold,a*.78f),2*CueUnit,true);
            KarenLightRibbon.Star(_hud,left.Lerp(right,u),11*CueUnit,White,a);
            KarenLightRibbon.Star(_hud,left.Lerp(right,.68f),7*CueUnit,Gold,a*.75f);
        }
    }

    private static NCard? VisibleCard(CardModel model)
    {
        var card=NCard.FindOnTable(model);
        return GodotObject.IsInstanceValid(card)&&card!.Model==model&&card.IsInsideTree()
            &&card.IsVisibleInTree()&&!card.IsQueuedForDeletion()?card:null;
    }
    private void TicketFragments(Vector2 target,float age,float alpha)
    {
        float u=P(age,.28f,.78f);
        for(int i=0;i<4;i++){
            var direction=new Vector2(.5f+i*.26f,-1.15f+i*.19f);
            var point=target+direction*(12+u*46)*CueUnit;
            var along=direction.Normalized()*CueUnit;
            var normal=along.Orthogonal();
            var tint=new Color(i%2==0?TicketSilk:TicketIvory,alpha*(1-u));
            _hud.DrawColoredPolygon([point+along*12,point-along*5+normal*3,point-along*8-normal*2],tint);
        }
    }
    private void DrawTicketCards()
    {
        if(Kind==CueKind.TicketCast){
            var source=_sourceCaptured?_sourcePoint:_body;
            float head=P(_age,0,.64f)*1.85f,alpha=ShortCueAlpha;
            for(int side=-1;side<=1;side+=2){
                Vector2 Path(float t)=>KarenLightRibbon.Curve(source+new Vector2(side*90,48)*CueUnit,
                    source+new Vector2(-side*155,8)*CueUnit,source+new Vector2(-side*152,-88)*CueUnit,
                    source+new Vector2(side*48,-125)*CueUnit,t);
                KarenLightRibbon.Draw(_hud,Path,head,.88f,(side<0?8:6)*CueUnit,side<0?TicketSilk:TicketIvory,alpha);
            }
            TicketFragments(source+new Vector2(48,-125)*CueUnit,_age,alpha*.75f);
            return;
        }
        foreach(var model in _cards){
            if(_cardTimes[model]<0 || model.Pile?.Type!=PileType.Hand)continue;
            var card=VisibleCard(model);if(card==null)continue;
            var frame=card.GetNode<TextureRect>("%Frame");
            var target=frame.GetGlobalTransformWithCanvas()*(frame.Size*new Vector2(.82f,.025f));
            float t=_age-_cardTimes[model],head=P(t,0,.65f)*1.85f,alpha=Win(t,0,.78f,.035f,.22f);
            var source=PowerPoint();var normal=(target-source).Orthogonal().Normalized();
            Vector2 Path(float u)=>source.Lerp(target,u)+Vector2.Up*Mathf.Sin(u*Mathf.Pi)*125*CueUnit
                +normal*Mathf.Sin(u*Mathf.Tau)*Mathf.Sin(u*Mathf.Pi)*62*CueUnit;
            KarenLightRibbon.Draw(_hud,Path,head,.86f,7.5f*CueUnit,TicketSilk,alpha);
            if(t>.24f){
                KarenLightRibbon.Star(_hud,target,13*CueUnit,TicketIvory,Win(t,.24f,.70f,.035f,.22f));
                TicketFragments(target,t,alpha);
            }
        }
    }
}
