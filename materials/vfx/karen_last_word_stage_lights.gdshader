shader_type canvas_item;
render_mode unshaded, blend_mix;

uniform vec2 screen_size = vec2(1920.0, 1080.0);
uniform float effect_age = 0.0;
uniform float strength = 0.0;
uniform vec4 hud_rect;
uniform vec4 end_turn_rect;
uniform vec4 energy_rect;

// The reference uses a white-pink core over a saturated rose-red wash.
// Alpha compositing preserves that hue over the blue battlefield.
const vec3 WHITE_PINK = vec3(1.0, 0.955, 0.963);
const vec3 ROSE = vec3(1.0, 0.255, 0.420);
const vec3 CRIMSON = vec3(0.950, 0.060, 0.220);

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float noise(vec2 p) {
    vec2 cell = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(cell), hash(cell + vec2(1.0, 0.0)), f.x),
        mix(hash(cell + vec2(0.0, 1.0)), hash(cell + vec2(1.0)), f.x), f.y);
}

float protect_control(vec2 pixel, vec4 bounds) {
    if (bounds.z <= bounds.x || bounds.w <= bounds.y) return 1.0;
    vec2 outside = max(bounds.xy - pixel, pixel - bounds.zw);
    return smoothstep(-2.0, screen_size.y * 0.018, max(outside.x, outside.y));
}

float protect_orb(vec2 pixel, vec4 bounds) {
    if (bounds.z <= bounds.x || bounds.w <= bounds.y) return 1.0;
    vec2 center = (bounds.xy + bounds.zw) * 0.5;
    vec2 radius = max((bounds.zw - bounds.xy) * 0.5, vec2(screen_size.y * 0.025));
    return smoothstep(0.65, 1.65, length((pixel - center) / radius));
}

// Accumulate premultiplied layers, then unpremultiply once for blend_mix.
vec4 over(vec4 below, vec3 tint, float coverage) {
    float a = clamp(coverage, 0.0, 0.995);
    return vec4(tint * a + below.rgb * (1.0 - a), a + below.a * (1.0 - a));
}

vec4 beam(vec4 light, vec2 p, vec2 origin, float angle, float spread, float phase, float haze) {
    float t = effect_age;
    angle += sin(t * 0.38 + phase) * 0.014;
    vec2 axis = vec2(sin(angle), cos(angle));
    vec2 offset = p - origin;
    float along = dot(offset, axis);
    float across = abs(offset.x * axis.y - offset.y * axis.x);
    float width = 0.007 + max(along, 0.0) * spread;
    float core = exp(-1.8 * pow(across / width, 2.0));
    float halo = exp(-1.5 * pow(across / (width * 2.5), 2.0));
    float length_fade = (1.0 - smoothstep(0.16, 0.76, along)) * smoothstep(-0.010, 0.024, along);
    float turn_on = smoothstep(phase * 0.055, phase * 0.055 + 0.38, t);
    float breathing = 0.96 + 0.04 * sin(t * 0.75 + phase);
    float power = length_fade * turn_on * breathing * mix(0.90, 1.04, haze);
    light = over(light, ROSE, halo * power * 0.56);
    vec3 core_tint = mix(WHITE_PINK * 1.14, vec3(1.0, 0.38, 0.56), smoothstep(0.035, 0.38, along));
    light = over(light, core_tint, core * power * 0.91);
    float source = exp(-length(offset * vec2(1.0, 1.8)) * 65.0);
    light = over(light, WHITE_PINK * 1.14, source * turn_on * 0.99);
    return light;
}

void fragment() {
    vec2 uv = UV;
    float edge = min(uv.x, 1.0 - uv.x);
    float side_mask = 1.0 - smoothstep(0.100, 0.175, edge);
    if (side_mask <= 0.001 || strength <= 0.001) {
        COLOR = vec4(0.0);
    } else {
        float aspect = screen_size.x / screen_size.y;
        vec2 p = vec2(edge * aspect, uv.y);
        float side_phase = uv.x < 0.5 ? 0.0 : 1.7;
        float t = effect_age;
        float haze = noise(p * vec2(18.0, 6.0) + vec2(t * 0.07, -t * 0.12) + side_phase);
        float rig_y = max(0.070, hud_rect.w / screen_size.y + 0.014);
        float shoulder = exp(-pow((p.x - 0.070) / 0.235, 2.0))
            * (1.0 - smoothstep(0.30, 1.02, uv.y)) * smoothstep(rig_y - 0.03, rig_y + 0.02, uv.y);
        vec4 light = over(vec4(0.0), CRIMSON, shoulder * mix(0.32, 0.54, haze));
        light = over(light, ROSE, shoulder * mix(0.12, 0.23, haze));
        light = beam(light, p, vec2(0.018, rig_y), 0.025, 0.130, 0.0 + side_phase, haze);
        light = beam(light, p, vec2(0.110, rig_y), 0.175, 0.105, 1.0 + side_phase, haze);
        light = beam(light, p, vec2(0.215, rig_y), 0.295, 0.080, 2.0 + side_phase, haze);

        float dust = 0.0;
        for (int i = 0; i < 10; i++) {
            float seed = float(i) + side_phase * 19.0;
            vec2 particle = vec2(0.01 + hash(vec2(seed, 2.0)) * 0.265,
                0.095 + fract(hash(vec2(seed, 9.0)) - t * (0.008 + hash(vec2(seed, 5.0)) * 0.010)) * 0.78);
            float radius = 0.0009 + hash(vec2(seed, 8.0)) * 0.0012;
            dust += exp(-pow(length(p - particle) / radius, 2.0)) * (0.45 + 0.25 * sin(t * 1.1 + seed));
        }
        light = over(light, WHITE_PINK, dust * 0.75);
        float controls = protect_control(uv * screen_size, hud_rect)
            * protect_control(uv * screen_size, end_turn_rect)
            * protect_orb(uv * screen_size, energy_rect);
        float settle = mix(1.0, 0.94, smoothstep(0.70, 2.0, t));
        COLOR = vec4(light.rgb / max(light.a, 0.0001), light.a * side_mask * controls * strength * settle) * COLOR;
    }
}
