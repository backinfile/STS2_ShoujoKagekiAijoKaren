shader_type canvas_item;

uniform float sway_strength : hint_range(0.0, 0.04) = 0.018;
uniform float sway_speed : hint_range(0.1, 8.0) = 2.3;
uniform float sway_frequency : hint_range(1.0, 20.0) = 12.0;
uniform float cape_top_fade : hint_range(0.0, 1.0) = 0.08;
uniform float cape_bottom_fade : hint_range(0.0, 1.0) = 0.47;

float red_cape_mask(vec4 color) {
	float red_bias = color.r - max(color.g, color.b);
	float warm_red = smoothstep(0.05, 0.22, red_bias);
	float saturation = smoothstep(0.10, 0.28, color.r - color.b * 0.65);
	return warm_red * saturation * smoothstep(0.12, 0.35, color.a);
}

float vertical_cape_mask(vec2 uv) {
	float top = smoothstep(cape_top_fade, cape_top_fade + 0.08, uv.y);
	float bottom = 1.0 - smoothstep(cape_bottom_fade, cape_bottom_fade + 0.12, uv.y);
	return top * bottom;
}

void fragment() {
	vec4 base_color = texture(TEXTURE, UV);
	float mask = red_cape_mask(base_color) * vertical_cape_mask(UV);

	float phase = TIME * sway_speed + UV.y * sway_frequency;
	float cross_wave = sin(phase + UV.x * 5.0);
	float secondary_wave = sin(phase * 0.63 - UV.x * 9.0) * 0.45;
	float edge_weight = smoothstep(0.02, 0.42, abs(UV.x - 0.5) * 2.0);
	float upper_weight = 1.0 - smoothstep(0.36, 0.52, UV.y);
	vec2 offset = vec2(cross_wave + secondary_wave, sin(phase * 0.8) * 0.28);
	offset *= sway_strength * edge_weight * upper_weight * mask;

	vec4 swayed_color = texture(TEXTURE, UV + offset);
	COLOR = mix(base_color, swayed_color, mask);
}
