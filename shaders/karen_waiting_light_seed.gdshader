shader_type canvas_item;
render_mode unshaded, blend_add;
uniform vec4 light_color : source_color = vec4(0.94, 0.54, 0.64, 1.0);
uniform vec4 core_color : source_color = vec4(1.0, 0.965, 0.98, 1.0);
uniform float strength = 1.0;
varying float seed_opacity;
void vertex() {
    seed_opacity = COLOR.a;
}
void fragment() {
    vec2 p = (UV - vec2(0.5)) * 2.0;
    float r = length(p);
    float core = exp(-pow(r / 0.12, 2.0));
    float inner = exp(-pow(r / 0.30, 2.0)) * 0.62;
    float outer = exp(-pow(r / 0.66, 2.0)) * 0.26;
    vec3 emission = mix(light_color.rgb * 1.45, core_color.rgb * 1.45, clamp(core + inner * 0.45, 0.0, 1.0));
    COLOR = vec4(emission, (core + inner + outer) * strength * seed_opacity);
}
