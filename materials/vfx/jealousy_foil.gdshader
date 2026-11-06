shader_type canvas_item;
render_mode unshaded;

varying vec4 foil_color;
varying float foil_phase;
varying float foil_turn;

void vertex() {
    foil_color = COLOR;
    foil_phase = INSTANCE_CUSTOM.y;
    foil_turn = cos(INSTANCE_CUSTOM.x * 1.7 + foil_phase * 9.0);
    // Thin side-on flashes, with a slight second-axis tilt instead of dark paper.
    VERTEX.x *= 0.055 + 0.945 * abs(foil_turn);
    VERTEX.y *= 0.85 + 0.15 * abs(sin(INSTANCE_CUSTOM.x * 0.35 + foil_phase * 4.5));
}

void fragment() {
    vec2 p = (UV - vec2(0.5)) * 2.0;
    float d = max(abs(p.x), abs(p.y));
    float face = 1.0 - smoothstep(0.60, 0.67, d);
    float edge = smoothstep(0.47, 0.61, d) * face;
    // Mahiru's TV squares have pastel luminous faces, soft pearl gradients and corner glints.
    float pearl = 0.5 + 0.5 * sin((UV.x - UV.y) * 2.8 + foil_turn * 1.4 + foil_phase * 3.0);
    float gloss = pow(max(0.0, 1.0 - abs(UV.x + UV.y - 1.0 - foil_turn * 0.25)), 14.0);
    vec3 color = mix(foil_color.rgb, vec3(1.0, 0.98, 1.0), 0.12 + 0.22 * pearl);
    color = mix(color, vec3(1.0), clamp(edge * 0.30 + gloss * 0.34, 0.0, 0.65));
    float halo = (1.0 - smoothstep(0.55, 1.0, d)) * 0.20;
    vec2 corner = p - vec2(0.59, -0.59);
    float cross = exp(-abs(corner.x) * 48.0 - abs(corner.y) * 9.0)
                + exp(-abs(corner.y) * 48.0 - abs(corner.x) * 9.0);
    float twinkle = pow(0.5 + 0.5 * sin(foil_phase * 17.0 + foil_turn * 4.0), 8.0)
                  * smoothstep(0.2, 0.85, abs(foil_turn));
    color = mix(color, vec3(1.0), clamp(cross * twinkle, 0.0, 0.75));
    COLOR = vec4(color, clamp(max(face, halo) + cross * twinkle * 0.28, 0.0, 1.0) * foil_color.a);
}
