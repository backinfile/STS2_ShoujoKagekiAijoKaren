shader_type particles;
render_mode disable_force, disable_velocity;
uniform vec2 source_position = vec2(885.0,338.0);
uniform vec2 source_axis_x = vec2(40.0,0.0);
uniform vec2 source_axis_y = vec2(0.0,58.0);
uniform vec2 target_position = vec2(292.0,363.0);
uniform float trail_delay = 0.0;
uniform float trail_alpha = 1.0;
uniform bool bright_star = false;
uniform float flow_seed = 3.0;
uniform float spread_scale = 1.0;
float rand(float seed) { return fract(sin(seed*127.1+311.7)*43758.5453); }
void start() {
 float seed=float(NUMBER)+flow_seed;
 float gather_start=0.40+rand(seed+12.0)*0.45;
 // Most particles arrive quickly; a smaller, independently timed tail follows.
 float gather_duration=0.25+pow(rand(seed+8.0),3.0);
 CUSTOM=vec4(0.0,gather_start+gather_duration,rand(seed+31.0),0.12+rand(seed+7.0)*0.15);
 COLOR=vec4(1.0);
}
void process() {
 CUSTOM.x+=DELTA;
 float age=CUSTOM.x-trail_delay-rand(float(NUMBER)+flow_seed+40.0)*0.10;
 float seed=float(NUMBER)+flow_seed;
 vec2 origin=source_position+source_axis_x*(rand(seed+1.0)*2.0-1.0)+source_axis_y*(rand(seed+2.0)*2.0-1.0);
 vec2 spread=vec2(28.0+rand(seed+6.0)*1224.0,92.0+rand(seed+9.0)*552.0);
 spread=mix(source_position,spread,spread_scale);
 // Join the burst to the curl at a shared position AND velocity. There is no hold phase.
 float gather_start=0.40+rand(seed+12.0)*0.45;
 float gather_duration=CUSTOM.y-gather_start;
 float s=clamp((age-gather_start)/gather_duration,0.0,1.0);
 vec2 landing=target_position+vec2(rand(seed+17.0)-0.5,rand(seed+18.0)-0.5)*vec2(18.0,26.0);
 vec2 offset=spread-landing;
 float contraction=0.90+rand(seed+23.0)*0.65;
 float curl_angle=(rand(seed+22.0)*2.0-1.0)*0.65;
 // Each particle gets one broad bend, not a repeating wobble or a shared spiral.
 vec2 bend=vec2(rand(seed+24.0)-0.5,rand(seed+25.0)-0.5)*vec2(110.0,150.0);
 vec2 p;
 if(age<gather_start) {
  float u=clamp(age/gather_start,0.0,1.0);
  float kick_angle=rand(seed+42.0)*6.283185;
  vec2 kick=vec2(cos(kick_angle),sin(kick_angle))*(70.0+rand(seed+43.0)*220.0);
  vec2 start_tangent=(spread-origin)*(1.8+rand(seed+41.0)*1.2)+kick;
  // Exact derivative of the curl at s=0, converted to the burst's normalized time.
  vec2 curl_velocity=(-offset*contraction+vec2(-offset.y,offset.x)*curl_angle+bend*3.141593)/gather_duration;
  vec2 end_tangent=curl_velocity*gather_start;
  float u2=u*u;
  float u3=u2*u;
  p=(2.0*u3-3.0*u2+1.0)*origin+(u3-2.0*u2+u)*start_tangent
    +(-2.0*u3+3.0*u2)*spread+(u3-u2)*end_tangent;
 } else {
  float angle=atan(offset.y,offset.x)+s*curl_angle;
  float radius=length(offset)*pow(1.0-s,contraction);
  p=landing+vec2(cos(angle),sin(angle))*radius+bend*sin(s*3.141593)*(1.0-s);
 }
 float scale=CUSTOM.w*(bright_star?1.65:1.0)*(1.0-0.3*s);
 TRANSFORM=mat4(vec4(scale,0.0,0.0,0.0),vec4(0.0,scale,0.0,0.0),vec4(0.0,0.0,1.0,0.0),vec4(p,0.0,1.0));
 // Furnace portrait: orange embers, yellow flame, and a few warm cores.
 float heat=rand(seed+11.0);
 vec3 gold=mix(vec3(1.0,0.24,0.025),vec3(1.0,0.82,0.08),heat);
 if(heat>0.94) gold=mix(gold,vec3(1.0,0.94,0.42),(heat-0.94)/0.06);
 float alpha=smoothstep(0.0,0.035,age)*(1.0-smoothstep(0.93,1.0,s));
 COLOR=vec4(gold*1.1,alpha*trail_alpha*(0.65+rand(seed+21.0)*0.35));
 if(age<0.0) COLOR.a=0.0;
 if(age>CUSTOM.y) ACTIVE=false;
}
