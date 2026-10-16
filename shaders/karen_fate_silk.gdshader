shader_type canvas_item;
render_mode unshaded, blend_add;
uniform vec4 tint : source_color = vec4(1.0,0.25,0.3,1.0);
uniform float age = 0.0;
uniform float seed = 1.0;
uniform float strength = 1.0;
uniform float erosion = 0.0;
float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float noise(vec2 p){vec2 i=floor(p),f=fract(p);f=f*f*(3.0-2.0*f);return mix(mix(hash(i),hash(i+vec2(1,0)),f.x),mix(hash(i+vec2(0,1)),hash(i+vec2(1,1)),f.x),f.y);}
float cloud(vec2 p){return .57*noise(p)+.28*noise(p*2.13+9.1)+.15*noise(p*4.27-3.2);}
void fragment(){
    float u=UV.x,v=UV.y;
    float n=cloud(vec2(u*9.0-age*1.8+seed,v*5.5+seed));
    float fine=cloud(vec2(u*28.0-age*3.0,v*13.0+seed));
    float warp=v-.5+sin(u*17.0-age*3.3+seed)*.075+(n-.5)*.12;
    float feather=1.0-smoothstep(.29+n*.10,.50,abs(warp));
    float fold=.5+.5*sin(warp*30.0+u*14.0-age*4.2+n*4.0);
    float fiber=pow(max(0.0,.5+.5*sin(warp*103.0+u*24.0-age*4.0+n*8.0)),12.0);
    float pearl=pow(fold,8.0)*(.3+.7*fine);
    float body=(.16+.37*n+.20*fold+.24*pearl+.10*fiber)*feather;
    body*=1.0-.42*exp(-pow((u-.60)/.08,2.0));
    float tips=smoothstep(0.0,.10,u)*(1.0-smoothstep(.86,1.0,u));
    float torn=smoothstep(erosion-.10,erosion+.20,n);
    vec3 color=mix(tint.rgb,vec3(1.0,.89,.74),clamp(pearl*.16+fiber*.10,0.0,.3));
    color+=vec3(.10,.08,.07)*fiber;
    COLOR=vec4(color,body*tips*torn*strength);
}
