using Godot;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

public partial class NKarenBenefitVfx
{
    private float FoodAlpha => this is NKarenBananaVfx {PotionSucceeded:false}
        ? 1-P(Age,.9f,1.45f) : StoryAlpha;

    private void DrawFoodBack()
    {
        switch(Design) {
            case BenefitKind.Cake:
                WarmSmoke(Bottle+Vector2.Down*30*Unit,Age,85,60,5,.09f);break;
            case BenefitKind.Snack:
                WarmSmoke(Floor,Age,175,200,10,.14f);break;
            case BenefitKind.Dinner:
                WarmSmoke(Floor,Age*.82f,180,95,8,.09f);break;
            case BenefitKind.SnackTransform:
                WarmSmoke(StoryPoint+Vector2.Down*20*Unit,Age,85,45,4,.07f);break;
        }
    }
    private void DrawFoodFlow()
    {
        if(Design==BenefitKind.Cake) {DrawCakeAssembly();return;}
        if(Design==BenefitKind.SnackTransform) {DrawTransformationDust();return;}
        bool temporary=Design==BenefitKind.Snack;
        float alpha=FoodAlpha;
        int count=temporary?58:44;
        for(int i=0;i<count;i++) {
            float delay=Random(i+41)*(temporary?.3f:.55f);
            float u=(Age-delay)/(temporary?1.15f:1.85f);
            if(u<=0||u>=1)continue;
            float side=Random(i+7)<.5f?-1:1;
            float x=side*(24+Random(i+3)*70);
            float height=temporary?195:135;
            float y=-Ease(u)*height;
            x+=Mathf.Sin(u*5+i)*16;
            if(temporary&&Confirmed)x+=side*Ease(P(ResultAge,0,.65f))*55;
            else if(!temporary)x*=1-.45f*Ease(u);
            var point=Floor+new Vector2(x,y)*Unit;
            float a=Mathf.Sin(u*Mathf.Pi)*alpha;
            float size=(i%7==0?9:3)+Random(i+5)*3;
            Grain(_front,point,size,i%5==0?Cream:Gold,a*(i%7==0?.65f:.85f),i+Age*.4f);
            if(i%11==0)Star(_front,point,5,0,Cream,a*.65f);
        }
        if(!Confirmed)return;
        if(temporary) {
            float u=P(ResultAge,0,.52f),a=Window(ResultAge,0,.62f,.04f,.3f);
            var point=Body+Vector2.Down*(35-70*u)*Unit;
            Sprite(_front,_soft,point,new Vector2(80+90*u,140-95*u)*Unit,Gold,a*.32f);
        }else{
            // One real strength result, three weighted light beats and a shared warm decay.
            for(int j=0;j<3;j++) {
                float t=ResultAge-j*.075f,a=Window(t,0,.58f,.04f,.4f);
                var point=Body+new Vector2((j-1)*42,18-Ease(P(t,0,.4f))*36)*Unit;
                Star(_front,point,10+10*Mathf.Sin(P(t,0,.5f)*Mathf.Pi),0,Cream,a*.7f);
            }
        }
    }
    private void DrawCakeAssembly()
    {
        float alpha=FoodAlpha;
        for(int i=0;i<78;i++) {
            float delay=Random(i+20)*.27f;
            float u=P(Age-delay,0,.78f);
            if(Age<delay||u>=1)continue;
            float angle=Random(i+3)*Mathf.Tau;
            float radius=60+Random(i+4)*85;
            float turn=angle+(1-u)*1.1f;
            var point=Bottle+new Vector2(Mathf.Cos(turn)*radius*(1-Ease(u)),
                Mathf.Sin(turn)*radius*.65f*(1-Ease(u))+22*(1-u))*Unit;
            float size=(i%6==0?9:3)+Random(i+9)*4;
            float a=Mathf.Sin(u*Mathf.Pi)*alpha;
            Grain(_front,point,size,i%4==0?Cream:Gold,a*.8f,angle+u*2);
            if(i%13==0)Star(_front,point,4,0,Cream,a*.65f);
        }
        if(Confirmed) {
            float a=Window(ResultAge,0,.45f,.025f,.32f);
            Star(_front,Bottle,23,0,Cream,a*.7f);
            for(int i=0;i<14;i++) {
                float u=P(ResultAge,0,.62f),angle=Random(i)*Mathf.Tau;
                var point=Bottle+Polar(angle,8+Ease(u)*55,.65f);
                Grain(_front,point,3,Gold,Mathf.Sin(u*Mathf.Pi)*.5f,angle);
            }
        }
    }
    private void DrawTransformationDust()
    {
        float phase=Confirmed?ResultAge:Age;
        float a=Confirmed?Window(ResultAge,0,.9f,.04f,.45f):P(Age,0,.2f)*.45f;
        for(int i=0;i<20;i++) {
            float angle=Random(i+10)*Mathf.Tau+phase*.3f;
            float radius=Confirmed?20+Ease(P(phase,0,.7f))*42:70*(1-Ease(P(Age,0,.6f)))+14;
            var point=StoryPoint+Polar(angle,radius,.6f);
            Grain(_front,point,3+Random(i+3)*4,Gold,a*.65f,angle);
        }
    }
}
