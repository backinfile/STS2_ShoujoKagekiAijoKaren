shader_type canvas_item;
render_mode unshaded;
uniform float age = 0.0;
void fragment() {
    vec2 uv = UV;
    float y = abs(uv.y * 2.0 - 1.0);
    float grain = sin(uv.x * 81.0 + uv.y * 29.0) * sin(uv.x * 143.0 - age * 33.0);
    float width = (0.18 + 0.63 * pow(uv.x, 0.65)) * (1.0 - smoothstep(0.83, 1.0, uv.x));
    width += grain * 0.025;
    float edge = 1.0 - smoothstep(max(0.0, width - 0.12), width + 0.04, y);
    float core = 1.0 - smoothstep(width * 0.15, width * 0.63 + 0.01, y);
    float tail = smoothstep(0.0, 0.09, uv.x);
    float fade = 1.0 - smoothstep(0.055, 0.24, age);
    vec3 tint = mix(vec3(1.0, 0.20, 0.34), vec3(1.0, 0.97, 0.88), core);
    COLOR = vec4(tint, edge * tail * fade);
}
