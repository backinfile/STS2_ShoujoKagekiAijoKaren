shader_type canvas_item;
render_mode unshaded, blend_add;

varying vec4 glint_color;
varying float glint_phase;

void vertex() {
    glint_color = COLOR;
    glint_phase = INSTANCE_CUSTOM.y;
}

void fragment() {
    vec2 p = abs(UV - vec2(0.5)) * 2.0;
    float rays = pow(max(0.0, 1.0 - p.x), 3.0) * exp(-p.y * 35.0)
               + pow(max(0.0, 1.0 - p.y), 3.0) * exp(-p.x * 35.0);
    float core = exp(-dot(p, p) * 55.0);
    float pulse = 0.55 + 0.45 * sin(glint_phase * 12.0);
    COLOR = vec4(mix(glint_color.rgb, vec3(1.0), 0.65), min(1.0, rays + core) * pulse * glint_color.a);
}
