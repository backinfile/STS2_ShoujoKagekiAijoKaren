shader_type canvas_item;
render_mode unshaded, blend_add;

// Analytic blue-white wisp: pointed leading core, curved soft tail, no rectangular edge.
// Geometry/lifetime come from its owner so pausing, fading and time scale remain consistent.
void fragment() {
    float x = UV.x;
    float y = UV.y - 0.5 - 0.10 * (x - 0.20) * (x - 0.20);
    float nose = smoothstep(0.025, 0.15, x);
    float taper = pow(1.0 - smoothstep(0.16, 0.99, x), 1.5);
    float width = 0.012 + 0.082 * taper;
    float core = exp(-2.0 * pow((x - 0.24) / 0.21, 2.0))
        * exp(-2.0 * pow(y / width, 2.0)) * nose;
    float tail = exp(-2.0 * pow(y / (0.035 + 0.07 * taper), 2.0)) * taper * nose;
    float glow = exp(-2.0 * pow(y / 0.25, 2.0))
        * exp(-1.5 * pow((x - 0.30) / 0.38, 2.0)) * nose;
    vec3 light = vec3(0.84, 0.96, 1.0) * core * 0.88
        + vec3(0.24, 0.60, 1.0) * tail * 0.38
        + vec3(0.20, 0.48, 1.0) * glow * 0.12;
    float alpha = clamp(max(light.r, max(light.g, light.b)), 0.0, 1.0);
    COLOR = vec4(light / max(alpha, 0.001), alpha) * COLOR;
}
