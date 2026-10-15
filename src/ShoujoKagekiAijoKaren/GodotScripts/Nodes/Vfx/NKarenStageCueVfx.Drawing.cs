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
    private Vector2 Near => _foot+new Vector2(85,6)*_unit;
    private Vector2 Far => _foot+new Vector2(165,-145)*_unit;
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
        if(Kind==CueKind.Act2Hover)
            _shade.DrawRect(new Rect2(S(0,54),S(1280,482)),new Color(.035f,.07f,.15f,Alpha*.18f));
        if(Kind!=CueKind.Act2Cast)return;
        float a=Alpha;
        _shade.DrawRect(new Rect2(S(0,54),S(1280,666)),new Color(.065f,.068f,.075f,a*.52f));
        // The backdrop's stage plane follows the character's foot line, not the viewport edge.
        for(int i=0;i<6;i++){
            float x=44+i*198;
            _shade.DrawColoredPolygon([S(640,82),CastGroundPoint(x+36,false),CastGroundPoint(x+142,false)],new Color(.045f,.048f,.055f,a*.24f));
        }
        _shade.DrawRect(new Rect2(S(0,470),S(1280,250)),new Color(.08f,.082f,.09f,a*.14f));
    }
    private void DrawForegroundShade()
    {
        if(Kind!=CueKind.Act2Cast)return;
        float a=Alpha;
        _frontShade.DrawRect(new Rect2(S(0,54),S(1280,666)),new Color(.075f,.078f,.085f,a*.16f));
        _frontShade.DrawColoredPolygon([S(0,54),S(480,54),CastGroundPoint(170,true),CastGroundPoint(0,true)],new Color(.055f,.058f,.065f,a*.12f));
        _frontShade.DrawColoredPolygon([S(800,54),S(1280,54),CastGroundPoint(1280,true),CastGroundPoint(1110,true)],new Color(.055f,.058f,.065f,a*.12f));
    }
    private Vector2 CastGroundPoint(float designX,bool front) => new(S(designX,0).X,
        _foot.Y + (front ? 7 : -14) * _unit - Mathf.Sin(designX / 1280 * Mathf.Pi) * 5 * _unit);
    private void DrawCastBeams(Node2D layer,bool front)
    {
        float a=Alpha,u=Ease(P(_age,.02f,.48f));var from=S(640,82);
        int count=front?5:7;
        for(int i=0;i<count;i++){
            var floor=CastGroundPoint(front?135+i*252:44+i*198,front);
            var end=from.Lerp(floor,u);var delta=end-from;if(delta.Length()<1)continue;
            float width=(front?26:54)*_screen.X/1280;
            // Nested feathered strips have a flat end on the perspective floor.
            // A radial texture along the shaft would leak past that plane.
            for(int band=0;band<7;band++){
                float w=width*(1-band*.13f);
                layer.DrawColoredPolygon([from-Vector2.Right*2,from+Vector2.Right*2,end+Vector2.Right*w,end-Vector2.Right*w],
                    new Color(Blue.R,Blue.G,Blue.B,a*(front?.022f:.035f)));
            }
            var c=White;c.A=a*(front?.75f:.33f);layer.DrawLine(from,end,c,(front?2.5f:3.5f)*_screen.X/1280,true);
            float landed=P(_age,.40f,.52f);
            Soft(layer,floor,new Vector2(width*3.6f,22*_screen.Y/720),Blue,a*landed*(front?.45f:.56f));
            Soft(layer,floor,new Vector2(width*2,7*_screen.Y/720),White,a*landed*.38f);
        }
    }
    private void DrawBackground()
    {
        if(Kind==CueKind.Act2Cast){DrawCastBeams(_back,false);return;}
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
        for(int i=0;i<28;i++){
            float q=i/28f;if(q>=opened)break;float next=Mathf.Min((i+1)/28f,opened);
            var p=Near.Lerp(Far,q);var end=Near.Lerp(Far,next);
            float width=Mathf.Lerp(104,18,q)*_unit, endWidth=Mathf.Lerp(104,18,next)*_unit;
            // Continuous tapered ground projection, with soft nested edges rather than floating bars.
            for(int band=0;band<6;band++){
                float f=1-band*.14f;
                _back.DrawColoredPolygon([p+Vector2.Left*width*.5f*f,p+Vector2.Right*width*.5f*f,end+Vector2.Right*endWidth*.5f*f,end+Vector2.Left*endWidth*.5f*f],
                    new Color(White.R,White.G,White.B,a*(.022f+band*.007f)*(1-q*.3f)));
            }
            if(i%5==0){var c=White;c.A=a*.27f;_back.DrawLine(p+Vector2.Left*width*.42f,p+Vector2.Right*width*.42f,c,Mathf.Lerp(1.7f,.7f,q)*_unit,true);}
        }
        var head=Near.Lerp(Far,opened);Soft(_back,head,new Vector2(Mathf.Lerp(130,30,opened),18)*_unit,White,a*.48f);
    }
    private void DrawForeground()
    {
        if(Kind==CueKind.Act2Cast){
            DrawCastBeams(this,true);var from=S(640,82);
            Soft(this,from,S(380,190),Blue,Alpha*.65f);Soft(this,from,S(155,95),White,Alpha*.92f);return;
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
