using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

public partial class NKarenStageCueVfx
{
    private static readonly Color TicketSilk = new("#fa6279"), TicketIvory = new("#fff0d3");
    private Vector2 Act1Center => _body+new Vector2(70,-100)*_unit;
    private Vector2 Act1StarPoint(float u) => Act1Center+new Vector2(-100+200*u,-Mathf.Sin(u*Mathf.Pi)*54)*_unit;
    private float ShortCueAlpha => Kind switch {
        CueKind.TicketCast => Win(_age,0,.56f,.04f,.18f),
        CueKind.Act1Cast => Win(_age,0,.62f,.04f,.20f),
        CueKind.TicketReturn => _drawAt<0?Win(_age,0,.35f,.03f,.14f):Win(CardAccentAge,0,.48f,.03f,.17f),
        _ => _drawAt<0?Win(_age,0,.42f,.03f,.16f):Win(CardAccentAge,0,.5f,.03f,.2f)
    };
    private float CardAccentAge => _age-_cardTimes.Values.DefaultIfEmpty(_drawAt).Max();
    private void DrawAct1Opening()
    {
        float alpha=Win(_age,0,.62f,.05f,.22f);
        float head=Ease(P(_age,0,.26f));
        for(int i=0;i<5;i++){
            float u=i/4f;
            float lit=P(head,u-.12f,u+.08f);
            Star(_back,Act1StarPoint(u),i==2?8:5,i%2==0?Gold:White,alpha*lit*.78f);
            if(i>0){var c=Blue;c.A=alpha*lit*.27f;_back.DrawLine(Act1StarPoint((i-1)/4f),Act1StarPoint(u),c,1.0f*_unit,true);}
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
        if(Kind==CueKind.Act1Cast){
            if(_confirmedAt<0)return;
            float t=_age-_confirmedAt,u=Ease(P(t,0,.20f)),alpha=Win(t,0,.32f,.02f,.16f);
            Trail(_hud,Act1StarPoint(.5f),PowerPoint(),u,Gold,alpha*.45f,20);
            return;
        }
        // A draw is signalled only by this actual Shine play; delivered-card accents
        // are attached below to the returned models, never to predicted hand slots.
        float head=P(_age,0,.38f)*1.40f;
        var from=_sourceCaptured?_sourcePoint:PowerPoint();var target=DrawPilePoint();
        KarenLightRibbon.Draw(_hud,t=>KarenLightRibbon.Curve(from,from+Vector2.Up*45*_unit,
            target+Vector2.Up*55*_unit,target,t),head,.38f,2.4f*_unit,Blue,Win(_age,0,.48f,.03f,.18f)*.70f);
        foreach(CardModel model in _cards){
            if(_cardTimes[model]<0 || model.Pile?.Type!=MegaCrit.Sts2.Core.Entities.Cards.PileType.Hand)continue;
            var card=NCard.FindOnTable(model);
            if(card==null || card.Model!=model || !card.IsVisibleInTree() || !card.IsInsideTree())continue;
            float t=_age-_cardTimes[model],u=Ease(P(t,0,.25f)),alpha=Win(t,0,.45f,.025f,.20f);
            var frame=card.GetNode<TextureRect>("%Frame");var transform=frame.GetGlobalTransformWithCanvas();
            var center=transform*(frame.Size*new Vector2(.5f,0));
            var edge=transform*(frame.Size*new Vector2(u,0));
            Soft(_hud,edge,new Vector2(38,10)*_unit,Gold,alpha*.55f,transform.X.Angle());
            Star(_hud,center,4.5f,White,alpha*.80f);
        }
    }
    private void DrawTicketCards()
    {
        if(Kind==CueKind.TicketCast){
            var source=_sourceCaptured?_sourcePoint:_body;
            float head=P(_age,0,.37f)*1.40f,alpha=Win(_age,0,.46f,.025f,.18f);
            for(int side=-1;side<=1;side+=2){
                Vector2 Path(float t)=>KarenLightRibbon.Curve(source+new Vector2(side*50,25)*_unit,
                    source+new Vector2(-side*85,-42)*_unit,source+new Vector2(side*75,-55)*_unit,
                    source+new Vector2(-side*28,-18)*_unit,t);
                KarenLightRibbon.Draw(_hud,Path,head,.45f,5*_unit,side<0?TicketSilk:TicketIvory,alpha*.80f);
            }
            if(_confirmedAt>=0)Star(_hud,PowerPoint(),6,TicketIvory,Win(_age-_confirmedAt,0,.23f,.02f,.14f)*.65f);
            return;
        }
        foreach(var model in _cards){
            if(_cardTimes[model]<0 || model.Pile?.Type!=MegaCrit.Sts2.Core.Entities.Cards.PileType.Hand)continue;
            if(CardPoint(model) is not {} target)continue;
            float t=_age-_cardTimes[model],head=P(t,0,.34f)*1.40f,alpha=Win(t,0,.48f,.025f,.19f);
            var source=PowerPoint();
            var across=(target-source).Orthogonal().Normalized();
            Vector2 Path(float u)=>source.Lerp(target,u)+across*(Mathf.Sin(u*Mathf.Tau)*28*_unit*Mathf.Sin(u*Mathf.Pi));
            KarenLightRibbon.Draw(_hud,Path,head,.42f,4.7f*_unit,TicketSilk,alpha*.83f);
            KarenLightRibbon.Draw(_hud,Path,head,.18f,1.6f*_unit,TicketIvory,alpha*.75f);
            if(t>.18f)for(int i=0;i<3;i++){
                float u=P(t-.18f,0,.24f);
                var point=target+new Vector2(18+i*12,-8-i*10)*_unit*u;
                var tint=new Color(i%2==0?TicketSilk:TicketIvory,alpha*(1-u)*.65f);
                _hud.DrawColoredPolygon([point,point+new Vector2(-7,-2)*_unit,point+new Vector2(-3,3)*_unit],tint);
            }
        }
    }
}
