shader_type canvas_item;
render_mode unshaded, blend_add;

uniform float effect_age = 0.0;
uniform vec2 screen_size = vec2(1920.0, 1080.0);
uniform vec4 confirm_rect;
uniform vec4 peek_rect;
uniform sampler2D border_texture : source_color, filter_linear, repeat_disable;

float clear_control(vec2 pixel, vec4 bounds) {
    vec2 center = (bounds.xy + bounds.zw) * 0.5;
    vec2 radius = max((bounds.zw - bounds.xy) * 0.5, vec2(screen_size.y * 0.035));
    return smoothstep(0.65, 1.45, length((pixel - center) / radius));
}

float flow_noise(vec2 p) {
    vec2 cell = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    vec4 h = fract(sin(vec4(dot(cell, vec2(127.1, 311.7)),
        dot(cell + vec2(1.0, 0.0), vec2(127.1, 311.7)),
        dot(cell + vec2(0.0, 1.0), vec2(127.1, 311.7)),
        dot(cell + vec2(1.0), vec2(127.1, 311.7)))) * 43758.5453);
    return mix(mix(h.x, h.y, f.x), mix(h.z, h.w, f.x), f.y);
}

void fragment() {
    vec2 p = UV;
    float t = effect_age;
    float aspect = screen_size.x / screen_size.y;
    float depth = min(min(p.x, 1.0 - p.x) * aspect, min(p.y, 1.0 - p.y));
    vec2 q = (p - 0.5) * vec2(aspect, 1.0);
    float angle = atan(q.y, q.x);

    // Advect detail clockwise around the accepted composition. The broad exposure
    // anchors stay in their original corners; only their contours and light move.
    vec4 sides = exp(-vec4(p.y, 1.0 - p.x, 1.0 - p.y, p.x) * 11.0);
    vec2 tangent = vec2(sides.x - sides.z, sides.y - sides.w);
    tangent /= max(length(tangent), 0.001);
    vec2 normal = vec2(-tangent.y, tangent.x);
    vec2 field = vec2(cos(angle - t * 0.28), sin(angle - t * 0.28)) * 3.0 + vec2(depth * 9.0, -depth * 12.0);
    float smoke = flow_noise(field) * 0.65
        + flow_noise(field * 2.03 + vec2(-t * 0.35, t * 0.18)) * 0.35;
    float wave = sin(angle * 6.0 - t * 2.5 + depth * 12.0);
    float ripple = sin(angle * 13.0 - t * 5.2 - depth * 21.0);
    vec2 drift = tangent * (wave * 0.022 + ripple * 0.008)
        + normal * ((smoke - 0.5) * 0.033);
    vec2 sample_uv = clamp(p + drift / vec2(aspect, 1.0), vec2(0.0), vec2(1.0));
    vec4 border = texture(border_texture, sample_uv);
    float center_clear = 1.0 - smoothstep(0.18, 0.28, depth);
    float warm = smoothstep(0.10, 0.65, border.g);

    // Traveling crests reveal the flow even with the hand completely untouched.
    // Different velocities keep this from reading as a whole-image brightness pulse.
    float crest = pow(0.5 + 0.5 * cos(angle * 8.0 - t * 3.2 + depth * 25.0 + smoke * 2.0), 3.0);
    float light_flow = mix(0.64 + smoke * 0.64, 0.72 + crest * 0.78, warm);
    vec3 light = border.rgb * border.a * mix(0.58, 0.94, warm) * light_flow;
    float side_depth = min(p.x, 1.0 - p.x) * aspect;
    float red_shoulder = 0.055 * exp(-pow((side_depth - 0.14) / 0.09, 2.0));
    light += vec3(1.0, 0.022, 0.07) * red_shoulder * (0.55 + smoke * 0.85 + crest * 0.20);
    float controls = clear_control(p * screen_size, confirm_rect) * clear_control(p * screen_size, peek_rect);
    COLOR = vec4(light, center_clear * controls) * COLOR;
}
