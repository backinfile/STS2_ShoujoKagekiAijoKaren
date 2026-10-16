shader_type canvas_item;
render_mode unshaded;
uniform float decay : hint_range(0.0, 1.0) = 0.0;
uniform float reveal : hint_range(0.0, 1.0) = 0.0;
void fragment() {
    // The reference blade progresses from its upper-left tip to its lower-right tip.
    float swept = clamp((UV.y - 0.106858) / 0.8126, 0.0, 1.0);
    float visible = 1.0 - smoothstep(reveal - 0.06, reveal + 0.02, swept);
    // CanvasItem fragment COLOR already includes the source texture and draw tint.
    // Keep the single flat surface intact; fade it uniformly after impact.
    COLOR.a *= visible * (1.0 - decay);
}
