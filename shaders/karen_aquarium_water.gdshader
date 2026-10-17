shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float clock = 0.0;
uniform float strength = 0.0;
vec2 hash(vec2 p) { return fract(sin(vec2(dot(p, vec2(127.1,311.7)), dot(p, vec2(269.5,183.3)))) * 43758.5453); }
void fragment() {
    vec2 p = (UV - .5) * vec2(8.0, 4.5);
    p += vec2(sin(p.y * 3.7 + clock * .57), cos(p.x * 3.3 - clock * .43)) * .28;
    vec2 cell = floor(p), f = fract(p);
    float near1 = 10.0, near2 = 10.0;
    for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) {
        vec2 g = vec2(float(x),float(y));
        vec2 h = hash(cell + g);
        vec2 pos = g + .5 + .32 * sin(clock * .42 + 6.2831853 * h) - f;
        float d = dot(pos,pos);
        if (d < near1) { near2 = near1; near1 = d; } else { near2 = min(near2,d); }
    }
    float line = exp(-(sqrt(near2) - sqrt(near1)) * 19.0);
    float mask = pow(max(0.0, 1.0 - length((UV - .5) * 2.0)), .70);
    vec3 blue = mix(vec3(.06,.42,.69), vec3(.35,.83,1.0), line);
    COLOR = vec4(blue * (.07 + .93 * line), strength * mask);
}
