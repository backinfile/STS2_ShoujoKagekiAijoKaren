using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

public partial class NKarenStageCueVfx
{
    private static readonly Color White = new("#f4f9ff"), Blue = new("#96c2f5"), Gold = new("#fada8e"), Pink = new("#f79fdb");
    private static float P(float t, float a, float b) => Mathf.Clamp((t-a)/(b-a), 0, 1);
    private static float Ease(float t) => 1-Mathf.Pow(1-Mathf.Clamp(t,0,1),3);
    private static float Win(float t, float start, float end, float attack=.05f, float release=.15f) => P(t,start,start+attack)*(1-P(t,end-release,end));
    private Vector2 S(float x,float y) => new(x/1280*_screen.X,y/720*_screen.Y);
    private float Alpha => P(_age,0,.18f)*(_releaseAt<0 ? 1 : 1-P(_age-_releaseAt,Kind==CueKind.Act2Hover?0:.18f,Kind==CueKind.Act2Hover?.18f:.55f));
    private Vector2 Near => _foot+new Vector2(80,12)*_unit;
    private Vector2 Far => Near+new Vector2(185,-70)*_unit;
    private Vector2 DuetCenter => _body+new Vector2(130,-105)*_unit;
    private static Vector2 Arc(Vector2 a,Vector2 b,float u,float h) => a.Lerp(b,u)+Vector2.Up*Mathf.Sin(u*Mathf.Pi)*h;
    private void Soft(Node2D node, Vector2 point, Vector2 size, Color c,float a,float angle=0)
    {
        if(a<=0)return; c.A*=Mathf.Clamp(a,0,1); node.DrawSetTransform(point,angle);
        node.DrawTextureRect(_soft,new Rect2(-size/2,size),false,c);node.DrawSetTransform(Vector2.Zero);
    }
    private void Glow(Node2D node,Vector2 p,float radius,Color c,float a) => Soft(node,p,Vector2.One*radius*_unit,c,a);
    private void Star(Node2D node,Vector2 p,float r,Color c,float a)
    {
        if(a<=0)return;r*=_unit;c.A*=Mathf.Clamp(a,0,1);var pts=new Vector2[8];
        for(int i=0;i<8;i++){float angle=-Mathf.Pi/2+i*Mathf.Tau/8;pts[i]=p+Vector2.FromAngle(angle)*(i%2==0?r:r*.22f);}
        node.DrawColoredPolygon(pts,c);Glow(node,p,r/_unit*4,c,a*.25f);
    }
    private void Trail(Node2D node,Vector2 a,Vector2 b,float u,Color c,float alpha,float h=40)
    {
        for(int i=0;i<14;i++){float q=u-i*.019f;if(q<0)break;Glow(node,Arc(a,b,q,h*_unit),9-i*.4f,c,alpha*(1-i/14f)*.5f);}
    }
    private Vector2? CardPoint(CardModel model)
    {
        var node=NCard.FindOnTable(model);if(node==null||!node.IsInsideTree())return null;
        var portrait=node.GetNode<TextureRect>("%Portrait");return portrait.GetGlobalTransformWithCanvas()*(portrait.Size*.5f);
    }
    private Vector2 PowerPoint()
    {
        var power=_room.FindChildren("*","",true,false).OfType<NPower>().FirstOrDefault(p=>p.Model.Owner==_owner.Creature &&
            (Kind is CueKind.TicketCast or CueKind.TicketReturn ? p.Model is KarenTicketPower : Kind is CueKind.Act1Cast or CueKind.Act1Draw ? p.Model is KarenStarlightPower : p.Model is KarenStarlight02Power));
        return power != null ? power.GetGlobalTransformWithCanvas()*(power.Size*.5f) : _foot+new Vector2(30,25)*_unit;
    }
    private void DrawShade()
    {
        if(Kind!=CueKind.Act2Hover)return;
        _shade.DrawRect(new Rect2(S(0,54),S(1280,482)),new Color(.035f,.07f,.15f,Alpha*.18f));
    }
    private void DrawBackground()
    {
        if(Kind==CueKind.Act2Hover){
            float a=Alpha;for(int i=0;i<6;i++){
                float x=95+i*218, slant=i<3?-74:74;
                Soft(_back,S(x+slant*.45f,335),S(180,430),Blue,a*.43f,slant<0?-.15f:.15f);
                Soft(_back,S(x,410),S(100,235),White,a*.18f);
            }
            Soft(_back,S(640,475),S(1230,132),Blue,a*.34f);Soft(_back,S(640,492),S(1050,42),White,a*.4f);return;
        }
        if(Kind is CueKind.TicketCast or CueKind.TicketReturn)DrawRoad();
        if(Kind==CueKind.Act1Cast){
            var center=_body+new Vector2(130,-35-100*Ease(P(_age,0,.55f)))*_unit;
            for(int i=-4;i<=4;i++)Soft(_back,center+new Vector2(i*16,Mathf.Sin(i*.5f)*7)*_unit,new Vector2(80,165)*_unit,Blue,Alpha*.22f,i*.025f);
        }
    }
    private void DrawRoad()
    {
        float a=Kind==CueKind.TicketReturn ? (_drawAt<0?Alpha*.72f:Win(_age-_drawAt,0,.26f,.015f,.18f)*.7f) : Alpha;
        float opened=Kind==CueKind.TicketCast?Ease(P(_age,.02f,.54f)):1;
        for(int i=0;i<24;i++){
            float q=i/23f;if(q>opened)break;var p=Near.Lerp(Far,q);float width=80-61*q;
            Soft(_back,p,new Vector2(width,13-8*q)*_unit,White,a*.53f);
            if(i%4==0){var c=White;c.A=a*.48f;_back.DrawLine(p+Vector2.Left*width*.45f*_unit,p+Vector2.Right*width*.45f*_unit,c,1.4f*_unit,true);}
        }
    }
    private void DrawForeground()
    {
        if(Kind==CueKind.Act2Cast){
            float a=Alpha,u=Ease(P(_age,.02f,.48f));var from=S(640,82);
            for(int i=0;i<7;i++){
                var end=from.Lerp(S(640+(i-3)/3f*578,526),u);var mid=from.Lerp(end,.5f);float angle=(end-from).Angle()-Mathf.Pi/2;
                Soft(this,mid,new Vector2(_screen.X/1280*70,from.DistanceTo(end)*1.15f),Blue,a*.72f,angle);
                var c=White;c.A=a*.9f;DrawLine(from,end,c,(i==3?5:3.5f)*_screen.X/1280,true);
            }
            Soft(this,from,S(380,190),Blue,a*.72f);Soft(this,from,S(155,95),White,a*.95f);Soft(this,S(640,512),S(1230,80),Blue,a*.4f);return;
        }
        if(Kind==CueKind.Duet){
            float u=Ease(P(_age,0,.35f)),a=_energyAt<0?1:1-P(_age-_energyAt,.06f,.32f);
            if(_sourceCaptured){Star(this,Arc(_sourcePoint+Vector2.Right*27*_unit,DuetCenter,u,28*_unit),12,Gold,a);Star(this,Arc(_sourcePoint-Vector2.Right*20*_unit,DuetCenter+new Vector2(-20,18)*_unit,u,42*_unit),5,White,a);}
            return;
        }
        if(Kind==CueKind.TicketCast && _confirmedAt>=0){float a=Win(_age-_confirmedAt,0,.4f,.02f,.2f);Star(this,Far+Vector2.Up*16*_unit,10,Gold,a);Star(this,Far+new Vector2(13,-4)*_unit,4,White,a*.7f);}
        if(Kind==CueKind.Act1Cast && _confirmedAt>=0)Star(this,_body+new Vector2(130,-135)*_unit,11,Pink,Win(_age-_confirmedAt,0,.4f,.02f,.18f));
    }
    private void DrawCards()
    {
        if(_confirmedAt>=0 && Kind is CueKind.TicketCast or CueKind.Act1Cast or CueKind.Act2Cast){
            float t=_age-_confirmedAt,u=Ease(P(t,.04f,.35f)),a=Win(t,0,.55f,.025f,.22f);
            Vector2 from=Kind==CueKind.TicketCast?Near:Kind==CueKind.Act1Cast?_body+new Vector2(130,-135)*_unit:S(640,88);
            Trail(_hud,from,PowerPoint(),u,Kind==CueKind.Act1Cast?Pink:Blue,a*.5f,30);return;
        }
        if(Kind==CueKind.TicketReturn && _drawAt>=0){
            float t=_age-_drawAt,u=Ease(P(t,0,.18f));Trail(_hud,Far,Near,u,Gold,Win(t,0,.28f,.015f,.09f)*.7f,0);
        }
        foreach(var card in _cards){
            if(CardPoint(card) is not {} target)continue;float t=_age-_cardTimes[card];
            if(Kind==CueKind.Act1Draw){
                float a=Win(t,0,.5f,.03f,.19f),u=Ease(P(t,.03f,.24f));
                var node=NCard.FindOnTable(card);if(node==null)continue;
                var portrait=node.GetNode<TextureRect>("%Portrait");var transform=portrait.GetGlobalTransformWithCanvas();
                var edge=transform*new Vector2(portrait.Size.X*u,0);
                Soft(_hud,edge,new Vector2(60,15)*_unit,Blue,a*.9f,transform.X.Angle());Star(_hud,edge,3.5f,Pink,a);
            }else{
                float delay=Kind==CueKind.TicketReturn?.16f:0,u=Ease(P(t,delay,delay+.32f)),a=Win(t,delay,delay+.62f,.02f,.2f);
                var from=Kind==CueKind.Duet?_promisePoint:Near;Trail(_hud,from,target,u,Gold,a*.65f,48);if(u<1)Star(_hud,Arc(from,target,u,48*_unit),5,White,a);
            }
        }
        if(Kind==CueKind.Duet && _energyAt>=0 && LocalContext.IsMe(_owner)){
            var counter=_room.Ui.EnergyCounterContainer.GetChildren().OfType<NEnergyCounter>().FirstOrDefault();if(counter==null)return;
            var target=counter.GetGlobalTransformWithCanvas()*(counter.Size*.5f);float t=_age-_energyAt,u=Ease(P(t,0,.36f)),a=Win(t,0,.65f,.025f,.25f);
            Trail(_hud,DuetCenter,target,u,Gold,a*.6f,40);if(u<1)Star(_hud,Arc(DuetCenter,target,u,40*_unit),5,Gold,a);Glow(_hud,target,60,Gold,Win(t,.3f,.7f,.02f,.25f)*.5f);
        }
    }
}
