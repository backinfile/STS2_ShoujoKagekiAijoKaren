shader_type canvas_item;
render_mode unshaded, blend_add;

uniform float intensity = 0.0;
uniform float age = 0.0;
uniform bool ground = false;
uniform vec4 light_color : source_color = vec4(1.0, 0.87, 0.63, 1.0);

void fragment() {
    float light;
    if (ground) {
        vec2 point = (UV - vec2(0.5)) * 2.0;
        float radius = length(point);
        light = exp(-radius * radius * 3.8) * (1.0 - smoothstep(0.60, 1.0, radius));
    } else {
        float half_width = mix(0.045, 0.47, UV.y);
        float center = mix(0.405, 0.57, UV.y) + sin(age * 0.65) * UV.y * 0.028;
        float signed_across = (UV.x - center) / half_width;
        float across = abs(signed_across);
        float edge = exp(-across * across * 1.5) * (1.0 - smoothstep(0.60, 1.0, across));
        float length_fade = smoothstep(0.0, 0.05, UV.y) * (1.0 - smoothstep(0.86, 1.0, UV.y));
        // Two broad, slowly moving shafts add volume without a noise or stripe texture.
        float shafts = exp(-pow((signed_across + 0.30 + sin(age * 0.55) * 0.09) / 0.17, 2.0));
        shafts += 0.65 * exp(-pow((signed_across - 0.33 + sin(age * 0.43 + 1.2) * 0.10) / 0.21, 2.0));
        light = edge * length_fade * (mix(0.60, 0.27, UV.y) + shafts * 0.20);
    }
    COLOR = vec4(light_color.rgb, light * intensity * light_color.a);
}
