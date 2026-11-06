shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float age = 0.0;
uniform float flow_age = 0.0;
uniform float motion_amount = 1.0;
uniform float strength = 0.0;
uniform float closing = 0.0;
uniform vec2 gather_a = vec2(0.4, 0.3);
uniform vec2 gather_b = vec2(0.6, 0.3);
uniform sampler2D curtain_tex : source_color, filter_linear, repeat_disable;
uniform sampler2D backdrop : hint_screen_texture, filter_linear, repeat_disable;
float band(float d, float width) { return exp(-pow(d / width, 2.0)); }
vec3 silk(vec2 p) {
    float valid = step(0.0, p.x) * step(p.x, 1.0) * step(0.0, p.y) * step(p.y, 1.0);
    // Texture borders become internal edges during gathering: feather them away.
    float border = smoothstep(0.0, 0.12, p.x) * (1.0 - smoothstep(0.88, 1.0, p.x))
                 * smoothstep(0.0, 0.12, p.y) * (1.0 - smoothstep(0.88, 1.0, p.y));
    valid *= mix(1.0, border, smoothstep(0.0, 0.15, closing));
    // Travelling waves bend the painted folds, not just their opacity.
    vec2 uv = p;
    uv.x += (sin(p.y * 8.0 - flow_age * 1.65) * 0.034 + sin(p.y * 18.0 + flow_age * 2.1) * 0.014) * motion_amount;
    uv.y += (sin(p.x * 7.5 + flow_age * 1.35) * 0.044 + sin(p.x * 16.0 - flow_age * 1.8) * 0.018) * motion_amount;
    vec4 cloth = texture(curtain_tex, clamp(uv, vec2(0.001), vec2(0.999)));
    return cloth.rgb * cloth.a * valid * (0.82 + 0.10 * sin(p.y * 13.0 - flow_age * 2.2));
}
void fragment() {
    vec2 p = UV;
    vec3 light;
    if (closing > 0.0) {
        // Inverse mapping pulls each half of the field into its chosen card.
        float scale = max(0.025, pow(1.0 - closing, 1.45));
        vec2 a = (p - mix(vec2(0.25, 0.5), gather_a, closing)) / scale + vec2(0.25, 0.5);
        vec2 b = (p - mix(vec2(0.75, 0.5), gather_b, closing)) / scale + vec2(0.75, 0.5);
        light = (silk(a) * (1.0 - smoothstep(0.42, 0.54, a.x))
               + silk(b) * smoothstep(0.46, 0.58, b.x)) * (1.0 - closing * 0.35);
    } else {
        float opened = smoothstep(0.0, 0.5, age);
        vec2 entrance = vec2((p.x - 0.5) / mix(1.28, 1.0, opened) + 0.5, p.y);
        light = silk(entrance);
    }
    float ambient_fade = 1.0 - smoothstep(0.0, 0.65, closing);
    vec2 pool_delta = (p - vec2(0.48, 0.28)) * vec2(2.2, 1.6);
    // Restore some battlefield color through the native dimmer, like the concept's
    // warm stage lighting. The native grid draws after this layer and stays crisp.
    light += (texture(backdrop, SCREEN_UV).rgb * 0.65
            + vec3(0.22, 0.12, 0.045) * exp(-dot(pool_delta, pool_delta))) * ambient_fade;
    for (int i = 0; i < 7; i++) {
        float f = float(i) - 3.0;
        float x = 0.47 + f * (0.024 + p.y * 0.055) + sin(flow_age * 0.8 + f) * 0.008;
        float shaft = band(p.x - x, 0.008 + p.y * 0.008) * (1.0 - smoothstep(0.04, 0.72, p.y));
        light += vec3(1.0, 0.71, 0.35) * shaft * 0.34 * ambient_fade;
    }
    float aspect = SCREEN_PIXEL_SIZE.y / SCREEN_PIXEL_SIZE.x;
    for (int i = 0; i < 32; i++) {
        float f = float(i) + 1.0;
        vec2 star = vec2(fract(sin(f * 12.9898) * 43758.5453),
                         fract(sin(f * 78.233) * 23421.631 - flow_age * (0.025 + mod(f, 3.0) * 0.01)));
        star.x += sin(flow_age * 1.3 + f * 2.0) * 0.018;
        float twinkle = pow(0.5 + 0.5 * sin(flow_age * 3.2 + f * 2.7), 2.0);
        vec2 d = (p - star) * vec2(aspect, 1.0);
        float halo = exp(-dot(d, d) / 0.000075);
        float cross = band(d.x, 0.0008) * band(d.y, 0.008) + band(d.y, 0.0008) * band(d.x, 0.008);
        light += vec3(1.0, 0.71, 0.33) * (halo * 0.18 + cross * 0.85) * (0.2 + twinkle * 0.8) * ambient_fade;
    }
    COLOR = vec4(light, strength);
}
