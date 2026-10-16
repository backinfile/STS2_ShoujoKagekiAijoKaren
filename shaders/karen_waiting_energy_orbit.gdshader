shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float strength = 0.0;
uniform float clock = 0.0;
uniform float build = 1.0;
uniform float opening = 0.0;
uniform float phase = 0.0;
uniform int seed_count = 1;
uniform vec2 outline_points[42];
uniform float outline_perimeter = 6.0;
uniform vec4 light_color : source_color = vec4(0.94, 0.54, 0.64, 1.0);
uniform vec4 core_color : source_color = vec4(1.0, 0.965, 0.98, 1.0);
const float ORBIT_TAU = 6.28318530718;
void fragment() {
    vec2 p = (UV - vec2(0.5)) * 2.0;
    float radius = 62.0 / 120.0;
    float delta = 10.0;
    float along = 0.0;
    float travelled = 0.0;
    for (int i = 0; i < 42; i++) {
        vec2 a = outline_points[i] * radius;
        vec2 edge = (outline_points[(i + 1) % 42] - outline_points[i]) * radius;
        float edge_length = length(edge);
        float t = clamp(dot(p - a, edge) / max(dot(edge, edge), 0.0000001), 0.0, 1.0);
        float distance_to_edge = length(p - a - edge * t);
        if (distance_to_edge < delta) {
            delta = distance_to_edge;
            along = (travelled + t * edge_length) / (outline_perimeter * radius) * ORBIT_TAU;
        }
        travelled += edge_length;
    }
    float core = exp(-pow(delta / 0.0080, 2.0));
    float inner = exp(-pow(delta / 0.022, 2.0)) * 0.30;
    float glow = exp(-pow(delta / 0.065, 2.0)) * 0.12;
    float sector = fract(along / ORBIT_TAU * float(max(seed_count, 1)));
    float painted = build >= 0.999 ? 1.0 : (1.0 - smoothstep(build - 0.012, build + 0.012, sector));
    if (build <= 0.001) painted = 0.0;
    float shimmer = 0.96 + 0.04 * sin(along * 7.0 - clock * 0.8);
    float light = core * 0.72 * shimmer + inner + glow;
    vec3 emission = mix(light_color.rgb * 1.20, core_color.rgb * 1.10, core * 0.08);
    COLOR = vec4(emission, strength * painted * light);
}
