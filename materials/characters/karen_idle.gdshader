shader_type canvas_item;

// Two breathing cycles, advanced by the node so scene pauses also pause the cloth.
uniform float idle_phase = 0.0;
uniform float breath_lift : hint_range(0.0, 0.02) = 0.006;
uniform float sway_strength : hint_range(0.0, 0.04) = 0.005;
uniform vec4 cape_region_1 = vec4(0.16, 0.20, 0.49, 0.44);
uniform vec4 cape_region_2 = vec4(0.60, 0.02, 0.74, 0.38);
uniform vec4 cape_region_3 = vec4(0.46, 0.18, 0.60, 0.36);

varying vec4 sprite_modulate;

void vertex() {
	sprite_modulate = COLOR;
}

float rect_mask(vec2 uv, vec4 rect) {
	vec2 feather = min(vec2(0.035), (rect.zw - rect.xy) * 0.3);
	vec2 leading = smoothstep(rect.xy, rect.xy + feather, uv);
	vec2 trailing = vec2(1.0) - smoothstep(rect.zw - feather, rect.zw, uv);
	return leading.x * leading.y * trailing.x * trailing.y;
}

float breath_at(float phase) {
	float cycle = fract(phase);
	// 1.76 s inhale, 2.64 s exhale; zero velocity at both turnarounds.
	float half_cycle = cycle < 0.4 ? cycle / 0.4 : (1.0 - cycle) / 0.6;
	return 0.5 - 0.5 * cos(3.14159265 * half_cycle);
}

void fragment() {
	float breath = breath_at(idle_phase);
	// The head moves as a unit. The lift fades through the hips to stationary legs.
	float upper_body = 1.0 - smoothstep(0.34, 0.76, UV.y);
	vec2 uv = UV + vec2(-0.0012, breath_lift) * breath * upper_body;
	float chest = rect_mask(uv, vec4(0.40, 0.22, 0.60, 0.49));
	uv.x -= (uv.x - 0.50) * 0.012 * breath * chest;

	float left_cape = rect_mask(uv, cape_region_1);
	float right_cape = rect_mask(uv, cape_region_2);
	float top_cape = rect_mask(uv, cape_region_3);
	float cape = max(max(left_cape, right_cape), top_cape);
	// Keep the collar quiet; the free ends follow the breath about 0.3 s later.
	float free_edge = smoothstep(0.025, 0.25, abs(uv.x - 0.51));
	float wave = sin(6.28318531 * (idle_phase - 0.07) + uv.x * 5.0);
	float secondary = sin(3.14159265 * idle_phase - uv.y * 4.0) * 0.22;
	vec2 cloth = vec2(wave * 0.22, wave + secondary);
	uv += cloth * sway_strength * cape * mix(0.18, 1.0, free_edge);

	// Sample once, including transparent pixels, so the silhouette moves without
	// leaving the original outline behind. Preserve Sprite2D/ancestor modulation.
	COLOR = texture(TEXTURE, uv) * sprite_modulate;
}
