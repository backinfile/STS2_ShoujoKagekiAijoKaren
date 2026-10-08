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
float rand(float seed) { return fract(sin(seed*127.1+311.7)*43758.5453); }
void start() {
 float seed=float(NUMBER)+flow_seed;
 CUSTOM=vec4(0.0,2.25+rand(seed+8.0)*0.72,rand(seed+31.0),0.07+rand(seed+7.0)*0.10);
 COLOR=vec4(1.0);
}
void process() {
 CUSTOM.x+=DELTA;
 float age=CUSTOM.x-trail_delay-rand(float(NUMBER)+flow_seed+40.0)*0.10;
 float seed=float(NUMBER)+flow_seed;
 vec2 origin=source_position+source_axis_x*(rand(seed+1.0)*2.0-1.0)+source_axis_y*(rand(seed+2.0)*2.0-1.0);
 vec2 spread=vec2(28.0+rand(seed+6.0)*1224.0,92.0+rand(seed+9.0)*552.0);
 float expansion=clamp(age/(0.48+rand(seed+12.0)*0.26),0.0,1.0);
 float burst=1.0-pow(1.0-expansion,1.8+rand(seed+41.0)*2.6);
 vec2 p=mix(origin,spread,burst);
 float kick_angle=rand(seed+42.0)*6.283185;
 vec2 kick=vec2(cos(kick_angle),sin(kick_angle))*(70.0+rand(seed+43.0)*220.0);
 p+=kick*sin(expansion*3.141593)*pow(1.0-expansion,1.4);
 float gather_start=0.76+rand(seed+14.0)*0.34;
 float s=clamp((age-gather_start)/(CUSTOM.y-gather_start),0.0,1.0);
 if(age>gather_start) {
  vec2 pivot=vec2(640.0,368.0)+vec2(rand(seed+15.0)-0.5,rand(seed+16.0)-0.5)*vec2(240.0,180.0);
  vec2 landing=target_position+vec2(rand(seed+17.0)-0.5,rand(seed+18.0)-0.5)*vec2(18.0,26.0);
  vec2 center=mix(pivot,landing,smoothstep(0.0,0.70+rand(seed+19.0)*0.25,s));
  vec2 offset=spread-pivot;
  float angle=atan(offset.y,offset.x)+smoothstep(0.0,1.0,s)*(0.38+rand(seed+22.0)*0.44);
  float radius=length(offset)*pow(1.0-s,1.2+rand(seed+23.0)*0.9);
  // Seeded variations remain continuous in time and agree across trailing layers.
  float fit=1.0;
  p=center+vec2(cos(angle),sin(angle))*radius*fit;
  float envelope=sin(s*3.141593)*pow(1.0-s,0.5);
  vec2 drift=vec2(sin(s*6.283185+seed),sin(s*8.0+seed*1.7));
  p+=drift*(18.0+rand(seed+24.0)*42.0)*envelope;
 }
 float scale=CUSTOM.w*(bright_star?1.65:1.0)*(1.0-0.3*s);
 TRANSFORM=mat4(vec4(scale,0.0,0.0,0.0),vec4(0.0,scale,0.0,0.0),vec4(0.0,0.0,1.0,0.0),vec4(p,0.0,1.0));
 vec3 gold=mix(vec3(1.0,0.66,0.27),vec3(1.0,0.93,0.69),rand(seed+11.0));
 float alpha=smoothstep(0.0,0.035,age)*(1.0-smoothstep(0.93,1.0,s));
 COLOR=vec4(gold*1.8,alpha*trail_alpha*(0.65+rand(seed+21.0)*0.35));
 if(age<0.0) COLOR.a=0.0;
 if(age>CUSTOM.y) ACTIVE=false;
}