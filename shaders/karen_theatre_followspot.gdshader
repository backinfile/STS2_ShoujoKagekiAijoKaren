shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float strength = 0.0;
uniform float clock = 0.0;
uniform vec4 light_color : source_color = vec4(0.9, 0.6, 0.5, 1.0);
void fragment() {
    // Short local beams breathe above the energy counter, with no hard source dot.
    float width = mix(0.46, 0.12, UV.y);
    float cross_section = exp(-pow((UV.x - 0.5) / width, 2.0) * 2.4);
    float fade = smoothstep(0.0, 0.15, UV.y) * (1.0 - smoothstep(0.80, 1.0, UV.y));
    float haze = 0.98 + 0.02 * sin(UV.y * 9.0 - clock * 0.6);
    COLOR = vec4(light_color.rgb, cross_section * fade * haze * strength);
}
