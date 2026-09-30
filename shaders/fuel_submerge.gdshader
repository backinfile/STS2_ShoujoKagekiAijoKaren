shader_type canvas_item;
uniform float surface_cut = 1.0;
uniform float heat = 0.0;
varying vec4 vertex_tint;
void vertex() { vertex_tint = COLOR; }
void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    tex.rgb = mix(tex.rgb, vec3(1.0, 0.54, 0.08), heat * 0.55);
    tex.a *= 1.0 - smoothstep(surface_cut - 0.008, surface_cut + 0.008, UV.y);
    COLOR = vertex_tint * tex;
}
