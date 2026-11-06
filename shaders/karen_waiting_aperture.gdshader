shader_type canvas_item;
render_mode unshaded, blend_mix;
uniform float strength = 0.0;
uniform float clock = 0.0;
uniform float opening = 0.0;
uniform float activity = 0.0;
void fragment() {
    // A narrow contrasting rim keeps the local slit legible on Karen's white collar.
    float y = (UV.y - 0.5) * 2.0;
    float extent = mix(0.028, 0.24, opening) * sqrt(max(0.0, 1.0 - y * y));
    float distance_to_lip = abs(abs(UV.x - 0.5) - extent);
    float lip = exp(-pow(distance_to_lip / 0.020, 2.0));
    float contour = exp(-pow(distance_to_lip / 0.047, 2.0));
    float tips = 1.0 - smoothstep(0.65, 1.0, abs(y));
    vec3 cold = mix(vec3(0.48, 0.78, 0.93), vec3(0.92, 0.98, 1.0), opening * 0.4 + activity * 0.5);
    vec3 rim = mix(vec3(0.025, 0.09, 0.14), cold, lip);
    float alpha = contour * tips * clamp(strength * 2.1, 0.0, 1.0);
    COLOR = vec4(rim, alpha * (0.98 + 0.02 * sin(clock * 1.3)));
}
