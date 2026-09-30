shader_type canvas_item;
uniform float elapsed = 0.0;
uniform float pulse = 0.0;
varying vec4 vertex_tint;
void vertex() { vertex_tint = COLOR; }
void fragment() {
    // Restrict motion to the interior liquid; masonry and the transparent silhouette stay still.
    float mask = smoothstep(0.46, 0.50, UV.y) * (1.0 - smoothstep(0.67, 0.70, UV.y));
    mask *= smoothstep(0.07, 0.16, UV.x) * (1.0 - smoothstep(0.84, 0.93, UV.x));
    vec2 flow = vec2(sin(UV.y * 42.0 + elapsed * 2.8), cos(UV.x * 35.0 - elapsed * 2.0)) * 0.0025 * mask;
    vec4 tex = texture(TEXTURE, UV + flow);
    tex.rgb *= 1.0 + mask * (0.025 * sin(UV.x * 24.0 + elapsed * 4.0) + pulse * 0.2);
    COLOR = vertex_tint * tex;
}
