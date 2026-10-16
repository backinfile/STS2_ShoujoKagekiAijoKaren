shader_type canvas_item;
render_mode unshaded;

varying vec4 foil_color;
varying float foil_phase;
varying float foil_turn;

void vertex() {
    foil_color = COLOR;
    foil_phase = INSTANCE_CUSTOM.y;
    foil_turn = cos(INSTANCE_CUSTOM.x * 1.7 + foil_phase * 9.0);
    VERTEX.x *= 0.14 + 0.86 * abs(foil_turn);
}

void fragment() {
    vec2 p = abs(UV - vec2(0.5)) * 2.0;
    float d = max(p.x, p.y);
    float face = 1.0 - smoothstep(0.60, 0.67, d);
    float edge = smoothstep(0.45, 0.60, d) * face;
    float gloss = pow(max(0.0, 1.0 - abs(UV.x + UV.y - 0.9 - foil_turn * 0.3)), 16.0);
    float halo = pow(1.0 - smoothstep(0.55, 1.0, d), 2.0) * 0.24;
    vec3 color = mix(foil_color.rgb, vec3(1.0), clamp(edge * 0.55 + gloss * 0.25, 0.0, 0.7));
    COLOR = vec4(color, max(face, halo) * foil_color.a);
}
