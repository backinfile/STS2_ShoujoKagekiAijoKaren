shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float clock = 0.0;
uniform float phase = 0.0;
uniform float strength = 0.0;
void fragment() {
    vec2 uv = UV;
    float t = clock * 1.35 + phase;
    float pulse = sin(t);
    float x = (uv.x - .5) / (1.0 + .045 * pulse);
    float y = uv.y - .26;
    float ellipse = length(vec2(x / .37, y / (.224 - .01 * pulse)));
    float lip_y = .365 + .014 * cos(x * 55.0 + sin(t));
    float cap = (1.0 - smoothstep(.97, 1.02, ellipse)) * (1.0 - smoothstep(lip_y, lip_y + .012, uv.y));
    float edge = exp(-abs(ellipse - .97) * 63.0) * (1.0 - smoothstep(.377, .39, uv.y));
    float lip = exp(-abs(uv.y - lip_y) * 190.0) * (1.0 - smoothstep(.31, .365, abs(x)));
    float veins = pow(max(0.0, cos(atan(y / .224, x / .37) * 12.0 + ellipse * 2.0)), 12.0) * cap * smoothstep(.12, .85, ellipse);
    float lobes = 0.0;
    for (int i = 0; i < 4; i++) {
        float a = float(i) * 1.5707963 + .785398;
        vec2 c = vec2(cos(a) * .059, .255 + sin(a) * .038);
        float d = length((vec2(x, uv.y) - c) / vec2(.038, .026));
        lobes += exp(-abs(d - 1.0) * 9.0);
    }
    float tentacles = 0.0;
    for (int i = 0; i < 11; i++) {
        float f = float(i);
        float start = (f / 10.0 - .5) * .55;
        float len = .36 + .12 * sin(f * 2.7 + phase);
        float v = clamp((uv.y - .36) / len, 0.0, 1.0);
        float wave = sin(v * 6.5 - t + f * 1.9) * .030 * v + sin(v * 12.0 - t * .7 + f) * .012 * v;
        float dx = abs(x - start * (1.0 - v * .27) - wave);
        float width = mix(.0035, .0014, v);
        float thread = exp(-dx / width) + .12 * exp(-dx / (width * 3.0));
        tentacles += thread * smoothstep(.35, .39, uv.y) * (1.0 - smoothstep(.72, 1.0, v));
    }
    vec3 cyan = vec3(.22, .72, 1.0);
    vec3 lilac = vec3(.68, .56, 1.0);
    vec3 color = mix(cyan, lilac, smoothstep(-.30, .32, x) * .48);
    float light = cap * (.24 + .16 * max(0.0, 1.0 - ellipse)) + edge * .82 + exp(-abs(ellipse - .94) * 13.0) * .10 + lip * .44 + veins * .14 + lobes * .56 + tentacles * .49;
    COLOR = vec4(color * light, strength);
}
