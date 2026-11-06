shader_type canvas_item;
render_mode unshaded, blend_add;

// Original procedural effect material. Its interior flows and erodes every frame;
// there are no food, paper, hands, seals, lamps or reference-Mod textures.
uniform float age=0.0;
uniform float result_age=-1.0;
uniform float strength=1.0;
uniform float seed=1.0;
uniform int mode=0;
uniform bool front_layer=false;
uniform bool trigger=false;
uniform bool failed=false;

float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float noise(vec2 p){vec2 i=floor(p),f=fract(p);f=f*f*(3.0-2.0*f);return mix(mix(hash(i),hash(i+vec2(1,0)),f.x),mix(hash(i+vec2(0,1)),hash(i+vec2(1,1)),f.x),f.y);}
float cloud(vec2 p){return .58*noise(p)+.28*noise(p*2.13+7.2)+.14*noise(p*4.37-3.1);}
float window(float t,float a,float b){return smoothstep(a,a+.12,t)*(1.0-smoothstep(b-.3,b,t));}
float gauss(float d,float width){return exp(-d*d/max(.0001,width*width));}
float flare(vec2 p){float r=length(p);return max(0.0,1.0-r)*gauss(p.x*p.y,.007)+.35*exp(-r*9.0);}

void fragment(){
    vec2 p=(UV-.5)*2.0;p.y=-p.y;
    float t=age,r=max(0.0,result_age);
    bool hit=result_age>=0.0;
    float lead=trigger?.28:(mode==6?.48:.7);
    float charge=smoothstep(0.0,lead,t);
    float n=cloud(p*4.2+vec2(seed,t*1.8));
    float detail=noise(p*22.0+vec2(-t*2.0,seed+t*3.0));
    float d=0.0;
    float white=0.0;
    float mix_color=.0;
    vec3 warm=vec3(1.0,.53,.12),cool=vec3(1.0,.91,.60);
    if(mode==0){
        // A ragged, particulate funnel tightens around the real potion origin.
        float squeeze=hit?mix(2.6,1.7,smoothstep(0.0,.5,r)):mix(.9,2.6,charge);
        vec2 q=p*squeeze;
        float radius=length(q),a=atan(q.y,q.x);
        float curl=sin(a*3.0+radius*6.0+t*5.0+(n-.5)*4.0);
        d=exp(-radius*2.8)*smoothstep(.25,.7,n)*.8;
        d+=gauss(curl,.23)*exp(-radius*2.3)*smoothstep(.2,.7,detail)*.8;
        white=exp(-radius*12.0)*charge*.8;
        if(hit){d*=1.0-smoothstep(.15,.65,r);white*=1.0-smoothstep(0.0,.4,r);}
        if(failed){d*=1.0-smoothstep(.9,1.4,t);white=0.0;}
    }else if(mode==1){
        // Fast, broad updraft, then a lateral release: temporary strength.
        float height=(p.y+.8)/1.65;
        float sweep=hit?r*.6:0.0;
        float x=p.x+sin(p.y*5.0-t*5.0)*.12;
        x=sign(x)*max(0.0,abs(x)-sweep*.22);
        float width=.12+.28*height;
        float plume=gauss(x,width)*(1.0-smoothstep(.25,1.0,height));
        float billow=cloud(vec2(x*9.0,height*7.0-t*4.0)+seed);
        d=plume*smoothstep(.25,.67,billow)*2.0;
        d*=smoothstep(-.82,-.65,p.y)*(1.0-smoothstep(.48,.92,p.y));
        d*=hit?1.0-smoothstep(.05,.85,r):charge;
        white=hit?d*window(r,-.07,.38)*.7:0.0;
        warm=vec3(1.0,.48,.14);cool=vec3(1.0,.85,.54);
    }else if(mode==2){
        // Three staggered rising folds, weighted and slower than the updraft.
        for(int i=0;i<3;i++){
            float side=float(i)-1.0;
            float local_t=hit?r-float(i==1?2:i)*.075:t-lead;
            float wave=hit?window(local_t,-.08,1.0):charge*.2;
            float height=(p.y+.8)/1.7;
            float x=p.x-side*(.32-.12*height)+sin(height*8.0-t*2.0+side)*.055;
            float fold=gauss(x,.065+.085*(1.0-height));
            float profile=smoothstep(-.82,-.62,p.y)*(1.0-smoothstep(.0,.8,p.y));
            float grain=smoothstep(.18,.74,cloud(vec2(x*12.0,height*7.0-t*1.8)+seed+side));
            d+=fold*profile*(.15+grain*.95)*wave*1.1;
            white+=fold*profile*wave*.15;
        }
        d+=gauss(p.y+.65,.07)*gauss(p.x,.58)*n*.3*charge;
        warm=vec3(1.0,.61,.19);cool=vec3(1.0,.96,.72);
    }else if(mode==3){
        // Pearl folds around a pale-blue core: the card and buff's cool three-part motif.
        // The three volumes stay separate; no ticket silhouette or closed triangle outline.
        float gather=smoothstep(0.0,trigger?.28:.62,t);
        float release=hit?smoothstep(.04,.65,r):0.0;
        float body=0.0;
        for(int i=0;i<3;i++){
            float angle=1.5708+float(i)*2.0944;
            vec2 axis=vec2(cos(angle),sin(angle));
            vec2 across=vec2(-axis.y,axis.x);
            vec2 q=p-axis*release*.20;
            q+=vec2(n-.5,detail-.5)*.035;
            float along=dot(q,axis),side=dot(q,across);
            float center=mix(.48,.29,gather);
            float fold=gauss(side,.085+.04*n)*gauss(along-center,.20+.05*gather);
            float frosted=.38+.62*smoothstep(.24,.72,cloud(q*9.0+vec2(seed,t*.8)));
            body+=fold*frosted*smoothstep(.04,.17,along);
        }
        float core=exp(-length(p)*13.0)*gather;
        float fade=hit?1.0-smoothstep(.22,.82,r):1.0;
        d=(body*.72+core*.30)*gather*fade;
        white=body*.24*gather*fade;
        mix_color=clamp(length(p)*2.8,0.0,1.0);
        warm=vec3(.62,.77,1.0);cool=vec3(1.0,.98,.92);
    }else if(mode==4){
        // Two noisy lobes travel inward without a solid line or outlined symbol.
        float converge=hit?1.0:charge;
        float y=p.y+.22;
        float channel=clamp((y+.65)/1.1,0.0,1.0);
        float x0=.67*cos(channel*1.5708)+.1*(1.0-converge);
        float warped_x=p.x+(n-.5)*.2;
        float left=gauss(warped_x+x0,.12+.07*n),right=gauss(warped_x-x0,.12+.07*(1.0-n));
        float body=(left+right)*smoothstep(-.72,-.52,y)*(1.0-smoothstep(.18,.62,y));
        float flow=cloud(vec2(p.x*11.0,y*9.0-t*3.3)+seed);
        d=body*smoothstep(.2,.7,flow)*charge*1.25;
        if(hit)d*=1.0-smoothstep(.18,.85,r);
        white=hit?flare((p-vec2(0,.19))*2.1)*window(r,-.07,.7)*.55:0.0;
        mix_color=smoothstep(-.12,.12,p.x);
        warm=vec3(1.0,.30,.18);cool=vec3(.18,.48,1.0);
    }else if(mode==5){
        // Paired stage spotlights converge on each real player's feet, on the same cue.
        float height=clamp((p.y+.88)/1.72,0.0,1.0);
        float width=.14+.11*(1.0-height);
        float fan=gauss(p.x-height*.56,width)+gauss(p.x+height*.56,width);
        float ends=smoothstep(-.94,-.78,p.y)*(1.0-smoothstep(.35,.91,p.y));
        d=fan*ends*(.85+.10*n)*smoothstep(0.0,.26,t);
        d*=mix(.8,.52,height);
        d*=hit?1.0-smoothstep(.45,1.15,r):1.0;
        white=d*.26;warm=vec3(1.0,.70,.39);cool=vec3(1.0,.96,.78);
    }else if(mode==6){
        // Compressed white core becomes a cut-glass flare, then actual energy departs.
        float scale=hit?mix(1.2,.72,smoothstep(0.0,.22,r)):mix(2.5,1.2,charge);
        vec2 q=p*scale;
        float a=.45;
        vec2 rotated=mat2(vec2(cos(a),sin(a)),vec2(-sin(a),cos(a)))*q;
        float star=flare(q)+flare(rotated*vec2(1.5,1.0))*.25;
        float vapor=exp(-length(q)*5.5)*smoothstep(.3,.72,n);
        d=(star*.7+vapor*.28)*charge;
        white=star*.6*charge;
        if(hit){d*=1.0-smoothstep(.1,.7,r);white*=1.0-smoothstep(.0,.4,r);}
        warm=vec3(.22,.54,1.0);cool=vec3(.6,.88,1.0);
    }else{
        // Transformation accents a native card preview; no strength-like body burst.
        float radius=length(p);
        float collapse=hit?mix(.32,.95,smoothstep(0.0,.6,r)):mix(.8,.32,charge);
        float fragments=smoothstep(.45,.75,detail)*smoothstep(.2,.7,n);
        d=gauss(radius-collapse,.14)*fragments*.6;
        if(hit)d*=1.0-smoothstep(.2,.9,r);
        white=0.0;
    }
    float edges=(1.0-smoothstep(.80,1.0,abs(p.x)))*(1.0-smoothstep(.80,1.0,abs(p.y)));
    d=max(0.0,d)*edges;white=max(0.0,white)*edges;
    float core=smoothstep(.2,.85,d);
    vec3 color=mix(warm,cool,clamp(mix_color+(mode==4?0.0:core*.55),0.0,1.0));
    float luminance=front_layer?d*core*.26+white*.32:d*.82+white*.32;
    color=mix(color,vec3(1.0,.98,.9),clamp(white,0.0,.85));
    COLOR=vec4(color,clamp(luminance*strength,0.0,.9));
}
