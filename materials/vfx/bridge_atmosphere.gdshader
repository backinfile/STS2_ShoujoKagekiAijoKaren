shader_type canvas_item;
render_mode unshaded;

uniform float age = 0.0;
uniform float strength = 0.0;

void fragment() {
    // Broad moving folds without high-frequency noise or dithering.
    vec2 p = UV;
    float fold = sin(p.x * 10.0 + sin(p.y * 5.0) + age * 0.08) * 0.5 + 0.5;
    float bank = exp(-pow((p.y - 0.24 - fold * 0.14) / 0.21, 2.0));
    vec3 wine = mix(vec3(0.050, 0.018, 0.037), vec3(0.19, 0.028, 0.070), bank * 0.67);
    float coverage = 0.92 * (1.0 - smoothstep(0.22, 1.0, p.y));
    COLOR = vec4(wine, coverage * strength);
}
