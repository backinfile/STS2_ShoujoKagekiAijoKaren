shader_type canvas_item;
render_mode blend_add, unshaded;

uniform float age = 0.0;

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}
float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), f.x),
        mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0)), f.x), f.y);
}
void fragment() {
    float edge = abs(UV.y - 0.5) * 2.0;
    float grain = noise(vec2(UV.x * 38.0 - age * 7.0, UV.y * 9.0));
    float erosion = 1.0 - smoothstep(0.62 + grain * 0.34, 1.02, edge);
    float streaks = noise(vec2(UV.x * 11.0 - age * 4.0, UV.y * 35.0));
    // Preserve a continuous cutting core; only the outer mist breaks into wisps.
    float texture = mix(1.0, 0.32 + streaks * 0.85, smoothstep(0.1, 0.8, edge));
    COLOR.a *= erosion * texture;
}
